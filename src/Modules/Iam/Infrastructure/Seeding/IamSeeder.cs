using Iam.Application.Parties;
using Iam.Application.Tenants;
using Iam.Application.Users;

namespace Iam.Infrastructure.Seeding;

/// <summary>The fixed bootstrap plan: which tenant/organization/operator to create when absent.</summary>
/// <param name="OperatorPassword">Plaintext is required here for the one-time seed hash; never logged, never persisted.</param>
public sealed record IamSeedPlan(
    string TenantId,
    string TenantName,
    string InitialOrganizationName,
    string OperatorLoginName,
    string OperatorDisplayName,
    string OperatorTypeCode,
    string OperatorNumber,
    string OperatorPassword);

/// <summary>What the run actually created; both false = pure no-op run.</summary>
public sealed record IamSeedResult(bool TenantCreated, bool OperatorCreated);

/// <summary>
/// Idempotent deployment seeder (design §deployment): creates the initial tenant + operator through
/// the canonical APPLICATION use cases (not raw stores) so the tenant/party/user semantics and the
/// domain events stay identical to provisioning-by-API. Every step is existence-guarded, running it
/// twice leaves row counts identical; duplicates surface as conflicts rather than silent merges.
/// 冪等 seeder：經正典應用層用例植種初始租戶＋操作員；每步先檢存在，重跑零新增。
/// </summary>
public sealed class IamSeeder(
    ProvisionTenantUseCase tenantProvisioning,
    ITenantStoreScope tenantStores,
    ProvisionUserUseCase userProvisioning,
    IUserStoreScope userStores,
    IPasswordHasher hasher,
    IUserCredentialStore credentials)
{
    public async Task<IamSeedResult> SeedAsync(IamSeedPlan plan, CancellationToken cancellationToken = default)
    {
        var tenantCreated = false;
        await using (var tenantStore = tenantStores.Platform())
        {
            if (!await tenantStore.TenantExistsAsync(plan.TenantId, cancellationToken))
            {
                await tenantProvisioning.HandleAsync(
                    new ProvisionTenantCommand(plan.TenantId, plan.TenantName, plan.InitialOrganizationName), cancellationToken);
                tenantCreated = true;
            }
        }

        await using (var userStore = userStores.Member(plan.TenantId))
        {
            if (await userStore.FindAsync(plan.TenantId, plan.OperatorLoginName, cancellationToken) is not null)
            {
                return new IamSeedResult(tenantCreated, OperatorCreated: false);
            }
        }

        // The canonical provisioning path resolves-or-creates the operator's Party (exact tuple match),
        // publishes UserProvisioned, and re-checks login uniqueness before any side effect.
        await userProvisioning.HandleAsync(
            new ProvisionUserCommand(plan.TenantId, plan.OperatorLoginName, plan.OperatorDisplayName, null, plan.OperatorTypeCode, plan.OperatorNumber),
            cancellationToken);
        await credentials.SavePasswordHashAsync(plan.TenantId, plan.OperatorLoginName, hasher.Hash(plan.OperatorPassword), cancellationToken);

        return new IamSeedResult(tenantCreated, OperatorCreated: true);
    }
}
