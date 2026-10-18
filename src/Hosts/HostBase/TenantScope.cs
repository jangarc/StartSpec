namespace Hosts.Base;

/// <summary>Per-request identity resolution outcome (ADR-006: Hosts only carry identity, never authorization decisions).</summary>
public sealed class TenantScope
{
    public string? ActiveTenantId { get; internal set; }
}
