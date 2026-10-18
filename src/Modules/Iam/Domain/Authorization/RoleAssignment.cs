using SharedKernel;

namespace Iam.Domain.Authorization;

/// <summary>
/// Per-tenant assignment of a user to a tenant role. Cross-tenant references are structurally
/// impossible: issuing requires the role aggregate itself. Revocation is a one-way transition
/// raising <see cref="RoleRevoked"/>; a revoked assignment never re-enables.
/// 按租戶的角色指派：經聚合驗證使跨租戶指派結構上不可能；撤銷為單向轉換並發 RoleRevoked，撤銷後不重生。
/// </summary>
public sealed class RoleAssignment
{
    private readonly List<DomainEvent> _events = [];

    /// <summary>Technical identity <c>assignment_id</c>.</summary>
    public Guid AssignmentId { get; private set; }

    /// <summary>Owning tenant of the assignment (the role's tenant — verified at issue time).</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Assigned per-tenant user identity.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Assigned role technical identity.</summary>
    public Guid RoleId { get; private set; }

    /// <summary>UTC time the assignment was recorded.</summary>
    public DateTimeOffset IssuedAt { get; private set; }

    /// <summary>Revoked assignments never authenticate a permission again.</summary>
    public bool IsRevoked { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    // Parameterless shape for EF materialization only — domain code must use Issue.
    private RoleAssignment()
    {
    }

    private RoleAssignment(
        Guid assignmentId,
        string tenantId,
        Guid userId,
        Guid roleId,
        DateTimeOffset issuedAt,
        bool isRevoked,
        DateTimeOffset? revokedAt)
    {
        AssignmentId = assignmentId;
        TenantId = tenantId;
        UserId = userId;
        RoleId = roleId;
        IssuedAt = issuedAt;
        IsRevoked = isRevoked;
        RevokedAt = revokedAt;
    }

    /// <summary>Issues the assignment inside the role's own tenant; a foreign role is rejected.</summary>
    public static RoleAssignment Issue(Role role, string tenantId, Guid userId, DateTimeOffset issuedAt)
    {
        if (role is null)
        {
            throw new ArgumentException("Role definition is required.", nameof(role));
        }

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant identifier is required.", nameof(tenantId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User binding is required.", nameof(userId));
        }

        if (role.TenantId != tenantId.Trim())
        {
            throw new ArgumentException(
                $"Role belongs to tenant '{role.TenantId}'; the assignment was scoped to '{tenantId.Trim()}'.",
                nameof(role));
        }

        return new RoleAssignment(Guid.NewGuid(), role.TenantId, userId, role.RoleId, issuedAt, false, null);
    }

    /// <summary>
    /// One-way lifecycle transition; an already-revoked assignment rejects the transition again.
    /// 單向生命週期轉換；已撤銷再撤銷即拒絕。
    /// </summary>
    public void Revoke(DateTimeOffset at)
    {
        if (IsRevoked)
        {
            throw new InvalidOperationException($"Assignment to role '{RoleId}' is already revoked.");
        }

        IsRevoked = true;
        RevokedAt = at;
        _events.Add(new RoleRevoked
        {
            OccurredAt = at,
            TenantId = TenantId,
            UserId = UserId,
            RoleId = RoleId,
        });
    }

    /// <summary>Drains raised events for publication (exhausting — a second pull returns nothing).</summary>
    public IReadOnlyList<DomainEvent> PullDomainEvents()
    {
        var drained = _events.ToArray();
        _events.Clear();
        return drained;
    }
}
