namespace Iam.Application.Authorization;

/// <summary>
/// Scope factory for role stores: roles are member-only — every store face is born with the resolved
/// tenant (construction-level filter, fail-closed); no platform face exists, so role data is never
/// reachable without a tenant scope.
/// 角色範圍工廠：一律攜已解析租戶構造（fail-closed）；無平台面，租戶範圍外絕不可達。
/// </summary>
public interface IRoleStoreScope
{
    /// <summary>Binds the store face to the resolved tenant.</summary>
    IRoleStore Member(string tenantId);
}
