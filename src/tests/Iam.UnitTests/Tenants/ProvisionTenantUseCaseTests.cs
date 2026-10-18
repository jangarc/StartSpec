using ContractSeedwork.Fakes;
using Iam.Application.Tenants;
using Iam.ContractTests.Fakes;
using Xunit;

namespace Iam.UnitTests.Tenants;

/// <summary>
/// Application use-case TDD over the locked store contract's in-memory fake: uniqueness guard,
/// persistence delegation, and bus publication of the drained domain event.
/// use-case 層 TDD：唯一性防護、委派持久、經事件 bus 發布抽乾事件。
/// </summary>
public sealed class ProvisionTenantUseCaseTests
{
    private const string ExistingTenant = "acme";

    private static readonly DateTimeOffset FixedNow = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static ProvisionTenantCommand NewCommand(string tenantId = "globex") =>
        new(tenantId, "Globex Health", "Globex HQ");

    [Fact]
    public async Task Delegates_to_store_and_returns_the_provisioned_organization_identity()
    {
        var store = new InMemoryTenantStore();
        var bus = new FakeEventBus();
        var useCase = new ProvisionTenantUseCase(store, new FakeClock(FixedNow), bus);

        ProvisionTenantResponse response = await useCase.HandleAsync(NewCommand());

        Assert.Equal("globex", response.TenantId);
        Assert.True(await store.TenantExistsAsync("globex"));
        var entry = Assert.Single((await store.ListOrganizationsAsync("globex")).Items);
        Assert.Equal(response.OrganizationId, entry.OrganizationId);
    }

    [Fact]
    public async Task Duplicate_provision_is_a_conflict_without_a_second_save()
    {
        var store = new InMemoryTenantStore();
        var bus = new FakeEventBus();
        var useCase = new ProvisionTenantUseCase(store, new FakeClock(FixedNow), bus);
        await useCase.HandleAsync(NewCommand(ExistingTenant));
        var savedBefore = store.SavedCount;

        var rejected = await Assert.ThrowsAsync<TenantAlreadyExistsException>(
            () => useCase.HandleAsync(NewCommand(ExistingTenant)));

        Assert.Equal(ExistingTenant, rejected.TenantId);
        Assert.Equal(savedBefore, store.SavedCount); // create nothing on conflict
        Assert.Single(bus.Published); // only the original provisioning event
    }

    [Fact]
    public async Task Publishes_the_drained_TenantProvisioned_after_persistence()
    {
        var store = new InMemoryTenantStore();
        var bus = new FakeEventBus();
        var useCase = new ProvisionTenantUseCase(store, new FakeClock(new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero)), bus);

        await useCase.HandleAsync(NewCommand());

        var published = Assert.Single(bus.Published);
        var provisioning = Assert.IsType<Iam.Domain.Tenants.TenantProvisioned>(published);
        Assert.Equal("globex", provisioning.TenantId);
        Assert.Equal(new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero), provisioning.OccurredAt);
    }
}
