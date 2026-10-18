using Iam.Application.Authorization;
using Iam.Domain.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Iam.Infrastructure.Persistence;

/// <summary>
/// EF-backed <see cref="IRoleStore"/> implementation. Member-scoped BY CONSTRUCTION: the context is
/// born with the resolved tenant (fail-closed global filter) plus an explicit tenant_id predicate.
/// The store owns its context; call sites SHALL dispose.
/// 角色 EF 持久實作：構造級租戶範圍（fail-closed 全域過濾＋明式 tenant_id 謂詞）；呼叫端須 dispose。
/// </summary>
public sealed class EfRoleStore(DbContextOptions<IamDbContext> options, string activeTenantId) : IRoleStore, IAsyncDisposable
{
    private readonly IamDbContext _db = new(options, activeTenantId);

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    public Task<Role?> FindAsync(string tenantId, Guid roleId, CancellationToken cancellationToken = default) =>
        _db.Roles
            .Where(role => role.TenantId == tenantId && role.RoleId == roleId)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Role>> ListAsync(string tenantId, CancellationToken cancellationToken = default) =>
        await _db.Roles
            .Where(role => role.TenantId == tenantId)
            .ToListAsync(cancellationToken);

    public async Task SaveAsync(Role role, CancellationToken cancellationToken = default)
    {
        // (tenant, name) uniqueness is a store-level backstop for the concurrent-provision race; the
        // aggregate cannot see concurrent rows.
        _db.Add(role);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
