using Iam.Domain.Tenants;
using SharedKernel.Ports;

namespace Iam.Application.Tenants;

/// <summary>{tenant_id, name, initial organization name} — handed over by the Host with no further business validation (G3: business rules in Application).</summary>
public sealed record ProvisionTenantCommand(string TenantId, string TenantName, string InitialOrganizationName);

/// <summary>Response contract owned by the use case (G1: contracts belong to Application).</summary>
public sealed record ProvisionTenantResponse(string TenantId, Guid OrganizationId);

/// <summary>
/// Provisions a tenant: unique-identifier check, aggregate creation, persistence, and publication of the
/// raised <see cref="TenantProvisioned"/> over the event bus (at-least-once — consumers must be idempotent).
/// 建立租戶：唯一性檢查→聚合建立→持久→以 <see cref="IEventBus"/> 發布事件（at-least-once）。
/// </summary>
public sealed class ProvisionTenantUseCase(ITenantStore store, IClock clock, IEventBus eventBus)
{
    public async Task<ProvisionTenantResponse> HandleAsync(ProvisionTenantCommand command, CancellationToken cancellationToken = default)
    {
        if (await store.TenantExistsAsync(command.TenantId, cancellationToken))
        {
            throw new TenantAlreadyExistsException(command.TenantId);
        }

        var tenant = Tenant.Provision(command.TenantId, command.TenantName, command.InitialOrganizationName, clock.UtcNow);
        await store.SaveAsync(tenant, cancellationToken);

        foreach (var domainEvent in tenant.PullDomainEvents())
        {
            await eventBus.PublishAsync(domainEvent, cancellationToken);
        }

        return new ProvisionTenantResponse(tenant.TenantId, tenant.Organizations[0].OrganizationId);
    }
}
