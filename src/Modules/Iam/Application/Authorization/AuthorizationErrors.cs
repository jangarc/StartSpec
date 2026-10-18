namespace Iam.Application.Authorization;

/// <summary>
/// Raised when a role is unreachable in the acting tenant — an unknown role and a foreign-tenant
/// role share this single shape (denied-as-not-found; no cross-tenant probing oracle).
/// 角色不可達例外：未知角色與他租戶角色共用同一錯誤形（denied-as-not-found，不可反推他租戶）。
/// </summary>
public sealed class RoleUnknownException(string tenantId, Guid roleId)
    : Exception($"Role '{roleId}' is not reachable in tenant '{tenantId}'.");

/// <summary>
/// Raised when a role assignment is unreachable in the acting tenant (denied-as-not-found).
/// 角色指派不可達例外（denied-as-not-found）。
/// </summary>
public sealed class RoleAssignmentUnknownException(string tenantId, Guid assignmentId)
    : Exception($"Role assignment '{assignmentId}' is not reachable in tenant '{tenantId}'.");
