using System.Reflection;
using Iam.Application.Users;
using Xunit;

namespace Iam.ContractTests.Auth;

/// <summary>
/// Credential store port contract (auth seam; fake leg first, wired EF leg re-runs the SAME facts):
/// the port exposes NO listable or traversable API, an unknown tenant and an unknown login are
/// indistinguishable (both resolve to null), the stored hash round-trips exactly, and logins with
/// the same name in other tenants stay independent. Seeds ride the abstract user hook because the
/// EF store requires a provisioned users row (FK); the fake needs none.
/// 憑證儲存埠契約：無列出／遍歷面；未知租戶與未知名不可區分（皆 null）；雜湊精確往返；跨租戶同登入獨立。需先備 user 列（FK），假腿無此需求。
/// </summary>
public abstract class UserCredentialStoreContractBase
{
    protected abstract Task<IUserCredentialStore> CreateCredentialStoreAsync();

    /// <summary>Seeds the users row the pair address (fake: no-op; wired: provisions party + user).</summary>
    protected abstract Task SeedUserAsync(string tenantId, string loginName);

    /// <summary>Wired: applies migrations; fake: no-op.</summary>
    protected abstract Task WaitForSeamAsync();

    [Fact]
    public void Credential_store_port_exposes_no_listable_or_traversable_api()
    {
        var portType = typeof(IUserCredentialStore);
        var methodNames = portType.GetInterfaces().Prepend(portType)
            .SelectMany(interfaceType => interfaceType.GetMethods())
            .Select(method => method.Name)
            .Distinct()
            .Order()
            .ToArray();

        Assert.Equal((string[])["FindPasswordHashAsync", "SavePasswordHashAsync"], methodNames);
    }

    [Fact]
    public async Task Saves_and_resolves_the_hash_by_tenant_and_login()
    {
        const string tenantId = "nina-c1";
        const string loginName = "cred-u1";
        await WaitForSeamAsync();
        var credentialStore = await CreateCredentialStoreAsync();
        await SeedUserAsync(tenantId, loginName);

        Assert.Null(await credentialStore.FindPasswordHashAsync(tenantId, loginName));
        await credentialStore.SavePasswordHashAsync(tenantId, loginName, "stored-hash-c1");
        var found = await credentialStore.FindPasswordHashAsync(tenantId, loginName);

        Assert.Equal("stored-hash-c1", found);
    }

    [Fact]
    public async Task Unknown_tenant_and_unknown_login_both_resolve_null()
    {
        await WaitForSeamAsync();
        var credentialStore = await CreateCredentialStoreAsync();

        var unknownTenant = await credentialStore.FindPasswordHashAsync("ghost-c-tent", "cred-ghost");
        var unknownLogin = await credentialStore.FindPasswordHashAsync("nina-c2", "cred-ghost");

        Assert.Null(unknownTenant);
        Assert.Null(unknownLogin);
    }

    [Fact]
    public async Task Same_login_in_another_tenant_is_independent()
    {
        const string tenantId = "nina-c3";
        await WaitForSeamAsync();
        var credentialStore = await CreateCredentialStoreAsync();
        await SeedUserAsync(tenantId, "shared-cred");

        await credentialStore.SavePasswordHashAsync(tenantId, "shared-cred", "stored-hash-c3");

        Assert.Equal("stored-hash-c3", await credentialStore.FindPasswordHashAsync(tenantId, "shared-cred"));
        Assert.Null(await credentialStore.FindPasswordHashAsync("nina-c4", "shared-cred"));
    }
}
