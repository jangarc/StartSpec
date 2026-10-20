using System.Text.Json;
using Iam.Application.Users;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.Ports;

namespace Iam.Infrastructure.Persistence;

/// <summary>
/// Real access-token validator (auth seam, design decisions 2/7): HMAC-SHA256 signature check plus
/// lifetime check against the host clock; the session identity comes ONLY from validated claims;
/// every failure path (absent/malformed/tampered/expired) returns null so callers share one
/// rejection shape — no oracle between failure kinds.
/// 真實驗章器：HMAC 驗簽＋效期；身分僅取自驗證通過的 claims；所有失敗一律 null（單一拒絕形）。
/// </summary>
public sealed class JwtTokenInspector(byte[] signingKey) : ISessionInspector
{
    public SessionIdentity? Inspect(string accessToken)
    {
        try
        {
            var result = new JsonWebTokenHandler().ValidateTokenAsync(accessToken, new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                IssuerSigningKey = new SymmetricSecurityKey(signingKey),
                ValidateIssuerSigningKey = true,
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                ClockSkew = TimeSpan.FromSeconds(30),
            }).GetAwaiter().GetResult();

            return result is { IsValid: true, SecurityToken: JsonWebToken token }
                ? new SessionIdentity(token.GetClaim("tenant_id").Value, Guid.Parse(token.GetClaim("user_id").Value))
                : null;
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException or JsonException or SecurityTokenException)
        {
            return null;
        }
    }
}
