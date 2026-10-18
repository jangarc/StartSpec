namespace Iam.Application.Users;

/// <summary>
/// Rotation state of one sign-in family (one family per sign-in / device): the current pair plus the
/// token rotated away for the reuse window. A revoked family is inactive everywhere.
/// 一次登入（=一裝置）一個家族的輪換狀態：現行配對＋寬限窗內的重放前 token；家族一經撤銷即全域失效。
/// </summary>
public sealed record StoredRefreshFamily(
    Guid FamilyId,
    string TenantId,
    Guid UserId,
    string LoginName,
    string CurrentRefreshTokenSha,
    string CurrentRefreshToken,
    string CurrentAccessToken,
    DateTimeOffset RefreshExpiresAt,
    string? RotatedFromSha = null,
    DateTimeOffset? RotatedAt = null,
    bool IsActive = true);

/// <summary>
/// Session seam port (contract-kept): refresh families are addressable ONLY by the exact token
/// SHA256 (current or the immediately rotated-from token) — no listable or traversal surface; the
/// disabled-user sweep revokes every family of one user.
/// session 接縫埠：家族僅能以精確 token SHA256 定址（現行或旋轉前 token）——無列出／遍歷面；停用清掃撤銷該使用者全家族。
/// </summary>
public interface IRefreshFamilyStore
{
    Task<StoredRefreshFamily?> FindByTokenShaAsync(string tokenSha256, CancellationToken cancellationToken = default);

    Task SaveAsync(StoredRefreshFamily family, CancellationToken cancellationToken = default);

    Task RevokeAllAsync(string tenantId, Guid userId, CancellationToken cancellationToken = default);
}
