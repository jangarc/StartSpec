using SharedKernel;

namespace Iam.Domain.Authorization;

/// <summary>
/// Raised when a role assignment is revoked (one-way transition). Carries the frozen ADR-009 envelope;
/// watcher nodes refresh the enforcer within the propagation bound — at-least-once, idempotent.
/// 角色撤銷事件（單向轉換）；信封欄位繼承基底；watcher 節點於傳播上限內刷新執行器；at-least-once 冪等。
/// </summary>
public sealed record RoleRevoked : DomainEvent
{
    /// <summary>Per-tenant user whose assignment was revoked.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Identifier of the revoked role.</summary>
    public required Guid RoleId { get; init; }
}
