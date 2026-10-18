using Iam.Domain.Party;

namespace Iam.Application.Parties;

/// <summary>
/// Platform-level Party persistence seam (Application-owned contract, persistence side). Guarded
/// reach by design: only exact-identifier resolution and whole-aggregate save — no listable or
/// traversable surface exists (identity/party spec: parties are never listed, not even platform-side).
/// 平台級 Party 持久接縫；守衛式觸達：僅精確比對與整聚合保存，結構上不提供列出／遍歷 API。
/// </summary>
public interface IPartyStore : IAsyncDisposable
{
    /// <summary>Exact tuple reach only — near matches never resolve; null when no stored party carries the tuple.</summary>
    Task<Party?> FindExactAsync(PartyIdentifier tuple, CancellationToken cancellationToken = default);

    /// <summary>Persists the whole aggregate; a tuple already claimed by another party conflicts.</summary>
    Task SaveAsync(Party party, CancellationToken cancellationToken = default);
}
