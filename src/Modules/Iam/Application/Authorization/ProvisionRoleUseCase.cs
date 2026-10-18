using Iam.Domain.Authorization;

namespace Iam.Application.Authorization;

/// <summary>{tenant, name, permission grants} — handed over by the Host with no further business validation (G3: business rules in Application).</summary>
public sealed record ProvisionRoleCommand(string TenantId, string Name, IReadOnlyList<string> Permissions);

/// <summary>Exposes the created role id only (G1: contract owned by the use case).</summary>
public sealed record ProvisionRoleResponse(Guid RoleId);

/// <summary>
/// Role definition provisioning: the aggregate guards its own invariants (non-blank grants) and the
/// store enforces (tenant, name) uniqueness. No domain event is raised — the authorization spec
/// governs assignment and evaluation only.
/// 角色定義建立：聚合自我守護不變式；store 強制 (tenant, name) 唯一；本規格未要求事件——不自行發明。
/// </summary>
public sealed class ProvisionRoleUseCase(IRoleStoreScope roleStores)
{
    public async Task<ProvisionRoleResponse> HandleAsync(ProvisionRoleCommand command, CancellationToken cancellationToken = default)
    {
        var role = Role.Provision(command.TenantId, command.Name, command.Permissions);
        await using var store = roleStores.Member(role.TenantId);
        await store.SaveAsync(role, cancellationToken);
        return new ProvisionRoleResponse(role.RoleId);
    }
}
