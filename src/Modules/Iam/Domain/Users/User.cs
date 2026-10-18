using SharedKernel;

namespace Iam.Domain.Users;

/// <summary>
/// Per-tenant login account bound to the global Party identity. Born active (raising UserProvisioned);
/// disabling is a one-way transition that raises UserDisabled. Re-enabling is not specified by the
/// identity/user-auth spec — record the need instead of inventing it.
/// 按租戶掛載的登入帳號、綁定全域 Party 身分；建立即啟用並發 UserProvisioned；停用為單向轉換並發
/// UserDisabled；恢復啟用規格未定義——僅記錄需求，不自行發明。
/// </summary>
public sealed class User
{
    private readonly List<DomainEvent> _events = [];

    /// <summary>Technical identity <c>user_id</c>.</summary>
    public Guid UserId { get; }

    /// <summary>Owning tenant of the login account (the tenant_id of every scoped record).</summary>
    public string TenantId { get; }

    /// <summary>Login name; unique within the owning tenant.</summary>
    public string LoginName { get; }

    /// <summary>The platform Party identity this account belongs to.</summary>
    public Guid PartyId { get; }

    /// <summary>Active accounts authenticate; disabled accounts are rejected at sign-in and refresh.</summary>
    public bool IsActive { get; private set; }

    private User(Guid userId, string tenantId, string loginName, Guid partyId)
    {
        UserId = userId;
        TenantId = tenantId;
        LoginName = loginName;
        PartyId = partyId;
    }

    /// <summary>Creates the user in active state bound to the resolved Party and raises UserProvisioned.</summary>
    public static User Provision(string tenantId, string loginName, Guid partyId, DateTimeOffset occurredAt)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant identifier is required.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(loginName))
        {
            throw new ArgumentException("Login name is required.", nameof(loginName));
        }

        if (partyId == Guid.Empty)
        {
            throw new ArgumentException("Party binding is required.", nameof(partyId));
        }

        var user = new User(Guid.NewGuid(), tenantId.Trim(), loginName.Trim(), partyId) { IsActive = true };
        user._events.Add(new UserProvisioned
        {
            OccurredAt = occurredAt,
            TenantId = user.TenantId,
            UserId = user.UserId,
            PartyId = user.PartyId,
            LoginName = user.LoginName,
        });

        return user;
    }

    /// <summary>
    /// One-way lifecycle transition; an already-disabled user rejects the transition again.
    /// 單向生命週期轉換；已停用再停用即拒絕。
    /// </summary>
    public void Disable(DateTimeOffset at)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException($"User '{LoginName}' is already disabled.");
        }

        IsActive = false;
        _events.Add(new UserDisabled
        {
            OccurredAt = at,
            TenantId = TenantId,
            UserId = UserId,
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
