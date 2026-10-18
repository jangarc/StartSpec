using Iam.Application.Users;
using Xunit;

namespace Iam.ContractTests.Auth;

/// <summary>
/// Token issuer port contract (auth seam, scenarios derived from identity/user-auth; the fake leg
/// binds FIRST, the wired JWT leg re-runs the SAME locked facts): every issued pair binds exactly the
/// requested subject, access and refresh are distinct kinds, and refresh values are fresh per issue
/// (rotation assumes perfection here — the session use case owns the reuse window on top).
/// 權杖簽發埠契約：每次配對精確綁定主體、存取與刷新為兩類值、刷新值每次簽發皆新；接縫由 session 用例上層治理寬限窗。
/// </summary>
public abstract class TokenIssuerContractBase
{
    /// <summary>The last issued subject (fake: recorded; wired: decoded from the JWT claims).</summary>
    protected abstract Task<TokenIdentity?> LastSubjectAsync();

    protected abstract Task<ITokenIssuer> CreateIssuerAsync();

    [Fact]
    public async Task Issued_pairs_bind_exactly_the_requested_subject()
    {
        var issuer = await CreateIssuerAsync();
        var subject = new TokenIdentity("acme", Guid.NewGuid());

        await issuer.IssueAsync(subject);

        var lastSubject = await LastSubjectAsync();
        Assert.NotNull(lastSubject);
        Assert.Equal(subject.TenantId, lastSubject!.TenantId);
        Assert.Equal(subject.UserId, lastSubject.UserId);
    }

    [Fact]
    public async Task Access_and_refresh_are_distinct_kinds()
    {
        var issuer = await CreateIssuerAsync();

        var issued = await issuer.IssueAsync(new TokenIdentity("acme", Guid.NewGuid()));

        Assert.NotEqual(issued.AccessToken, issued.RefreshToken);
    }

    [Fact]
    public async Task Refresh_values_are_fresh_per_issue()
    {
        var issuer = await CreateIssuerAsync();
        var subject = new TokenIdentity("acme", Guid.NewGuid());

        var first = await issuer.IssueAsync(subject);
        var second = await issuer.IssueAsync(subject);

        Assert.NotEqual(first.RefreshToken, second.RefreshToken);
        Assert.NotEqual(first.AccessToken, second.AccessToken);
    }
}
