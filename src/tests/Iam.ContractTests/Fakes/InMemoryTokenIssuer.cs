using Iam.Application.Users;

namespace Iam.ContractTests.Fakes;

/// <summary>
/// In-memory fake for the <see cref="ITokenIssuer"/> auth seam port: records every subject and the
/// issue count, returns deterministic opaque strings (no real signing) so family rotation state and
/// replay logic stay testable without crypto.
/// 簽發 fake：記錄簽發主體與次數、回傳決定性 opaque 字串；家族輪換與重放邏輯免真簽章即可測。
/// </summary>
public sealed class InMemoryTokenIssuer : ITokenIssuer
{
    private int _counter;

    /// <summary>Subjects in issuance order (the fake-side analogue of decodable JWT claims).</summary>
    public List<TokenIdentity> Subjects { get; } = [];

    /// <summary>Total issuance calls observed.</summary>
    public int IssuedCount => _counter;

    public Task<IssuedTokens> IssueAsync(TokenIdentity subject, CancellationToken cancellationToken = default)
    {
        _counter++;
        Subjects.Add(subject);
        return Task.FromResult(new IssuedTokens(
            AccessToken: $"jwt:{_counter}:{subject.TenantId}:{subject.UserId}",
            RefreshToken: $"rt:{_counter}:{subject.TenantId}:{subject.UserId}"));
    }
}
