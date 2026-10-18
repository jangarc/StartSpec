using Iam.Application.Tenants;
using Microsoft.EntityFrameworkCore;

namespace Iam.Infrastructure.Persistence;

/// <summary>
/// Scope factory over shared <see cref="IamDbContext"/> options: the platform entry binds an unscoped
/// context, the member path binds the resolved tenant (construction-level filter, fail-closed).
/// 範圍工廠：平台入口＝無範圍 context；成員路徑＝已解析租戶之 scoped context（fail-closed）。
/// </summary>
public sealed class TenantStoreScope(DbContextOptions<IamDbContext> options) : ITenantStoreScope
{
    public ITenantStore Platform()
    {
        return new EfTenantStore(options, null);
    }

    public ITenantStore Member(string tenantId)
    {
        return new EfTenantStore(options, tenantId);
    }
}
