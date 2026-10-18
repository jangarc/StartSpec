using SharedKernel.Ports;

namespace ContractSeedwork.Fakes;

/// <summary>
/// Deterministic in-memory <see cref="IClock"/> for unit and contract runs.
/// 可凍結/推進的時間 fake。
/// </summary>
public sealed class FakeClock(DateTimeOffset initialUtcNow) : IClock
{
    private DateTimeOffset _utcNow = initialUtcNow;

    public DateTimeOffset UtcNow => _utcNow;

    public void Advance(TimeSpan delta)
    {
        _utcNow = _utcNow.Add(delta);
    }

    public void Set(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }
}
