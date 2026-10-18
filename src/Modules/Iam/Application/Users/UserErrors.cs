namespace Iam.Application.Users;

/// <summary>
/// Per-tenant login-name conflict (unique key). The use case pre-empts with the typed error before any
/// party-side effect; the store surfaces the unique-index race as the same shape. Hosts map to 409.
/// 同租戶登入名衝突（唯一鍵）：用例先型別化拒絕（零副作用）、store 將唯一索引競態譯為同一形狀；Host 映射 409。
/// </summary>
public sealed class UserLoginConflictException(string tenantId, string loginName, Exception? innerException = null)
    : Exception($"Login name '{loginName}' is already used by tenant '{tenantId}'.", innerException)
{
    /// <summary>The conflicting tenant scope.</summary>
    public string TenantId => tenantId;

    /// <summary>The conflicting login name.</summary>
    public string LoginName => loginName;
}

/// <summary>
/// The SINGLE generic sign-in failure: wrong password, unknown login, unknown tenant, and a disabled
/// user are deliberately indistinguishable (no enumeration oracle; design decision 4). Hosts map to
/// the same status on every path.
/// 單一通用登入失敗：錯密／未知名／未知租戶／停用一律不可區分（防枚舉 oracle；設計決策 4）；各路徑 Host 映射同一狀態碼。
/// </summary>
public sealed class AuthenticationFailedException(string message) : Exception(message);

/// <summary>
/// Refresh rejection: rotated-out tokens (beyond window), expired tokens, unknown tokens, and revoked
/// families are indistinguishable — no oracle for which token was replayed. Hosts map to 401.
/// 刷新拒絕：窗外輪換舊 token、逾期、未知、已撤銷家族一律單一通用型別（不可反推何者重放）；Host 映射 401。
/// </summary>
public sealed class TokenRefreshRejectedException(string message) : Exception(message);

/// <summary>
/// Raised when a login account is unreachable in the acting tenant (denied-as-not-found; mirrors the
/// module's RoleUnknownException shape). Unknown and foreign-tenant users share ONE shape.
/// 停用目標不可達（denied-as-not-found；同模組 RoleUnknownException 形）：未知與他租戶帳戶共用同一形。
/// </summary>
public sealed class UserUnknownException(string tenantId, string loginName)
    : Exception($"Login '{loginName}' is not reachable in tenant '{tenantId}'.");
