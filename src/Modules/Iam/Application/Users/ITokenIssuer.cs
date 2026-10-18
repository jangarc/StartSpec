namespace Iam.Application.Users;

/// <summary>The authenticated subject both tokens bind to (the stateless access JWT carries identity and tenant_id only).</summary>
public sealed record TokenIdentity(string TenantId, Guid UserId);

/// <summary>One live session pair: the short-lived access token plus the opaque rotating refresh token.</summary>
public sealed record IssuedTokens(string AccessToken, string RefreshToken);

/// <summary>
/// Auth seam port (contract-kept; tests bind the fake first): issues the access–refresh pair bound
/// exactly to the subject — access short-lived, refresh opaque and fresh per issuance. Decoding and
/// signature validation of the access token stay with the HostBase binding.
/// 認證接縫埠：按主體精確簽發存取–刷新配對；存取短效、刷新 opaque 且每次簽發皆新；解碼與驗章留 HostBase 綁定。
/// </summary>
public interface ITokenIssuer
{
    Task<IssuedTokens> IssueAsync(TokenIdentity subject, CancellationToken cancellationToken = default);
}
