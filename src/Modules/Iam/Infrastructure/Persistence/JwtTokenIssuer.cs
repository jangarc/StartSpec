using System.Security.Cryptography;
using Iam.Application.Users;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.Ports;

namespace Iam.Infrastructure.Persistence;

/// <summary>
/// Real JSON Web Token adapter (auth seam, design decisions 3/7): the stateless access JWT carries
/// identity and tenant_id only and is per-issue fresh (jti); the refresh token is an opaque random
/// value. Claims are never authorization input — execution stays server-side (design decision 7).
/// 真實 JWT 轉接：無狀態存取 JWT 僅攜身份＋tenant_id 且每次簽發皆新（jti）；刷新為 opaque 隨機值；claims 絕不作授權依據。
/// </summary>
public sealed class JwtTokenIssuer(byte[] signingKey, IClock clock, TimeSpan accessTtl) : ITokenIssuer
{
    public Task<IssuedTokens> IssueAsync(TokenIdentity subject, CancellationToken cancellationToken = default)
    {
        var handler = new JsonWebTokenHandler();
        var issuedAt = clock.UtcNow;
        var accessToken = handler.CreateToken(new SecurityTokenDescriptor
        {
            IssuedAt = issuedAt.UtcDateTime,
            Expires = issuedAt.Add(accessTtl).UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                ["tenant_id"] = subject.TenantId,
                ["user_id"] = subject.UserId,
                ["jti"] = Guid.NewGuid().ToString(),
            },
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(signingKey), SecurityAlgorithms.HmacSha256),
        });
        var refreshToken = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        return Task.FromResult(new IssuedTokens(accessToken, refreshToken));
    }
}
