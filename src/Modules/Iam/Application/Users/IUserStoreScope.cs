namespace Iam.Application.Users;

/// <summary>
/// Store-for-scope factory port (Application-owned seam contract). Users exist only member-scoped:
/// every face is born with the server-resolved tenant (no platform face — denied by construction).
/// 範圍工廠 port：使用者僅成員面——一律攜伺服器端解析之租戶構造（無平台面）。
/// </summary>
public interface IUserStoreScope
{
    /// <summary>Member face for the RESOLVED tenant (call sites dispose the store after use).</summary>
    IUserStore Member(string tenantId);
}
