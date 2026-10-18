using Iam.Application.Tenants;
using Iam.ContractTests.Fakes;
using Iam.ContractTests.Tenants;
using Xunit;

namespace Iam.ContractTests.Tenants.FakeLegs;

/// <summary>Fake leg: one shared in-memory store per test instance; the scope rides on the port parameters.</summary>
public sealed class InMemoryTenantStoreLeg : TenantStoreContractBase
{
    private readonly InMemoryTenantStore _store = new();

    protected override Task<ITenantStore> CreateScopeStoreAsync(string? activeTenantId)
    {
        return Task.FromResult<ITenantStore>(_store);
    }
}
