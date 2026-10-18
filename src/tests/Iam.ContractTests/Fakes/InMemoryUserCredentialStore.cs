using Iam.Application.Users;

namespace Iam.ContractTests.Fakes;

/// <summary>
/// In-memory fake for the <see cref="IUserCredentialStore"/> auth seam port: password hashes keyed by
/// the exact (tenant, login) pair — an unknown tenant or unknown login resolves to null, so no caller
/// can distinguish them through the port (anti-enumeration surface).
/// 認證接縫憑證儲存偽：以精確 (tenant, login) 為鍵；未知租戶或未知登入皆回 null——經此埠無法區分（反枚舉面）。
/// </summary>
public sealed class InMemoryUserCredentialStore : IUserCredentialStore
{
    private readonly Dictionary<(string TenantId, string LoginName), string> _hashes = [];

    public int SavedCount => _hashes.Count;

    public Task<string?> FindPasswordHashAsync(string tenantId, string loginName, CancellationToken cancellationToken = default)
    {
        _hashes.TryGetValue((tenantId, loginName), out var hash);
        return Task.FromResult(hash);
    }

    public Task SavePasswordHashAsync(string tenantId, string loginName, string passwordHash, CancellationToken cancellationToken = default)
    {
        _hashes[(tenantId, loginName)] = passwordHash;
        return Task.CompletedTask;
    }
}
