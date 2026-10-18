namespace Iam.Application.Users;

/// <summary>
/// Auth seam port (contract-kept): per-(tenant, login) password hash storage with NO listable or
/// cross-reachable surface — an unknown tenant and an unknown login are indistinguishable through
/// this port (both resolve to null), so callers cannot enumerate tenants or accounts.
/// 認證接縫埠：按 (tenant, login) 存取密碼雜湊；無列出或跨達面——未知租戶與未知登入經此埠無法區分（皆回 null），呼叫端無從枚舉。
/// </summary>
public interface IUserCredentialStore
{
    /// <summary>Resolves the stored hash or null when either tenant or login is unknown.</summary>
    Task<string?> FindPasswordHashAsync(string tenantId, string loginName, CancellationToken cancellationToken = default);

    /// <summary>Stores (or replaces) the password hash for the exact (tenant, login) pair.</summary>
    Task SavePasswordHashAsync(string tenantId, string loginName, string passwordHash, CancellationToken cancellationToken = default);
}
