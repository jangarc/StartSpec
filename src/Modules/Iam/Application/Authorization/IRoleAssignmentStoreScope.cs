namespace Iam.Application.Authorization;

/// <summary>
/// Scope factory for role-assignment stores: assignments are member-only — every store face is born
/// with the resolved tenant (construction-level filter, fail-closed); no platform face exists.
/// 指派範圍工廠：一律攜已解析租戶構造（fail-closed）；無平台面。
/// </summary>
public interface IRoleAssignmentStoreScope
{
    /// <summary>Binds the store face to the resolved tenant.</summary>
    IRoleAssignmentStore Member(string tenantId);
}
