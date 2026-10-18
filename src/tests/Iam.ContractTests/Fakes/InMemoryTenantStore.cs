using Iam.Application.Tenants;
using Iam.Domain.Tenants;

namespace Iam.ContractTests.Fakes;

/// <summary>
/// In-memory <see cref="ITenantStore"/> fake for the persistence seam CONTRACT LEG (module-owned port:
/// the fake stays in module tests per ADR-010). Semantics mirror the wired store: scoped listing,
/// denied-as-not-found single lookup, cursor pagination without totals, duplicate-save conflict.
/// 模組自有 port 的 fake 留在模組測試；語意與 EF 實作一致。
/// </summary>
public sealed class InMemoryTenantStore : ITenantStore
{
    private readonly Dictionary<string, TenantSnapshot> _tenants = [];

    /// <summary>Save calls observed (use-case dedup verification hook).</summary>
    public int SavedCount { get; private set; }

    public Task<bool> TenantExistsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_tenants.ContainsKey(tenantId));
    }

    public Task SaveAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        SavedCount++;
        if (_tenants.ContainsKey(tenant.TenantId))
        {
            throw new TenantConflictException(tenant.TenantId);
        }

        _tenants[tenant.TenantId] = new TenantSnapshot(
            tenant.TenantId,
            tenant.Name,
            tenant.Organizations.Select(organization => new OrganizationEntry(organization.OrganizationId, organization.Name)).ToList());

        return Task.CompletedTask;
    }

    public Task<OrganizationEntry?> FindOrganizationAsync(string tenantId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        if (!_tenants.TryGetValue(tenantId, out var snapshot))
        {
            return Task.FromResult<OrganizationEntry?>(null);
        }

        var entry = snapshot.Organizations.FirstOrDefault(organization => organization.OrganizationId == organizationId);
        return Task.FromResult(entry);
    }

    public Task<OrganizationPage> ListOrganizationsAsync(string tenantId, int limit = 50, string? cursor = null, CancellationToken cancellationToken = default)
    {
        if (limit < 1 || limit > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "Page limit must be between 1 and 200.");
        }

        var scoped = _tenants.TryGetValue(tenantId, out var snapshot)
            ? snapshot.Organizations
            : new List<OrganizationEntry>();

        var window = scoped.OrderBy(organization => organization.OrganizationId);
        if (cursor is not null)
        {
            var cursorId = Guid.Parse(cursor);
            window = window.Where(organization => organization.OrganizationId > cursorId).OrderBy(organization => organization.OrganizationId);
        }

        var fetched = window.Take(limit + 1).ToList();
        var hasNext = fetched.Count > limit;

        return Task.FromResult(new OrganizationPage(fetched.Take(limit).ToList(), hasNext));
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    private sealed record TenantSnapshot(string TenantId, string Name, List<OrganizationEntry> Organizations);
}
