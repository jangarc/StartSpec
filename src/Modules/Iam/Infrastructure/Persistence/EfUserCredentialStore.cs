using Iam.Application.Users;
using Microsoft.EntityFrameworkCore;

namespace Iam.Infrastructure.Persistence;

/// <summary>
/// EF-backed <see cref="IUserCredentialStore"/>: password hashes live on the users row as the
/// <c>password_hash</c> shadow property. Every call opens its own context scoped to the REQUESTED
/// tenant (construction-level filter, fail-closed) with exact (tenant, login) predicates — an
/// unknown tenant and an unknown login are indistinguishable through this port, and no list or
/// traversal API exists.
/// EF 憑證儲存：密碼雜湊存於 users 列之 password_hash shadow 欄；每次呼叫以請求租戶建立自持 context（構造級過濾 fail-closed）＋精確述詞；
/// 未知租戶與未知名不可區分；無列出／遍歷面。
/// </summary>
public sealed class EfUserCredentialStore(DbContextOptions<IamDbContext> options) : IUserCredentialStore
{
    public async Task<string?> FindPasswordHashAsync(string tenantId, string loginName, CancellationToken cancellationToken = default)
    {
        await using var context = new IamDbContext(options, tenantId);
        var user = await context.Users
            .Where(candidate => candidate.TenantId == tenantId && candidate.LoginName == loginName)
            .SingleOrDefaultAsync(cancellationToken);
        return user is null ? null : context.Entry(user).Property<string>("password_hash").CurrentValue;
    }

    public async Task SavePasswordHashAsync(string tenantId, string loginName, string passwordHash, CancellationToken cancellationToken = default)
    {
        await using var context = new IamDbContext(options, tenantId);
        var user = await context.Users
            .Where(candidate => candidate.TenantId == tenantId && candidate.LoginName == loginName)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException($"No user '{loginName}' exists in tenant '{tenantId}'.");
        context.Entry(user).Property<string>("password_hash").CurrentValue = passwordHash;
        await context.SaveChangesAsync(cancellationToken);
    }
}
