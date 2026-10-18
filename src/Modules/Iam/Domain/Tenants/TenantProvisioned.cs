using SharedKernel;

namespace Iam.Domain.Tenants;

/// <summary>
/// Raised when a tenant is provisioned. Carries the frozen ADR-009 envelope fields (inherited base)
/// plus the provision facts. Evolves additively only.
/// 租戶建立事件：信封欄位繼承基底（凍結），加法演進。
/// </summary>
public sealed record TenantProvisioned : DomainEvent
{
    /// <summary>Tenant display name at provisioning time.</summary>
    public required string TenantName { get; init; }

    /// <summary>Initial organization identity created together with the tenant.</summary>
    public required Guid OrganizationId { get; init; }
}
