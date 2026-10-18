using Iam.Application.Tenants;
using Iam.Domain.Tenants;
using Xunit;

namespace Iam.ContractTests.Tenants;

/// <summary>
/// Locked persistence-seam contract suite (ADR-010), scenarios derived FROM the spec identity-tenant
/// delta — never from the first implementation. Double-run: fake leg (in-memory) AND wired leg
/// (EF Core 10 / Npgsql against Testcontainers Postgres). Writes go through the platform-entry store
/// (no scope = the provisioning path); reads always go through the RESOLVED tenant's scoped store —
/// the construction-level global filter makes any other wiring read nothing (fail-closed), while the
/// fake leg carries the scope at parameter level. Identifiers are distinct per test because both legs
/// share one database in the wired run. Stores own their resources and are disposed by the tests.
/// 契約套件：場景由 spec 增量派生；寫入經平台入口 store、讀取經已解析租戶的 scoped store；各測試識別互異且 store 皆 dispose。
/// </summary>
public abstract class TenantStoreContractBase
{
    /// <summary>Fake leg is a no-op; the wired leg applies migrations before the first subject.</summary>
    protected virtual Task WaitForMigrationsAsync() => Task.CompletedTask;

    /// <summary>Creates the subject store for a tenant scope (null = platform/provisioning view).</summary>
    protected abstract Task<ITenantStore> CreateScopeStoreAsync(string? activeTenantId);

    private static Tenant ProvisionedTenant(string tenantId, string organizationName)
    {
        return Tenant.Provision(tenantId, $"{tenantId}-name", organizationName, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task Saves_and_resolves_a_tenant_with_its_initial_organization()
    {
        const string tenantId = "acme-save";
        await WaitForMigrationsAsync();

        await using var platform = await CreateScopeStoreAsync(null);
        var tenant = ProvisionedTenant(tenantId, "Acme HQ");
        await platform.SaveAsync(tenant);

        await using var member = await CreateScopeStoreAsync(tenantId);
        Assert.True(await member.TenantExistsAsync(tenantId));

        var page = await member.ListOrganizationsAsync(tenantId, limit: 1);
        Assert.False(page.HasNext);
        var entry = Assert.Single(page.Items);
        Assert.Equal(tenant.Organizations[0].OrganizationId, entry.OrganizationId);
        Assert.Equal("Acme HQ", entry.Name);
    }

    [Fact]
    public async Task Duplicate_save_conflicts_and_persists_nothing_new()
    {
        const string tenantId = "acme-dup";
        await WaitForMigrationsAsync();

        await using var platform = await CreateScopeStoreAsync(null);
        await platform.SaveAsync(ProvisionedTenant(tenantId, "Acme HQ"));

        var conflict = await Assert.ThrowsAsync<TenantConflictException>(
            () => platform.SaveAsync(ProvisionedTenant(tenantId, "Acme HQ 2")));
        Assert.Equal(tenantId, conflict.TenantId);

        await using var member = await CreateScopeStoreAsync(tenantId);
        Assert.Single((await member.ListOrganizationsAsync(tenantId)).Items);
    }

    [Fact]
    public async Task Listings_exclude_other_tenants()
    {
        const string acmeId = "acme-lst";
        const string globexId = "globex-lst";
        await WaitForMigrationsAsync();

        await using var platform = await CreateScopeStoreAsync(null);
        var acme = ProvisionedTenant(acmeId, "Acme HQ");
        var globex = ProvisionedTenant(globexId, "Globex HQ");

        await platform.SaveAsync(acme);
        await platform.SaveAsync(globex);

        await using var acmeMember = await CreateScopeStoreAsync(acmeId);
        var acmeEntry = Assert.Single((await acmeMember.ListOrganizationsAsync(acmeId)).Items);
        Assert.Equal(acme.Organizations[0].OrganizationId, acmeEntry.OrganizationId);

        await using var globexMember = await CreateScopeStoreAsync(globexId);
        var globexEntry = Assert.Single((await globexMember.ListOrganizationsAsync(globexId)).Items);
        Assert.Equal(globex.Organizations[0].OrganizationId, globexEntry.OrganizationId);
    }

    [Fact]
    public async Task Unknown_and_cross_tenant_access_denied_as_not_found()
    {
        const string acmeId = "acme-x";
        await WaitForMigrationsAsync();

        await using var platform = await CreateScopeStoreAsync(null);
        var acme = ProvisionedTenant(acmeId, "Acme HQ");
        await platform.SaveAsync(acme);
        var acmeOrgId = acme.Organizations[0].OrganizationId;

        await using var outsider = await CreateScopeStoreAsync("globex-x");
        Assert.Empty((await outsider.ListOrganizationsAsync("globex-x")).Items);
        Assert.Null(await outsider.FindOrganizationAsync("globex-x", acmeOrgId));

        await using var member = await CreateScopeStoreAsync(acmeId);
        Assert.Null(await member.FindOrganizationAsync("unknown", acmeOrgId));
        Assert.NotNull(await member.FindOrganizationAsync(acmeId, acmeOrgId));
    }
}
