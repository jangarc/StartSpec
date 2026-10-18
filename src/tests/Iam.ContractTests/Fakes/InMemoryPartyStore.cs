using Iam.Application.Parties;
using Iam.Domain.Party;

namespace Iam.ContractTests.Fakes;

/// <summary>
/// In-memory IPartyStore fake (same conflict shape asEfPartyStore: cross-party claim rejects).
/// Instances are reused rather than copied — fake legs assert save + exact-reach semantics only.
/// 契約 fake：物件引用重用，提供與 EF 實作相同的精確觸達與衝突語意。
/// </summary>
public sealed class InMemoryPartyStore : IPartyStore
{
    private readonly Dictionary<Guid, Party> _parties = [];

    public Task<Party?> FindExactAsync(PartyIdentifier tuple, CancellationToken cancellationToken = default)
    {
        var hit = _parties.Values.FirstOrDefault(party
            => party.Identifiers.Any(identifier => identifier.TypeCode == tuple.TypeCode && identifier.Number == tuple.Number));

        return Task.FromResult(hit);
    }

    public Task SaveAsync(Party party, CancellationToken cancellationToken = default)
    {
        foreach (var stored in _parties.Values)
        {
            if (stored.PartyId == party.PartyId)
            {
                continue;
            }

            foreach (var tuple in party.Identifiers)
            {
                if (stored.Identifiers.Any(identifier => identifier.TypeCode == tuple.TypeCode && identifier.Number == tuple.Number))
                {
                    throw new PartyIdentifierConflictException(tuple.TypeCode, tuple.Number);
                }
            }
        }

        _parties[party.PartyId] = party;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
