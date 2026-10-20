namespace SharedKernel.Ports;

/// <summary>The verified session identity an authenticated request carries (ADR-009 envelope tenant_id/user_id as validated).</summary>
public sealed record SessionIdentity(string TenantId, Guid UserId);

/// <summary>
/// Cross-cutting auth port (ADR-007 admission: cross-cutting ports): validates an access token and
/// returns the session identity it binds; null when the token is absent, malformed, tampered, or
/// expired. The server NEVER resolves tenant scope from unverified client input (design decision 2 —
/// 絕不單獨信任客戶端自帶標頭)；每條失敗路徑共用同一 null 拒絕（不可反推失敗種類）。
/// </summary>
public interface ISessionInspector
{
    SessionIdentity? Inspect(string accessToken);
}
