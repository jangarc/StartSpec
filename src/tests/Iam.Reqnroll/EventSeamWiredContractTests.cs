using Hosts.Base;
using Iam.Domain.Authorization;
using Iam.Domain.Tenants;
using Iam.Domain.Users;
using Xunit;

namespace Iam.Reqnroll;

/// <summary>
/// Wired dual-run of the event seam contract suite (task 6.1, ADR-010: acceptance runs against the
/// fake AND the wired implementation). The same four-canonical-events contract asserts that passed on
/// the FakeEventBus run here against the production InMemoryEventBus (Hosts.Base PortAdapter): typed
/// delivery of the same instance, duplicate delivery of the same event_id (at-least-once, consumers
/// idempotent), and unsubscribe stops delivery.
/// 事件接縫契約套件之已接線雙跑：同一套四事件契約斷言對生產 InMemoryEventBus 執行——同實例投遞、
/// 同 event_id 重複投遞（at-least-once、消費者冪等）、退訂即停。
/// </summary>
public sealed class EventSeamWiredContractTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-wired";

    private sealed record CanonicalEvents(
        TenantProvisioned TenantProvisioned,
        UserProvisioned UserProvisioned,
        UserDisabled UserDisabled,
        RoleRevoked RoleRevoked);

    private static CanonicalEvents EmitCanonical()
    {
        var tenantEvent = (TenantProvisioned)Tenant.Provision(TenantId, "Wired Health", "Wired HQ", FixedNow)
            .PullDomainEvents().OfType<TenantProvisioned>().Single();

        var user = User.Provision(TenantId, "bob", Guid.NewGuid(), FixedNow);
        var userProvisionedEvent = user.PullDomainEvents().OfType<UserProvisioned>().Single();
        user.Disable(FixedNow);
        var userDisabledEvent = user.PullDomainEvents().OfType<UserDisabled>().Single();

        var role = Role.Provision(TenantId, "wired-refunder", ["wired.orders.refund"]);
        var assignment = RoleAssignment.Issue(role, TenantId, Guid.NewGuid(), FixedNow);
        assignment.Revoke(FixedNow);
        var roleRevokedEvent = assignment.PullDomainEvents().OfType<RoleRevoked>().Single();

        return new CanonicalEvents(tenantEvent, userProvisionedEvent, userDisabledEvent, roleRevokedEvent);
    }

    [Fact]
    public async Task Wired_four_canonical_events_deliver_to_typed_subscribers()
    {
        var bus = new InMemoryEventBus();
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
    }

    [Fact]
    public async Task Wired_duplicate_delivery_same_event_id_keeps_consumers_idempotent()
    {
        var bus = new InMemoryEventBus();
        var canonical = EmitCanonical();
        var deliveries = new List<RoleRevoked>();
        var processedEventIds = new HashSet<Guid>();
        var permissionRevokedSeen = false;
        using var subscription = bus.Subscribe<RoleRevoked>((e, _) =>
        {
            deliveries.Add(e);
            if (processedEventIds.Add(e.EventId))
            {
                permissionRevokedSeen = true;
            }

            return Task.CompletedTask;
        });

        await bus.PublishAsync(canonical.RoleRevoked);
        await bus.PublishAsync(canonical.RoleRevoked);

        Assert.Equal(2, deliveries.Count);
        Assert.Same(deliveries[0], deliveries[1]);
        Assert.Single(processedEventIds);
        Assert.True(permissionRevokedSeen);
    }

    [Fact]
    public async Task Wired_unsubscribe_stops_delivery()
    {
        var bus = new InMemoryEventBus();
        var canonical = EmitCanonical();
        var tenants = new List<TenantProvisioned>();
        using var subscription = bus.Subscribe<TenantProvisioned>((e, _) => { tenants.Add(e); return Task.CompletedTask; });

        await bus.PublishAsync(canonical.TenantProvisioned);
        subscription.Dispose();
        await bus.PublishAsync(canonical.TenantProvisioned);

        Assert.Single(tenants);
    }

    private static (List<TenantProvisioned> Tenants, List<UserProvisioned> UsersProvisioned, List<UserDisabled> UsersDisabled, List<RoleRevoked> RolesRevoked) WithSubscribers(InMemoryEventBus bus)
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
}
