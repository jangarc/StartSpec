namespace Hosts.Base;

/// <summary>
/// Cross-cutting composition home for all <c>Hosts.*</c> (design D1'/G4): tenant resolution, validation
/// pipeline, ProblemDetails, OpenTelemetry, and the binding adapters for SharedKernel cross-cutting ports
/// (<c>IEventBus</c>, <c>IClock</c> — adapters SHALL live here, not in a single module's Infrastructure).
/// 橫切只進此處；線上 Host 禁自動 migrate（唯一執行者為 <c>Hosts.Migrator</c>）。
/// </summary>
public static class HostBaseInfo
{
    /// <summary>Shared identity tag used by OpenTelemetry tenant/store labels.</summary>
    public const string Name = "HostBase";
}
