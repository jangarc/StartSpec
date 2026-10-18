using Iam.Application.Parties;
using Iam.ContractTests.Fakes;
using Iam.ContractTests.Parties;
using Xunit;

namespace Iam.ContractTests.Parties.FakeLegs;

/// <summary>Fake leg: one shared in-memory store per test instance; reference reuse matches save semantics.</summary>
public sealed class InMemoryPartyStoreLeg : PartyStoreContractBase
{
    private readonly InMemoryPartyStore _store = new();

    protected override Task<IPartyStore> CreateStoreAsync()
    {
        return Task.FromResult<IPartyStore>(_store);
    }
}
