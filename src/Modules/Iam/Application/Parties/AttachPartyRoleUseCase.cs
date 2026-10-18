using Iam.Domain.Party;
using SharedKernel.Ports;

namespace Iam.Application.Parties;

/// <summary>{tenant id, role type, exact identifier tuple} — Host hands over identity only (G2: the enforcer/decisions live in Application).</summary>
public sealed record AttachPartyRoleCommand(string TenantId, string RoleType, string TypeCode, string Number);

/// <summary>Exposes the bound party identifier only — the caller resolved the party by exact tuple, so nothing else may travel back (identity/party spec).</summary>
public sealed record AttachPartyRoleResponse(Guid PartyId);

/// <summary>
/// Attaches a per-tenant role to the EXISTING Party carrying the exact tuple; a miss is an error, not
/// a silent creation (provisioning owns creation; binding only ever targets a resolved party).
/// 認證綁定：只對精確元組命中的既有 Party 加掛角色；未命中＝錯誤，不得靜默新建（建立權歸 provisioning）。
/// </summary>
public sealed class AttachPartyRoleUseCase(IPartyStore store, IClock clock)
{
    public async Task<AttachPartyRoleResponse> HandleAsync(AttachPartyRoleCommand command, CancellationToken cancellationToken = default)
    {
        var party = await store.FindExactAsync(PartyIdentifier.Create(command.TypeCode, command.Number), cancellationToken)
            ?? throw new PartyNotFoundException(command.TypeCode, command.Number);

        party.AttachTenantRole(command.TenantId, command.RoleType, clock.UtcNow);
        await store.SaveAsync(party, cancellationToken);

        return new AttachPartyRoleResponse(party.PartyId);
    }
}
