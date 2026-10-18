using Iam.Domain.Party;
using SharedKernel.Ports;

namespace Iam.Application.Parties;

/// <summary>{tenant id, stated purpose, exact identifier tuple} — consent rides the party's per-timeline identity.</summary>
public sealed record GrantConsentCommand(string TenantId, string Purpose, string TypeCode, string Number);

/// <summary>
/// Records a NEW granted consent on the party's append-only timeline (timestamps bound to the record;
/// existing records are never edited).
/// 於 append-only 時間軸新增一筆同意記錄；記錄帶時間戳且既有記錄絕不修改。
/// </summary>
public sealed class GrantConsentUseCase(IPartyStore store, IClock clock)
{
    public async Task HandleAsync(GrantConsentCommand command, CancellationToken cancellationToken = default)
    {
        var party = await store.FindExactAsync(PartyIdentifier.Create(command.TypeCode, command.Number), cancellationToken)
            ?? throw new PartyNotFoundException(command.TypeCode, command.Number);

        party.GrantConsent(command.TenantId, command.Purpose, clock.UtcNow);
        await store.SaveAsync(party, cancellationToken);
    }
}
