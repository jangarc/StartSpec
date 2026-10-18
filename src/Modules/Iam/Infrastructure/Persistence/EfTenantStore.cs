using Iam.Application.Tenants;
using Iam.Domain.Tenants;
using Microsoft.EntityFrameworkCore;

namespace Iam.Infrastructure.Persistence;

/// <summary>
/// EF-backed <see cref="ITenantStore"/> implementation. Queries carry the tenant scope explicitly AND
/// rely on the DbContext global filter; duplicate identifiers surface as <see cref="TenantConflictException"/>
/// (unique key). The store owns its context; call sites dispose.
/// EF 持久實作：顯式範圍＋全域過濾雙保險；唯一鍵衝突譯為 TenantConflictException；呼叫端 dispose。
/// </summary>
public sealed class EfTenantStore : ITenantStore
{
    private readonly IamDbContext db;

    public EfTenantStore(DbContextOptions<IamDbContext> options, string? activeTenantId)
    {
        db = activeTenantId is null ? new IamDbContext(options) : new IamDbContext(options, activeTenantId);
    }

    public async ValueTask DisposeAsync()
    {
        await db.DisposeAsync();
    }

    public Task<bool> TenantExistsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        return db.Tenants.AnyAsync(tenant => tenant.TenantId == tenantId, cancellationToken);
    }

    public async Task SaveAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        // EF fails a repeated Add (same key, one context) at attach time, so this pre-check yields the
        // clean conflict shape; the unique key below remains the race-condition backstop.
        if (await db.Tenants.AnyAsync(candidate => candidate.TenantId == tenant.TenantId, cancellationToken))
        {
            throw new TenantConflictException(tenant.TenantId);
        }

        db.Add(tenant);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true
            || exception.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new TenantConflictException(tenant.TenantId, exception);
        }
    }

    public async Task<OrganizationEntry?> FindOrganizationAsync(string tenantId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var entry = await db.Organizations
            .Where(organization => organization.OrganizationId == organizationId && EF.Property<string>(organization, "tenant_id") == tenantId)
            .Select(organization => new { organization.OrganizationId, organization.Name })
            .SingleOrDefaultAsync(cancellationToken);

        return entry is null ? null : new OrganizationEntry(entry.OrganizationId, entry.Name);
    }

    public async Task<OrganizationPage> ListOrganizationsAsync(string tenantId, int limit = 50, string? cursor = null, CancellationToken cancellationToken = default)
    {
        if (limit < 1 || limit > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "Page limit must be between 1 and 200.");
        }

        var window = db.Organizations
            .Where(organization => EF.Property<string>(organization, "tenant_id") == tenantId)
            .OrderBy(organization => organization.OrganizationId);

        if (cursor is not null)
        {
            var cursorId = Guid.Parse(cursor);
            window = window.Where(organization => organization.OrganizationId > cursorId).OrderBy(organization => organization.OrganizationId);
        }

        var fetched = await window
            .Take(limit + 1)
            .Select(organization => new { organization.OrganizationId, organization.Name })
            .ToListAsync(cancellationToken);

        var hasNext = fetched.Count > limit;
        var items = fetched.Take(limit)
            .Select(organization => new OrganizationEntry(organization.OrganizationId, organization.Name))
            .ToList();

        return new OrganizationPage(items, hasNext);
    }
}
