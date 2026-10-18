using SharedKernel;

namespace Iam.Domain.Users;

/// <summary>
/// Raised when a per-tenant user is provisioned. Carries the frozen ADR-009 envelope fields (inherited
/// base; TenantId = owning tenant) plus the provision facts. Evolves additively only.
/// 使用者建立事件：信封欄位繼承基底（凍結）；TenantId＝所屬租戶；加法演進。
/// </summary>
public sealed record UserProvisioned : DomainEvent
{
    /// <summary>Per-tenant user identity.</summary>
    public required Guid UserId { get; init; }

    /// <summary>The bound platform Party identity.</summary>
    public required Guid PartyId { get; init; }

    /// <summary>Login name at provisioning time.</summary>
    public required string LoginName { get; init; }
}
