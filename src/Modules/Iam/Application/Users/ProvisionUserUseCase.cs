using Iam.Application.Parties;
using Iam.Domain.Users;
using SharedKernel.Ports;

namespace Iam.Application.Users;

/// <summary>{tenant, login, display data, exact identifier tuple} — handed over by the Host with no further business validation (G3: business rules in Application).</summary>
public sealed record ProvisionUserCommand(string TenantId, string LoginName, string DisplayName, DateOnly? DateOfBirth, string TypeCode, string Number);

/// <summary>Exposes the created user id and the bound party id only (G1: contract owned by the use case).</summary>
public sealed record ProvisionUserResponse(Guid UserId, Guid PartyId);

/// <summary>
/// User provisioning per identity/user-auth spec: per-tenant unique login (duplicate rejected BEFORE
/// any party-side effect), then the party identity resolves through the canonical party provisioning —
/// exact identifier match binds the existing Party, otherwise a new Party is created. UserProvisioned
/// publishes at-least-once (consumers are idempotent).
/// 使用者建立：登入名查重先於任何 party 側副作用；身分經正典 party 用例解析——精確命中綁定、否則新建；UserProvisioned at-least-once 發布。
/// </summary>
public sealed class ProvisionUserUseCase(ProvisionPartyUseCase partyProvisioning, IUserStoreScope userStores, IClock clock, IEventBus eventBus)
{
    public async Task<ProvisionUserResponse> HandleAsync(ProvisionUserCommand command, CancellationToken cancellationToken = default)
    {
        await using var userStore = userStores.Member(command.TenantId);

        if (await userStore.FindAsync(command.TenantId, command.LoginName, cancellationToken) is not null)
        {
            throw new UserLoginConflictException(command.TenantId, command.LoginName);
        }

        var party = await partyProvisioning.HandleAsync(
            new ProvisionPartyCommand(command.DisplayName, command.DateOfBirth, command.TypeCode, command.Number), cancellationToken);

        var user = User.Provision(command.TenantId, command.LoginName, party.PartyId, clock.UtcNow);
        await userStore.SaveAsync(user, cancellationToken);
        foreach (var domainEvent in user.PullDomainEvents())
        {
            await eventBus.PublishAsync(domainEvent, cancellationToken);
        }

        return new ProvisionUserResponse(user.UserId, party.PartyId);
    }
}
