namespace Iam.Domain.Authorization;

/// <summary>
/// Tenant-scoped permission decision over a (role definitions, assignments) snapshot. Deny-by-default:
/// the evaluated tenant must equal the user's own tenant, an assignment must be live (never revoked),
/// and the referenced role must be the member tenant's own definition granting the permission.
/// 租戶級判定：一律 deny-by-default——作用租戶＝所屬租戶、指派存續、且角色須為成員租戶自身定義並授予權限。
/// </summary>
public static class TenantPermissionDecider
{
    public static bool Decide(
        string evaluatedTenantId,
        string permission,
        string userTenantId,
        Guid userId,
        IReadOnlyList<Role> roleDefinitions,
        IReadOnlyList<RoleAssignment> assignments)
    {
        if (evaluatedTenantId != userTenantId)
        {
            return false;
        }

        var liveRoleIds = assignments
            .Where(assignment => !assignment.IsRevoked
                && assignment.TenantId == userTenantId
                && assignment.UserId == userId)
            .Select(assignment => assignment.RoleId)
            .ToArray();
        if (liveRoleIds.Length == 0)
        {
            return false;
        }

        return roleDefinitions.Any(role =>
            role.TenantId == userTenantId
            && liveRoleIds.Contains(role.RoleId)
            && role.Permissions.Contains(permission));
    }
}
