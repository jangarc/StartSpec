namespace Iam.Domain.Tenants;

/// <summary>
/// Organization member of the <see cref="Tenant"/> aggregate: created with the tenant (initial record),
/// extended by later tasks. Identity is a fresh guid.
/// 組織：隨租戶建立初始紀錄，識別為新產生 guid。
/// </summary>
public sealed class Organization
{
    /// <summary>Technical identity <c>organization_id</c>.</summary>
    public Guid OrganizationId { get; }

    /// <summary>Organization display name.</summary>
    public string Name { get; private set; }

    /// <summary>EF navigation back to the owning tenant (maps to the tenant_id column via relationship).</summary>
    public Tenant Tenant { get; internal set; } = null!;

    internal Organization(string name)
    {
        OrganizationId = Guid.NewGuid();
        Name = name;
    }
}
