using ContractSeedwork.Fakes;
using SharedKernel.Ports;

namespace ContractSeedwork;

/// <summary>
/// Base for port contract test suites (ADR-010): every scenario is derived from a spec requirement,
/// written fake-first, and re-runnable against a wired implementation through a factory. The base
/// admits no production reference and no business logic — test-purpose plumbing only.
/// 契約測試基底：場景由 spec 派生、fake 先行，僅收測試用途管線。
/// </summary>
public abstract class ContractTestBase
{
    /// <summary>Deterministic clock for envelope timestamps in contract runs.</summary>
    protected static FakeClock FixedClock() => new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
}

/// <summary>
/// Factory that swaps the subject implementation under the SAME locked contract suite
/// (fake first, wired implementation when it exists). 替換受測物而不動契約套件（雙跑）。
/// </summary>
public abstract class ContractSubjectFactory<TSubject>
    where TSubject : class
{
    /// <summary>Creates the fake/wireless subject.</summary>
    public abstract TSubject CreateFake();

    /// <summary>Creates the wired production subject; return null when not yet wired (tests skip the leg).</summary>
    public abstract TSubject? CreateWired();
}
