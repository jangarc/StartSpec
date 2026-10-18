using ContractSeedwork.Fakes;
using Iam.Domain.Authorization;
using Iam.Domain.Tenants;
using Iam.Domain.Users;
using Xunit;

namespace Iam.UnitTests.Events;

/// <summary>
/// Event seam contract suite (task 6.1, fake run; platform spec "Event delivery contract"): the four
/// canonical IAM events — each emitted by a real domain transition — deliver to typed subscribers as
/// the SAME instance, fake redelivery repeats the last event without a new publication (at-least-once
/// with the same event_id), and an idempotent consumer applying duplicates stays unchanged. The same
/// contract asserts run against the wired InMemoryEventBus in the Reqnroll project.
/// 事件接縫契約套件（fake 跑）：四枚正典事件經真實領域轉換發出；同實例投遞、重投遞為同一事件（同 event_id）、
/// 冪等消費者對重複投遞不變；同一套斷言在 Reqnroll 專案對已接線 InMemoryEventBus 雙跑。
/// </summary>
public sealed class EventSeamContractTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-alpha";

    private sealed record CanonicalEvents(
        TenantProvisioned TenantProvisioned,
        UserProvisioned UserProvisioned,
        UserDisabled UserDisabled,
        RoleRevoked RoleRevoked);

    private static CanonicalEvents EmitCanonical()
    {
        var tenantEvent = (TenantProvisioned)Tenant.Provision(TenantId, "Alpha Health", "Alpha HQ", FixedNow)
            .PullDomainEvents().OfType<TenantProvisioned>().Single();

        var user = User.Provision(TenantId, "alice", Guid.NewGuid(), FixedNow);
        var userProvisioned = user.PullDomainEvents().OfType<UserProvisioned>().Single();
        user.Disable(FixedNow);
        var userDisabled = user.PullDomainEvents().OfType<UserDisabled>().Single();

        var role = Role.Provision(TenantId, "refunder", ["orders.refund"]);
        var assignment = RoleAssignment.Issue(role, TenantId, Guid.NewGuid(), FixedNow);
        assignment.Revoke(FixedNow);
        var roleRevoked = assignment.PullDomainEvents().OfType<RoleRevoked>().Single();

        return new CanonicalEvents(tenantEvent, userProvisioned, userDisabled, roleRevoked);
    }

    [Fact]
    public async Task All_four_canonical_events_deliver_to_typed_subscribers()
    {
        var bus = new FakeEventBus();
        var canonical = EmitCanonical();
        var (tenants, usersProvisioned, usersDisabled, rolesRevoked) = WithSubscribers(bus);

        await bus.PublishAsync(canonical.TenantProvisioned);
        await bus.PublishAsync(canonical.UserProvisioned);
        await bus.PublishAsync(canonical.UserDisabled);
        await bus.PublishAsync(canonical.RoleRevoked);

        Assert.Same(canonical.TenantProvisioned, Assert.Single(tenants));
        Assert.Same(canonical.UserProvisioned, Assert.Single(usersProvisioned));
        Assert.Same(canonical.UserDisabled, Assert.Single(usersDisabled));
        Assert.Same(canonical.RoleRevoked, Assert.Single(rolesRevoked));
        Assert.All(
            new SharedKernel.DomainEvent[] { canonical.TenantProvisioned, canonical.UserProvisioned, canonical.UserDisabled, canonical.RoleRevoked },
            e => Assert.Equal(1, e.Version));
        Assert.Equal(4, new[]
            {
                canonical.TenantProvisioned.EventId,
                canonical.UserProvisioned.EventId,
                canonical.UserDisabled!.EventId,
                canonical.RoleRevoked.EventId,
            }
            .Distinct()
            .Count());
        Assert.Equal(4, bus.Published.Count);
    }

    [Fact]
    public async Task Fake_redelivery_repeats_last_event_same_event_id_without_new_publication()
    {
        var bus = new FakeEventBus();
        var canonical = EmitCanonical();
        var (tenants, usersProvisioned, usersDisabled, rolesRevoked) = WithSubscribers(bus);
        await PublishAllAsync(bus, canonical);

        await bus.SimulateRedeliveryAsync();

        Assert.Equal(2, rolesRevoked.Count);
        Assert.Same(canonical.RoleRevoked, rolesRevoked[1]);
        Assert.Single(usersDisabled);
        Assert.Equal(4, bus.Published.Count);
        Assert.Equal(5, bus.DeliveredCount);
    }

    [Fact]
    public async Task Idempotent_consumer_stays_unchanged_across_redelivery()
    {
        var bus = new FakeEventBus();
        var canonical = EmitCanonical();
        var processedEventIds = new HashSet<Guid>();
        var permissionRevokedSeen = false;
        using var subscription = bus.Subscribe<RoleRevoked>((e, _) =>
        {
            if (processedEventIds.Add(e.EventId))
            {
                permissionRevokedSeen = true;
            }

            return Task.CompletedTask;
        });

        await bus.PublishAsync(canonical.RoleRevoked);
        await bus.SimulateRedeliveryAsync();
        await bus.SimulateRedeliveryAsync();

        Assert.Equal(canonical.RoleRevoked.EventId, Assert.Single(processedEventIds));
        Assert.True(permissionRevokedSeen);
        Assert.Single(bus.Published);
        Assert.Equal(3, bus.DeliveredCount);
    }

    private static (List<TenantProvisioned> Tenants, List<UserProvisioned> UsersProvisioned, List<UserDisabled> UsersDisabled, List<RoleRevoked> RolesRevoked) WithSubscribers(FakeEventBus bus)
    {
        List<TenantProvisioned> tenants = [];
        List<UserProvisioned> usersProvisioned = [];
        List<UserDisabled> usersDisabled = [];
        List<RoleRevoked> rolesRevoked = [];

        bus.Subscribe<TenantProvisioned>((e, _) => { tenants.Add(e); return Task.CompletedTask; });
        bus.Subscribe<UserProvisioned>((e, _) => { usersProvisioned.Add(e); return Task.CompletedTask; });
        bus.Subscribe<UserDisabled>((e, _) => { usersDisabled.Add(e); return Task.CompletedTask; });
        bus.Subscribe<RoleRevoked>((e, _) => { rolesRevoked.Add(e); return Task.CompletedTask; });

        return (tenants, usersProvisioned, usersDisabled, rolesRevoked);
    }

    private static async Task PublishAllAsync(FakeEventBus bus, CanonicalEvents canonical)
    {
        await bus.PublishAsync(canonical.TenantProvisioned);
        await bus.PublishAsync(canonical.UserProvisioned);
        await bus.PublishAsync(canonical.UserDisabled);
        await bus.PublishAsync(canonical.RoleRevoked);
    }
}
