using SharedKernel;

namespace Iam.Domain.Tenants;

/// <summary>
/// Tenant aggregate root. The tenant identifier is operator-supplied and unique platform-wide;
/// duplicate provisioning is rejected at the persistence boundary (unique key + conflict mapping,
/// task 2.2) while the aggregate guards its own invariants here.
/// 租戶聚合根：識別由操作員提供、平台上唯一；重複建立於持久層以唯一鍵拒絕。
/// </summary>
public sealed class Tenant
{
    private readonly List<Organization> _organizations = [];
    private readonly List<DomainEvent> _events = [];

    /// <summary>Operator-supplied unique identifier (the tenant_id of every scoped record).</summary>
    public string TenantId { get; }

    /// <summary>Display name of the tenant.</summary>
    public string Name { get; private set; }

    /// <summary>Initial (and later) organizations owned by this tenant — aggregate-contained.</summary>
    public IReadOnlyList<Organization> Organizations => _organizations;

    private Tenant(string tenantId, string name)
    {
        TenantId = tenantId;
        Name = name;
    }

    /// <summary>
    /// Provisions a tenant with its initial organization and raises <see cref="TenantProvisioned"/>
    /// carrying the ADR-009 standard envelope. Red-blue: behavior tests were authored first.
    /// 建立租戶＋初始組織，並發出帶標準信封的 <see cref="TenantProvisioned"/>。
    /// </summary>
    public static Tenant Provision(string tenantId, string tenantName, string initialOrganizationName, DateTimeOffset occurredAt)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant identifier is required.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(tenantName))
        {
            throw new ArgumentException("Tenant name is required.", nameof(tenantName));
        }

        if (string.IsNullOrWhiteSpace(initialOrganizationName))
        {
            throw new ArgumentException("Initial organization name is required.", nameof(initialOrganizationName));
        }

        var tenant = new Tenant(tenantId.Trim(), tenantName.Trim());
        var organization = new Organization(initialOrganizationName.Trim());
        organization.Tenant = tenant;
        tenant._organizations.Add(organization);
        tenant._events.Add(new TenantProvisioned
        {
            OccurredAt = occurredAt,
            TenantId = tenant.TenantId,
            TenantName = tenant.Name,
            OrganizationId = organization.OrganizationId,
        });

        return tenant;
    }

    /// <summary>Drains raised events for publication (exhausting — a second pull returns nothing).</summary>
    public IReadOnlyList<DomainEvent> PullDomainEvents()
    {
        var drained = _events.ToArray();
        _events.Clear();
        return drained;
    }
}
