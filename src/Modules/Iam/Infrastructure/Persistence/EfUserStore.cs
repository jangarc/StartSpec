using Iam.Application.Users;
using Iam.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Iam.Infrastructure.Persistence;

/// <summary>
/// EF-backed <see cref="IUserStore"/> implementation. Member-scoped BY CONSTRUCTION (the context is
/// born with the resolved tenant, fail-closed global filter) AND by explicit (tenant, login)
/// predicates; duplicate logins surface as <see cref="UserLoginConflictException"/> (unique key
/// <c>ix_users_tenant_login</c>). The store owns its context; call sites dispose.
/// EF 持久實作：構造級租戶範圍＋顯式述詞雙保險；登入名衝突譯為 UserLoginConflictException；呼叫端 dispose。
/// </summary>
public sealed class EfUserStore : IUserStore
{
    private readonly IamDbContext db;

    public EfUserStore(DbContextOptions<IamDbContext> options, string activeTenantId)
    {
        db = new IamDbContext(options, activeTenantId);
    }

    public async ValueTask DisposeAsync()
    {
        await db.DisposeAsync();
    }

    public Task<User?> FindAsync(string tenantId, string loginName, CancellationToken cancellationToken = default)
    {
        return db.Users
            .Where(user => user.TenantId == tenantId && user.LoginName == loginName)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task SaveAsync(User user, CancellationToken cancellationToken = default)
    {
        // PK-offset upsert: the lifecycle Path (disable) re-saves an EXISTING aggregate — a row with
        // the same UserId updates in place; absent ids insert. The conflict floor stays for OTHER
        // rows bearing the same (tenant, login); ix_users_tenant_login remains the race backstop.
        if (await db.Users.AnyAsync(existing
                => existing.UserId != user.UserId && existing.TenantId == user.TenantId && existing.LoginName == user.LoginName, cancellationToken))
        {
            throw new UserLoginConflictException(user.TenantId, user.LoginName);
        }

        var existingRow = await db.Users.SingleOrDefaultAsync(candidate => candidate.UserId == user.UserId, cancellationToken);
        if (existingRow is null)
        {
            db.Add(user);
        }
        else
        {
            db.Entry(existingRow).CurrentValues.SetValues(user);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true
            || exception.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new UserLoginConflictException(user.TenantId, user.LoginName, exception);
        }
    }
}
