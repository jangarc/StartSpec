using Iam.Application.Users;
using Iam.ContractTests.Auth;
using Iam.ContractTests.Fakes;

namespace Iam.ContractTests.Auth.FakeLegs;

/// <summary>
/// Fake leg of the token issuer port contract: binds FIRST on the in-memory fake (task 4.3
/// fake-first discipline), pinning the locked facts before the real JWT adapter exists.
/// fake 腿：先行綁定鎖定事實，真實 JWT 轉接存在前即釘住契約。
/// </summary>
public sealed class InMemoryTokenIssuerLeg : TokenIssuerContractBase
{
    private readonly InMemoryTokenIssuer _issuer = new();

    protected override Task<ITokenIssuer> CreateIssuerAsync()
    {
        return Task.FromResult<ITokenIssuer>(_issuer);
    }

    protected override Task<TokenIdentity?> LastSubjectAsync()
    {
        TokenIdentity? subject = _issuer.Subjects.Count == 0 ? null : _issuer.Subjects[^1];
        return Task.FromResult(subject);
    }
}
