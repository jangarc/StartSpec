using SharedKernel;

namespace Iam.Domain.Party;

/// <summary>
/// Raised when a Party is provisioned platform-wide. Carries the frozen ADR-009 envelope fields
/// (inherited base) plus the provision facts. Evolves additively only.
/// Party 建立事件：信封欄位繼承基底（凍結），加法演進。
/// </summary>
public sealed record PartyProvisioned : DomainEvent
{
    /// <summary>Platform-scoped Party identity.</summary>
    public required Guid PartyId { get; init; }

    /// <summary>Display name at provisioning time.</summary>
    public required string DisplayName { get; init; }
}
