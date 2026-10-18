using Iam.Domain.Users;

namespace Iam.Application.Users;

/// <summary>
/// Persistence port for per-tenant users (Application-owned contract; implemented by module
/// Infrastructure). Addressing is (tenant, login) ONLY — users are never listable or id-reachable;
/// other tenants or unknown logins return NOTHING (denied-as-not-found: parameter-level scope with
/// the construction-level global filter as backstop). The store owns its resources: call sites SHALL dispose.
/// 使用者持久 port（契約歸 Application）：僅以 (tenant, login) 尋址、不可列出；呼叫端須 dispose。
/// </summary>
public interface IUserStore : IAsyncDisposable
{
    /// <summary>Single lookup by exact tenant + login pair.</summary>
    Task<User?> FindAsync(string tenantId, string loginName, CancellationToken cancellationToken = default);

    /// <summary>Persists the user; a duplicate (tenant, login) pair SHALL conflict.</summary>
    Task SaveAsync(User user, CancellationToken cancellationToken = default);
}
