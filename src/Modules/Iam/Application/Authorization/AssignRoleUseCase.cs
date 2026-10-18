using Iam.Domain.Authorization;
using SharedKernel.Ports;

namespace Iam.Application.Authorization;

/// <summary>{tenant, user, role} — handed over by the Host with no further business validation (G3: business rules in Application).</summary>
public sealed record AssignRoleCommand(string TenantId, Guid UserId, Guid RoleId);

/// <summary>Exposes the recorded assignment id only (G1: contract owned by the use case).</summary>
public sealed record AssignRoleResponse(Guid AssignmentId);

/// <summary>
/// Assignment per identity/authorization spec: the role must resolve INSIDE the acting tenant's own
/// scoped store — an unknown or foreign-tenant role is unreachable, so the call rejects and records
/// nothing (denied-as-not-found). After recording, the node's enforcer snapshot is torn down so the
/// fresh grant is immediately visible on this node (grant propagation across nodes is out of scope —
/// no event is spec'd for grants).
/// 角色指派：角色須在作用租戶自身的 scoped store 內解析——未知或他租戶角色不可達，故拒絕且不記錄。
/// 記錄後撕除本節點執行器快照，使新授權即時可見（跨節點授權傳播超出本期規格——不發明事件）。
/// </summary>
public sealed class AssignRoleUseCase(IRoleStoreScope roleStores, IRoleAssignmentStoreScope assignmentStores, IClock clock, PolicyEnforcer enforcer)
{
    public async Task<AssignRoleResponse> HandleAsync(AssignRoleCommand command, CancellationToken cancellationToken = default)
    {
        await using var roleStore = roleStores.Member(command.TenantId);
        var role = await roleStore.FindAsync(command.TenantId, command.RoleId, cancellationToken)
            ?? throw new RoleUnknownException(command.TenantId, command.RoleId);

        var assignment = RoleAssignment.Issue(role, command.TenantId, command.UserId, clock.UtcNow);
        await using var assignmentStore = assignmentStores.Member(command.TenantId);
        await assignmentStore.SaveAsync(assignment, cancellationToken);
        enforcer.Invalidate(command.TenantId);
        return new AssignRoleResponse(assignment.AssignmentId);
    }
}
