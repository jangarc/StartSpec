using Iam.Application.Users;
using Microsoft.EntityFrameworkCore;

namespace Iam.Infrastructure.Persistence;

/// <summary>
/// Scope factory over shared <see cref="IamDbContext"/> options: users are member-only — every store
/// face is born with the resolved tenant (construction-level filter, fail-closed); no platform face
/// exists, so user data is never reachable without a tenant scope.
/// 範圍工廠：users 僅成員面——一律攜已解析租戶構造（fail-closed）；無平台面，絕無租戶範圍外可達。
/// </summary>
public sealed class UserStoreScope(DbContextOptions<IamDbContext> options) : IUserStoreScope
{
    public IUserStore Member(string tenantId)
    {
        return new EfUserStore(options, tenantId);
    }
}
