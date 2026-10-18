using Iam.Domain.Tenants;

namespace Iam.Application.Tenants;

/// <summary>
/// Persistence port for the tenant aggregate (Application-owned contract; implemented by module
/// Infrastructure). Listing is tenant-scoped BY PARAMETER (the authenticated, server-resolved tenant),
/// so cross-tenant data is never addressable; the EF global filter is the construction-level backstop.
/// The store owns its persistence resources: call sites SHALL dispose.
/// 租戶持久 port（契約歸 Application）；清單以參數級租戶範圍過濾；呼叫端須 dispose。
/// </summary>
public interface ITenantStore : IAsyncDisposable
{
    /// <summary>True when the tenant identifier is already taken.</summary>
    Task<bool> TenantExistsAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>Persists the aggregate (tenant + organizations). Duplicate identifiers SHALL conflict.</summary>
    Task SaveAsync(Tenant tenant, CancellationToken cancellationToken = default);

    /// <summary>Single lookup; cross-tenant or unknown ids return NOTHING (denied-as-not-found semantics).</summary>
    Task<OrganizationEntry?> FindOrganizationAsync(string tenantId, Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>Cursor-paginated organization listing; exposes a has-next indicator only, never totals.</summary>
    Task<OrganizationPage> ListOrganizationsAsync(string tenantId, int limit = 50, string? cursor = null, CancellationToken cancellationToken = default);
}

/// <summary>Organization projection exposed by the store (no domain entity leaks).</summary>
public sealed record OrganizationEntry(Guid OrganizationId, string Name);

/// <summary>Cursor page result: has-next indicator only — no total count (identity/tenant spec).</summary>
public sealed record OrganizationPage(IReadOnlyList<OrganizationEntry> Items, bool HasNext);
