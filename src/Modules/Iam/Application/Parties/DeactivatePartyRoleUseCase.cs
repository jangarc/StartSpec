using Iam.Domain.Party;
using SharedKernel.Ports;

namespace Iam.Application.Parties;

/// <summary>{tenant id, role type, exact identifier tuple} — deactivation target resolved by exact reach.</summary>
public sealed record DeactivatePartyRoleCommand(string TenantId, string RoleType, string TypeCode, string Number);

/// <summary>
/// Ends the Party's active (tenant, role) pairing; Party, identifiers, and consent history stay intact
/// (identity/party spec — deactivation is a status change, never an edit of attached facts).
/// 停用角色僅標記結束；Party、識別與同意歷史保持完整（屬狀態變更，絕不更改既有事實）。
/// </summary>
public sealed class DeactivatePartyRoleUseCase(IPartyStore store, IClock clock)
{
    public async Task HandleAsync(DeactivatePartyRoleCommand command, CancellationToken cancellationToken = default)
    {
        var party = await store.FindExactAsync(PartyIdentifier.Create(command.TypeCode, command.Number), cancellationToken)
            ?? throw new PartyNotFoundException(command.TypeCode, command.Number);

        party.DeactivateTenantRole(command.TenantId, command.RoleType, clock.UtcNow);
        await store.SaveAsync(party, cancellationToken);
    }
}
