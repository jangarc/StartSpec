using ContractSeedwork.Fakes;
using Iam.Domain.Tenants;
using Xunit;

namespace Iam.UnitTests.Seedwork;

/// <summary>
/// Seedwork smoke tests: proves the contract base + SharedKernel-port fakes load and execute from their
/// home (ADR-010). Deep contract suites per seam arrive with their tasks (persistence 2.2, auth 4.2/4.3,
/// events 6.1) and run against this fake.
/// seedwork 基底載入並執行之證明；各接縫深契約套件於其任務進駐。
/// </summary>
public class ContractSeedworkSmokeTests
{
    [Fact]
    public void FakeClock_Is_Deterministic_And_Advances()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal(2026, clock.UtcNow.Year);
        clock.Advance(TimeSpan.FromMinutes(5));

        Assert.Equal(new DateTimeOffset(2026, 1, 1, 0, 5, 0, TimeSpan.Zero), clock.UtcNow);
    }

    [Fact]
    public async Task FakeEventBus_Delivers_To_Typed_Subscriber()
    {
        var bus = new FakeEventBus();
        var received = new List<TenantProvisioned>();
        using var subscription = bus.Subscribe<TenantProvisioned>(SubscribeCollect(received));

        var tenantEvent = ProvisionFirstEvent();
        await bus.PublishAsync(tenantEvent);

        Assert.Single(received);
        Assert.Single(bus.Published);
        Assert.Equal(1, bus.DeliveredCount);
    }

    [Fact]
    public async Task FakeEventBus_Redelivery_Repeats_Same_Event_Id_At_Least_Once()
    {
        var bus = new FakeEventBus();
        var ids = new List<Guid>();
        using var subscription = bus.Subscribe<TenantProvisioned>(SubscribeCollectIds(ids));

        var tenantEvent = ProvisionFirstEvent();
        await bus.PublishAsync(tenantEvent);
        await bus.SimulateRedeliveryAsync();

        // At-least-once: the SAME event (same event_id) arrives more than once — no new publication.
        Assert.Equal(2, ids.Count);
        Assert.Equal(ids[0], ids[1]);
        Assert.Single(bus.Published);

        // Unsubscribe stops further deliveries.
        subscription.Dispose();
        await bus.PublishAsync(tenantEvent);
        Assert.Equal(2, ids.Count);
    }

    private static TenantProvisioned ProvisionFirstEvent()
    {
        var drained = Tenant.Provision("acme", "Acme Health", "Acme HQ", DateTimeOffset.UtcNow).PullDomainEvents();
        return (TenantProvisioned)drained[0];
    }

    private static Func<TenantProvisioned, CancellationToken, Task> SubscribeCollect(List<TenantProvisioned> received)
    {
        return (e, _) =>
        {
            received.Add(e);
            return Task.CompletedTask;
        };
    }

    private static Func<TenantProvisioned, CancellationToken, Task> SubscribeCollectIds(List<Guid> ids)
    {
        return (e, _) =>
        {
            ids.Add(e.EventId);
            return Task.CompletedTask;
        };
    }
}
