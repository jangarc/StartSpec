namespace Hosts.Dev;

/// <summary>Assembly marker for architecture tests (ADR-001/002 guardrails).</summary>
public static class HostMarker
{
    /// <summary>Identifies the dev-only assembly in ArchTests allowlists.</summary>
    public const string OrgUnit = "Dev";
}
