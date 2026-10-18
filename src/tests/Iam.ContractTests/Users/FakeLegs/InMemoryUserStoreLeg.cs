using Iam.Application.Parties;
using Iam.Application.Users;
using Iam.ContractTests.Fakes;
using Iam.ContractTests.Users;

namespace Iam.ContractTests.Users.FakeLegs;

/// <summary>
/// Fake leg: one shared in-memory user store per test instance (scope carried at parameter level);
/// party anchors come from per-call in-memory party stores. Reference reuse matches save semantics.
/// Fake 腿：(tenant, login) 參數位範圍；party 錨點各事實獨立；引用重用符合保存語意。
/// </summary>
public sealed class InMemoryUserStoreLeg : UserStoreContractBase
{
    private readonly Dictionary<string, InMemoryUserStore> _stores = [];

    protected override Task<IUserStore> CreateUserStoreAsync(string activeTenantId)
    {
        if (!_stores.TryGetValue(activeTenantId, out var store))
        {
            store = new InMemoryUserStore();
            _stores[activeTenantId] = store;
        }

        return Task.FromResult<IUserStore>(store);
    }

    protected override Task<IPartyStore> CreatePartyStoreAsync()
    {
        return Task.FromResult<IPartyStore>(new InMemoryPartyStore());
    }
}
