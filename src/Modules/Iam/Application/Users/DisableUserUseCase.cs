using Iam.Domain.Users;
using SharedKernel.Ports;

namespace Iam.Application.Users;

/// <summary>{tenant, login} — handed over by the Host with no further business validation (G3: business rules in Application).</summary>
public sealed record DisableUserCommand(string TenantId, string LoginName);

/// <summary>
/// One-way disablement per the identity domain: the aggregate owns the transition (a second disable
/// rejects), the store persists it, ALL of the user's refresh families are swept (spec: disabling
/// SHALL revoke all families — fail-closed even against reuse-window replays), and UserDisabled
/// flows over the event bus exactly once per transition (at-least-once consumers are idempotent).
/// The address is (tenant, login) — the ONLY reachable face of the user port.
/// 單向停用：聚合自持轉換（再停用即拒絕）、store 持久化、積極清掃該使用者全家族（fail-closed），
/// UserDisabled 每次轉換恰投遞一枚（at-least-once，消費者冪等）；尋址僅 (tenant, login)。
/// </summary>
public sealed class DisableUserUseCase(IUserStoreScope stores, IRefreshFamilyStore families, IClock clock, IEventBus eventBus)
{
    public async Task HandleAsync(DisableUserCommand command, CancellationToken cancellationToken = default)
    {
        await using var store = stores.Member(command.TenantId);
        var user = await store.FindAsync(command.TenantId, command.LoginName, cancellationToken)
            ?? throw new UserUnknownException(command.TenantId, command.LoginName);

        user.Disable(clock.UtcNow);
        await store.SaveAsync(user, cancellationToken);
        await families.RevokeAllAsync(command.TenantId, user.UserId, cancellationToken);
        foreach (var domainEvent in user.PullDomainEvents())
        {
            await eventBus.PublishAsync(domainEvent, cancellationToken);
        }
    }
}
