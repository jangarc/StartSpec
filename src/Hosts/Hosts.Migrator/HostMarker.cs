namespace Hosts.Migrator;

/// <summary>Assembly marker for architecture tests (ADR-001/002/003 guardrails). Sole migration runner exemption applies only to this assembly's wiring.</summary>
public static class HostMarker
{
    /// <summary>Identifies the migrator assembly in ArchTests allowlists.</summary>
    public const string OrgUnit = "Migrator";
}
