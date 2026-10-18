using SharedKernel;

namespace Iam.Domain.Party;

/// <summary>
/// Platform-scoped identity aggregate for humans AND organizations — global-unique, deduplicated by
/// exact identifier tuple. Sits OUTSIDE tenant isolation as a guarded exception: the only sanctioned
/// cross-tenant reach is exact-match resolution at persistence level (tasks 3.2+; never listable).
/// Roles are played per tenant; consent records form an append-only timeline.
/// 平台級身分聚合：全域唯一、按精確證件元組去重；為租戶隔離的守衛例外；角色按租戶掛載；同意為 append-only 時間軸。
/// </summary>
public sealed class Party
{
    private readonly List<PartyIdentifier> _identifiers = [];
    private readonly List<PartyRole> _roles = [];
    private readonly List<ConsentRecord> _consents = [];
    private readonly List<DomainEvent> _events = [];

    /// <summary>Technical identity <c>party_id</c> (platform scope, no tenant_id).</summary>
    public Guid PartyId { get; }

    /// <summary>Display name — presentational only, never a match key.</summary>
    public string DisplayName { get; private set; }

    /// <summary>Birthdate (humans) — presentational only, never a match key.</summary>
    public DateOnly? DateOfBirth { get; private set; }

    public IReadOnlyList<PartyIdentifier> Identifiers => _identifiers;

    public IReadOnlyList<PartyRole> Roles => _roles;

    public IReadOnlyList<ConsentRecord> Consents => _consents;

    private Party(Guid partyId, string displayName, DateOnly? dateOfBirth)
    {
        PartyId = partyId;
        DisplayName = displayName;
        DateOfBirth = dateOfBirth;
    }

    /// <summary>
    /// Provisions a new Party for never-seen identifier tuples and raises <see cref="PartyProvisioned"/>.
    /// Deduplication vs persisted state is enforced by the store's exact-tuple lookup (tasks 3.2+);
    /// the aggregate guarantees tuple uniqueness within one provisioning.
    /// 建立新 Party（未見元組）並發出事件；對既有狀態的去重由持久層精確比對把守。
    /// </summary>
    public static Party Provision(string displayName, DateOnly? dateOfBirth, IReadOnlyList<PartyIdentifier> identifiers, DateTimeOffset occurredAt)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Party display name is required.", nameof(displayName));
        }

        if (identifiers is null || identifiers.Count == 0)
        {
            throw new ArgumentException("At least one identifier tuple is required.", nameof(identifiers));
        }

        var seen = new HashSet<PartyIdentifier>();
        foreach (var identifier in identifiers)
        {
            if (!seen.Add(identifier))
            {
                throw new ArgumentException("Identifier tuples must be unique within one provisioning.", nameof(identifiers));
            }
        }

        var party = new Party(Guid.NewGuid(), displayName.Trim(), dateOfBirth);
        party._identifiers.AddRange(identifiers);
        party._events.Add(new PartyProvisioned
        {
            OccurredAt = occurredAt,
            TenantId = null,
            PartyId = party.PartyId,
            DisplayName = party.DisplayName,
        });

        return party;
    }

    /// <summary>
    /// Attaches a role the Party plays within the given tenant (multi-role, multi-tenant per spec).
    /// Binding targets the EXISTING Party — the caller resolved the exact tuple match before this call;
    /// a duplicated active (tenant, role) pairing is rejected.
    /// 對既有 Party 加掛租戶角色；同租戶同角色之活動配對重複即拒絕。
    /// </summary>
    public PartyRole AttachTenantRole(string tenantId, string roleType, DateTimeOffset attachedAt)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant identifier is required.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(roleType))
        {
            throw new ArgumentException("Role type is required.", nameof(roleType));
        }

        tenantId = tenantId.Trim();
        roleType = roleType.Trim();

        if (_roles.Any(role => role.TenantId == tenantId && role.RoleType == roleType && role.IsActive))
        {
            throw new InvalidOperationException($"An active role '{roleType}' already exists for tenant '{tenantId}'.");
        }

        var role = new PartyRole(tenantId, roleType, attachedAt);
        _roles.Add(role);
        return role;
    }

    /// <summary>Marks the active role ended; the Party, its identifiers, and consent history stay intact.</summary>
    public void DeactivateTenantRole(string tenantId, string roleType, DateTimeOffset at)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant identifier is required.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(roleType))
        {
            throw new ArgumentException("Role type is required.", nameof(roleType));
        }

        var role = _roles.FirstOrDefault(candidate
            => candidate.TenantId == tenantId.Trim() && candidate.RoleType == roleType.Trim() && candidate.IsActive)
            ?? throw new InvalidOperationException($"No active role '{roleType}' exists for tenant '{tenantId}'.");

        role.Deactivate(at);
    }

    /// <summary>Records a granted consent; always a NEW record (append-only).</summary>
    public ConsentRecord GrantConsent(string tenantId, string purpose, DateTimeOffset occurredAt)
    {
        return AppendConsent(ConsentKind.Grant, tenantId, purpose, occurredAt);
    }

    /// <summary>
    /// Appends a revocation record; the original grant record is retained untouched (identity/party spec).
    /// 撤銷以新增記錄為之；原同意記錄原樣保留。
    /// </summary>
    public ConsentRecord RevokeConsent(string tenantId, string purpose, DateTimeOffset occurredAt)
    {
        return AppendConsent(ConsentKind.Revoke, tenantId, purpose, occurredAt);
    }

    private ConsentRecord AppendConsent(ConsentKind kind, string tenantId, string purpose, DateTimeOffset occurredAt)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant identifier is required.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(purpose))
        {
            throw new ArgumentException("Consent purpose is required.", nameof(purpose));
        }

        var record = new ConsentRecord(Guid.NewGuid(), kind, tenantId.Trim(), purpose.Trim(), occurredAt);
        _consents.Add(record);
        return record;
    }

    /// <summary>Drains raised events for publication (exhausting — a second pull returns nothing).</summary>
    public IReadOnlyList<DomainEvent> PullDomainEvents()
    {
        var drained = _events.ToArray();
        _events.Clear();
        return drained;
    }
}
