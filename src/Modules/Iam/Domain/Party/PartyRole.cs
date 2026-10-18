namespace Iam.Domain.Party;

/// <summary>
/// A role a Party plays within one tenant. Deactivation records WHEN it ended and keeps the Party
/// and its history intact; it is a status change, not an edit of attached facts.
/// 租戶內角色：停用只標記結束時點並保留 Party 與其歷史；屬狀態變更而非修改既有事實。
/// </summary>
public sealed class PartyRole
{
    /// <summary>Technical identity <c>party_role_id</c>.</summary>
    public Guid PartyRoleId { get; }

    /// <summary>The tenant whose scope this role plays in.</summary>
    public string TenantId { get; }

    /// <summary>Role code (e.g. employee, examinee) — belongs to per-tenant role catalogs (task 5.x).</summary>
    public string RoleType { get; }

    public DateTimeOffset AttachedAt { get; }

    public DateTimeOffset? DeactivatedAt { get; private set; }

    public bool IsActive => DeactivatedAt is null;

    internal PartyRole(string tenantId, string roleType, DateTimeOffset attachedAt)
    {
        PartyRoleId = Guid.NewGuid();
        TenantId = tenantId;
        RoleType = roleType;
        AttachedAt = attachedAt;
    }

    internal void Deactivate(DateTimeOffset at)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException($"Role '{RoleType}' of tenant '{TenantId}' is already deactivated.");
        }

        DeactivatedAt = at;
    }
}
