using Iam.Application.Users;
using Iam.Domain.Users;

namespace Iam.ContractTests.Fakes;

/// <summary>
/// In-memory IUserStore fake (same conflict shape as EfUserStore: duplicate (tenant, login) rejects).
/// Isolation carries at parameter level — the key pair is the scope. Instances are reused rather than
/// copied; the fake asserts save + (tenant, login) reach semantics only.
/// 契約 fake：以 (tenant, login) 鍵值提供與 EF 實作相同的衝突與尋址語意；物件引用重用。
/// </summary>
public sealed class InMemoryUserStore : IUserStore
{
    public int SavedCount { get; private set; }

    private readonly Dictionary<(string TenantId, string LoginName), User> _users = [];

    public Task<User?> FindAsync(string tenantId, string loginName, CancellationToken cancellationToken = default)
    {
        _users.TryGetValue((tenantId, loginName), out var found);
        return Task.FromResult(found);
    }

    public Task SaveAsync(User user, CancellationToken cancellationToken = default)
    {
        var key = (user.TenantId, user.LoginName);
        if (_users.TryGetValue(key, out var stored) && stored.UserId != user.UserId)
        {
            throw new UserLoginConflictException(user.TenantId, user.LoginName);
        }

        _users[key] = user;
        SavedCount++;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
