namespace Iam.Domain.Authorization;

/// <summary>
/// Tenant-scoped role definition: a named permission set owned by exactly one tenant. Grants are
/// immutable after provisioning; role-edit workflows are not specified by the authorization spec —
/// record the need instead of inventing them.
/// 租戶級角色定義：權限集建立後不可變；角色編輯流程規格未定義——僅記錄需求、不自行發明。
/// </summary>
public sealed class Role
{
    /// <summary>Technical identity <c>role_id</c>, unique platform-wide.</summary>
    public Guid RoleId { get; private set; }

    /// <summary>Owning tenant of the role definition (the tenant_id of every scoped record).</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Role name; unique within the owning tenant (enforced by the policy store).</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Frozen permission grants (non-blank, deduplicated).</summary>
    public string[] Permissions { get; private set; } = [];

    // Parameterless shape for EF materialization only — domain code must use Provision.
    private Role()
    {
    }

    private Role(Guid roleId, string tenantId, string name, string[] permissions)
    {
        RoleId = roleId;
        TenantId = tenantId;
        Name = name;
        Permissions = permissions;
    }

    /// <summary>Creates the role definition; at least one non-blank permission grant is required.</summary>
    public static Role Provision(string tenantId, string name, IEnumerable<string> permissions)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant identifier is required.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Role name is required.", nameof(name));
        }

        var grants = permissions.Distinct().ToList();
        if (grants.Count == 0 || grants.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("At least one non-blank permission grant is required.", nameof(permissions));
        }

        return new Role(Guid.NewGuid(), tenantId.Trim(), name.Trim(), [.. grants]);
    }
}
