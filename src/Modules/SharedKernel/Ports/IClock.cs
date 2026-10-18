namespace SharedKernel.Ports;

/// <summary>
/// Cross-cutting time source (ADR-007 admission category 3). Modules MUST NOT read the system clock
/// directly — inject this port so tests and lockstep envelope timestamps stay deterministic.
/// 跨模組時間埠；模組禁直讀系統時鐘，一律注入此 port。
/// </summary>
public interface IClock
{
    /// <summary>Current UTC time.</summary>
    DateTimeOffset UtcNow { get; }
}
