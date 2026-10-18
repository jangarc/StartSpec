using Iam.Domain.Party;
using SharedKernel.Ports;

namespace Iam.Application.Parties;

/// <summary>{display name, birthdate, exact identifier tuple} — handed over by the Host with no further business validation (G3: business rules in Application).</summary>
public sealed record ProvisionPartyCommand(string DisplayName, DateOnly? DateOfBirth, string TypeCode, string Number);

/// <summary>Exposes the party identifier and the created/bound outcome only — never resolves other party data (G1: contract owned by the use case).</summary>
public sealed record ProvisionPartyResponse(Guid PartyId, bool CreatedNew);

/// <summary>
/// The provisioning decision per identity/party spec: exact identifier match binds the existing Party
/// (no duplicate), a near match creates a new one — fuzzy merging is structurally impossible because
/// only the tuple is ever compared. Raising PartyProvisioned publishes at-least-once (consumers are idempotent).
/// 建立/綁定判定：精確命中＝綁定既有 Party（不建立重複）、近似＝新建；僅元組比對故結構上禁模糊合併；事件 at-least-once 發布。
/// </summary>
public sealed class ProvisionPartyUseCase(IPartyStore store, IClock clock, IEventBus eventBus)
{
    public async Task<ProvisionPartyResponse> HandleAsync(ProvisionPartyCommand command, CancellationToken cancellationToken = default)
    {
        var tuple = PartyIdentifier.Create(command.TypeCode, command.Number);
        var existing = await store.FindExactAsync(tuple, cancellationToken);
        if (existing is not null)
        {
            return new ProvisionPartyResponse(existing.PartyId, CreatedNew: false);
        }

        var party = Party.Provision(command.DisplayName, command.DateOfBirth, [tuple], clock.UtcNow);
        await store.SaveAsync(party, cancellationToken);
        foreach (var domainEvent in party.PullDomainEvents())
        {
            await eventBus.PublishAsync(domainEvent, cancellationToken);
        }

        return new ProvisionPartyResponse(party.PartyId, CreatedNew: true);
    }
}
