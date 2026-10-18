using Iam.Application.Parties;
using Iam.Application.Users;
using Iam.Domain.Party;
using Iam.Domain.Users;
using Xunit;

namespace Iam.ContractTests.Users;

/// <summary>
/// Locked persistence-seam contract suite (ADR-010) for per-tenant users, scenarios derived FROM the
/// identity/user-auth delta — never from the first implementation. Double-run: fake leg (in-memory)
/// AND wired leg (EF Core / Npgsql + tenant-scope guard over Testcontainers Postgres). Party anchors
/// come from the matching party contract store: a user always binds an EXISTING Party, which the
/// wired run makes structural via the required party_id FK. Login-name identity is (tenant, login);
/// differs-tenant-only rows stay independent. Identifiers are distinct per fact because both legs
/// share one Postgres database in the wired run (tenant-suite precedent).
/// 契約套件：場景由 user-auth 增量派生；使用者一律綁定既有 Party（wired 以 FK 驗證）；各事實識別互異且 store 皆 dispose。
/// </summary>
public abstract class UserStoreContractBase
{
    /// <summary>Fake leg is a no-op; the wired leg applies migrations before the first subject.</summary>
    protected virtual Task WaitForMigrationsAsync() => Task.CompletedTask;

    /// <summary>Creates the member-scoped user store for the given tenant.</summary>
    protected abstract Task<IUserStore> CreateUserStoreAsync(string activeTenantId);

    /// <summary>Creates the party anchor store (same contract family as the party seam).</summary>
    protected abstract Task<IPartyStore> CreatePartyStoreAsync();

    private static readonly DateTimeOffset FixedNow = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static Party PartyAnchor(string typeCode, string number)
    {
        return Party.Provision($"Party {number}", null, [PartyIdentifier.Create(typeCode, number)], FixedNow);
    }

    private async Task<Party> AnchoredPartyAsync(string typeCode, string number)
    {
        await using var partyStore = await CreatePartyStoreAsync();
        var party = PartyAnchor(typeCode, number);
        await partyStore.SaveAsync(party);
        return party;
    }

    protected async Task<User> SavedUserAsync(string tenantId, string loginName, string typeCode, string number)
    {
        var party = await AnchoredPartyAsync(typeCode, number);
        await using var userStore = await CreateUserStoreAsync(tenantId);
        var user = User.Provision(tenantId, loginName, party.PartyId, FixedNow);
        await userStore.SaveAsync(user);
        return user;
    }

    [Fact]
    public void User_store_port_exposes_no_listable_or_traversable_api()
    {
        var portType = typeof(IUserStore);
        var methodNames = portType.GetInterfaces().Prepend(portType)
            .SelectMany(interfaceType => interfaceType.GetMethods())
            .Select(method => method.Name)
            .Distinct()
            .Order()
            .ToArray();

        Assert.Equal((string[])["DisposeAsync", "FindAsync", "SaveAsync"], methodNames);
    }

    [Fact]
    public async Task Saves_and_resolves_user_by_tenant_and_login()
    {
        const string tenantId = "nina-u1";
        const string loginName = "nina-save";
        await WaitForMigrationsAsync();
        var saved = await SavedUserAsync(tenantId, loginName, "id-card", "nina-tup-1");

        await using var userStore = await CreateUserStoreAsync(tenantId);
        var found = await userStore.FindAsync(tenantId, loginName);

        Assert.NotNull(found);
        Assert.Equal(saved.UserId, found!.UserId);
        Assert.Equal(saved.PartyId, found.PartyId);
        Assert.True(found.IsActive);
    }

    [Fact]
    public async Task Unknown_login_returns_null()
    {
        await WaitForMigrationsAsync();

        await using var userStore = await CreateUserStoreAsync("nina-u2");
        Assert.Null(await userStore.FindAsync("nina-u2", "ghost-u2"));
    }

    [Fact]
    public async Task Duplicate_login_conflicts_within_tenant_and_persists_nothing_new()
    {
        const string tenantId = "nina-u3";
        await WaitForMigrationsAsync();
        var first = await SavedUserAsync(tenantId, "dupe-u3", "id-card", "nina-tup-3a");

        var otherParty = await AnchoredPartyAsync("passport", "nina-tup-3b");
        await using var userStore = await CreateUserStoreAsync(tenantId);
        var intruder = User.Provision(tenantId, "dupe-u3", otherParty.PartyId, FixedNow);

        var conflict = await Assert.ThrowsAsync<UserLoginConflictException>(() => userStore.SaveAsync(intruder));
        Assert.Equal(tenantId, conflict.TenantId);
        Assert.Equal("dupe-u3", conflict.LoginName);

        var found = await userStore.FindAsync(tenantId, "dupe-u3");
        Assert.NotNull(found);
        Assert.Equal(first.UserId, found!.UserId);
    }

    [Fact]
    public async Task Same_login_in_other_tenants_is_independent()
    {
        const string tenantA = "nina-u4a";
        const string tenantB = "nina-u4b";
        await WaitForMigrationsAsync();
        var userA = await SavedUserAsync(tenantA, "shared-u4", "id-card", "nina-tup-4a");
        var userB = await SavedUserAsync(tenantB, "shared-u4", "passport", "nina-tup-4b");
        Assert.NotEqual(userA.UserId, userB.UserId);

        await using var storeA = await CreateUserStoreAsync(tenantA);
        await using var storeB = await CreateUserStoreAsync(tenantB);
        var inA = await storeA.FindAsync(tenantA, "shared-u4");
        Assert.NotNull(inA);
        Assert.Equal(userA.UserId, inA!.UserId);
        var fromB = await storeB.FindAsync(tenantA, "shared-u4");
        Assert.Null(fromB);
    }
}
