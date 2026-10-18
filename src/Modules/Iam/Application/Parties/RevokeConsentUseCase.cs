using Iam.Domain.Party;
using SharedKernel.Ports;

namespace Iam.Application.Parties;

/// <summary>{tenant id, stated purpose, exact identifier tuple} — revocation always appends.</summary>
public sealed record RevokeConsentCommand(string TenantId, string Purpose, string TypeCode, string Number);

/// <summary>
/// Appends a revocation record; the original grant record is retained untouched (identity/party spec).
/// 新增撤銷記錄；原同意記錄原樣保留。
/// </summary>
public sealed class RevokeConsentUseCase(IPartyStore store, IClock clock)
{
    public async Task HandleAsync(RevokeConsentCommand command, CancellationToken cancellationToken = default)
    {
        var party = await store.FindExactAsync(PartyIdentifier.Create(command.TypeCode, command.Number), cancellationToken)
            ?? throw new PartyNotFoundException(command.TypeCode, command.Number);

        party.RevokeConsent(command.TenantId, command.Purpose, clock.UtcNow);
        await store.SaveAsync(party, cancellationToken);
    }
}
