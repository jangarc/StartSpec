using System.Text;
using ContractSeedwork.Fakes;
using Iam.Application.Users;
using Iam.ContractTests.Fakes;
using Iam.Infrastructure.Persistence;
using SharedKernel.Ports;
using Xunit;

namespace Iam.UnitTests.Session;

/// <summary>
/// Session inspector port contract (design decision 2 seam, fake-first): a fresh signed token yields
/// the session identity it binds; tampered, expired, or malformed values yield null — one uniform
/// rejection with no oracle between failure kinds. The fake leg binds FIRST; the wired leg re-runs
/// the same locked facts through the real JWT issuer + validator.
/// 驗章埠契約：新簽章值回傳工作階段身分；失敗種類一律 null、互不可反推；fake 腿先行、真實腿重跑。
/// </summary>
public abstract class SessionInspectorContractBase
{
    protected abstract (ITokenIssuer Issuer, ISessionInspector Inspector) CreateAsync();

    /// <summary>Issues a token the clock has already moved past (fake: expiry marker; wired: back-dated issuance).</summary>
    protected abstract Task<IssuedTokens> IssueExpiredAsync(TokenIdentity subject);

    [Fact]
    public async Task Fresh_signed_token_yields_the_bound_session_identity()
    {
        var (issuer, inspector) = CreateAsync();
        var subject = new TokenIdentity("acme", Guid.NewGuid());

        var issued = await issuer.IssueAsync(subject);
        var identity = inspector.Inspect(issued.AccessToken);

        Assert.NotNull(identity);
        Assert.Equal(subject.TenantId, identity!.TenantId);
        Assert.Equal(subject.UserId, identity.UserId);
    }

    [Fact]
    public async Task Tampered_token_is_rejected()
    {
        var (issuer, inspector) = CreateAsync();
        var issued = await issuer.IssueAsync(new TokenIdentity("acme", Guid.NewGuid()));
        var last = issued.AccessToken[^1];
        var tampered = issued.AccessToken[..^1] + (last == 'a' ? 'b' : 'a');

        Assert.NotEqual(issued.AccessToken, tampered);
        Assert.Null(inspector.Inspect(tampered));
    }

    [Fact]
    public async Task Expired_token_is_rejected()
    {
        var (_, inspector) = CreateAsync();
        var issued = await IssueExpiredAsync(new TokenIdentity("acme", Guid.NewGuid()));

        Assert.Null(inspector.Inspect(issued.AccessToken));
    }

    [Fact]
    public async Task Malformed_token_is_rejected()
    {
        var (_, inspector) = CreateAsync();

        Assert.Null(inspector.Inspect("garbage"));
    }
}

/// <summary>Fake leg: binds the contract first against the seedwork fakes (task 8.2 gate fix).</summary>
public sealed class InMemorySessionInspectorLeg : SessionInspectorContractBase
{
    private readonly InMemorySessionInspector _inspector = new();
    private readonly RecordingIssuer _issuer;

    public InMemorySessionInspectorLeg()
    {
        _issuer = new RecordingIssuer(new InMemoryTokenIssuer(), _inspector);
    }

    protected override (ITokenIssuer, ISessionInspector) CreateAsync()
        => (_issuer, _inspector);

    protected override async Task<IssuedTokens> IssueExpiredAsync(TokenIdentity subject)
    {
        var issued = await _issuer.IssueAsync(subject);
        _inspector.Expire(issued.AccessToken);
        return issued;
    }

    private sealed class RecordingIssuer(ITokenIssuer inner, InMemorySessionInspector inspector) : ITokenIssuer
    {
        public async Task<IssuedTokens> IssueAsync(TokenIdentity subject, CancellationToken cancellationToken = default)
        {
            var issued = await inner.IssueAsync(subject, cancellationToken);
            inspector.Register(issued.AccessToken, new SessionIdentity(subject.TenantId, subject.UserId));
            return issued;
        }
    }
}

/// <summary>Wired leg: the real JWT issuer + the real validator (expiry = back-dated issuance, validated against the real clock).</summary>
public sealed class JwtTokenInspectorLeg : SessionInspectorContractBase
{
    // Test-only 48-byte HMAC key; production key material comes from host configuration (IamComposition).
    private static readonly byte[] SigningKey =
        Encoding.UTF8.GetBytes("iam-lorem-ipsum-test-signing-key-secret-0001");

    private JwtTokenIssuer CreateIssuer(DateTimeOffset utcNow)
        => new(SigningKey, new FakeClock(utcNow), TimeSpan.FromMinutes(5));

    protected override (ITokenIssuer, ISessionInspector) CreateAsync()
        => (CreateIssuer(DateTimeOffset.UtcNow), new JwtTokenInspector(SigningKey));

    protected override Task<IssuedTokens> IssueExpiredAsync(TokenIdentity subject)
        => CreateIssuer(DateTimeOffset.UtcNow - TimeSpan.FromMinutes(6)).IssueAsync(subject);
}
