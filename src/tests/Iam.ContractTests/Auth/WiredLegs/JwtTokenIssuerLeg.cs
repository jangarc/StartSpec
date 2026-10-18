using System.Text;
using ContractSeedwork.Fakes;
using Iam.Application.Users;
using Iam.ContractTests.Auth;
using Iam.Infrastructure.Persistence;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Iam.ContractTests.Auth.WiredLegs;

/// <summary>
/// Wired leg of the token issuer port contract (task 4.3): the real JsonWebTokens adapter signs and
/// the subject binding is decoded back from the signed JWT claims. No container — the issuer is
/// stateless.
/// 真實腿：真 JWT 簽發器；主體綁定自簽章 JWT claims 解回；簽發器無狀態、不需容器。
/// </summary>
public sealed class JwtTokenIssuerLeg : TokenIssuerContractBase
{
    private static readonly DateTimeOffset FixedNow = new(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly IssuedTokens?[] _lastIssued = new IssuedTokens?[1];

    protected override Task<ITokenIssuer> CreateIssuerAsync()
    {
        // Test-only 48-byte HMAC key; production key material comes from host configuration (IamComposition).
        var signingKey = Encoding.UTF8.GetBytes("iam-lorem-ipsum-test-signing-key-secret-0001");
        var issuer = new JwtTokenIssuer(signingKey, new FakeClock(FixedNow), TimeSpan.FromMinutes(5));
        return Task.FromResult<ITokenIssuer>(new RecordingIssuer(issuer, _lastIssued));
    }

    protected override Task<TokenIdentity?> LastSubjectAsync()
    {
        var last = _lastIssued[0];
        if (last is null)
        {
            return Task.FromResult<TokenIdentity?>(null);
        }

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(last.AccessToken);
        return Task.FromResult<TokenIdentity?>(new TokenIdentity(jwt.GetClaim("tenant_id").Value, Guid.Parse(jwt.GetClaim("user_id").Value)));
    }

    private sealed class RecordingIssuer(ITokenIssuer inner, IssuedTokens?[] slot) : ITokenIssuer
    {
        public async Task<IssuedTokens> IssueAsync(TokenIdentity subject, CancellationToken cancellationToken = default)
        {
            var issued = await inner.IssueAsync(subject, cancellationToken);
            slot[0] = issued;
            return issued;
        }
    }
}
