using SharedKernel.Ports;

namespace Iam.Application.Authorization;

/// <summary>{tenant, assignment} — handed over by the Host with no further business validation (G3: business rules in Application).</summary>
public sealed record RevokeRoleCommand(string TenantId, Guid AssignmentId);

/// <summary>
/// Revocation per identity/authorization spec: one-way lifecycle transition that publishes RoleRevoked
/// over the event bus (at-least-once — watcher consumers are idempotent).
/// 撤銷：單向轉換並經匯流排發布 RoleRevoked（at-least-once——watcher 消費者冪等）。
/// </summary>
public sealed class RevokeRoleUseCase(IRoleAssignmentStoreScope assignmentStores, IClock clock, IEventBus eventBus)
{
    public async Task HandleAsync(RevokeRoleCommand command, CancellationToken cancellationToken = default)
    {
        await using var store = assignmentStores.Member(command.TenantId);
        var assignment = await store.FindAsync(command.TenantId, command.AssignmentId, cancellationToken)
            ?? throw new RoleAssignmentUnknownException(command.TenantId, command.AssignmentId);

        assignment.Revoke(clock.UtcNow);
        await store.SaveAsync(assignment, cancellationToken);
        foreach (var domainEvent in assignment.PullDomainEvents())
        {
            await eventBus.PublishAsync(domainEvent, cancellationToken);
        }
    }
}
