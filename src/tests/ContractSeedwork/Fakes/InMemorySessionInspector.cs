using SharedKernel.Ports;

namespace ContractSeedwork.Fakes;

/// <summary>
/// Reference fake of the SharedKernel-declared session inspector port (seam fake-first): the
/// contract suite binds these locked facts BEFORE the real JWT validator exists; the wired leg
/// re-runs the same facts through the real implementation.
/// SessionInspector port 的參考 fake：契約先行綁定；真實腿以真 JWT 驗章器重跑同一鎖定事實。
/// </summary>
public sealed class InMemorySessionInspector : ISessionInspector
{
    private sealed record Entry(SessionIdentity Identity, bool Expired);

    private readonly Dictionary<string, Entry> _entriesByToken = new(StringComparer.Ordinal);

    public void Register(string accessToken, SessionIdentity identity) => _entriesByToken[accessToken] = new Entry(identity, Expired: false);

    public void Expire(string accessToken)
    {
        if (_entriesByToken.TryGetValue(accessToken, out var entry))
        {
            _entriesByToken[accessToken] = entry with { Expired = true };
        }
    }

    public SessionIdentity? Inspect(string accessToken)
        => _entriesByToken.TryGetValue(accessToken, out var entry) && !entry.Expired ? entry.Identity : null;
}
