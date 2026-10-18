using SharedKernel;

namespace Iam.Domain.Users;

/// <summary>
/// Raised when a user is disabled (one-way transition). Carries the frozen ADR-009 envelope fields;
/// consumers (session/flow revocation) are at-least-once and idempotent. Evolves additively only.
/// 使用者停用事件（單向轉換）；信封欄位繼承基底；消費者冪等；加法演進。
/// </summary>
public sealed record UserDisabled : DomainEvent
{
    /// <summary>Per-tenant user identity.</summary>
    public required Guid UserId { get; init; }
}
