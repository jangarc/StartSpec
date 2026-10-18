using Iam.Application.Parties;
using Iam.Domain.Party;
using Xunit;

namespace Iam.ContractTests.Parties;

/// <summary>
/// Locked persistence-seam contract suite for the platform-level Party store, scenarios derived FROM
/// the identity/party spec delta. Double-run: fake leg (in-memory) AND wired leg (EF Core / Npgsql on
/// Testcontainers Postgres). The guard is STRUCTURAL: the port exposes only exact-tuple reach and
/// whole-aggregate save — no listable or traversable surface exists to call. Identifier tuples are
/// distinct per fact because both legs share one Postgres database in the wired run (tenant-suite precedent).
/// 契約套件：場景由 spec 派生；守衛為結構性——port 僅精確比對與整聚合保存，無列出/遍歷面；
/// wired 跑兩 legs 共用同一 Postgres 資料庫，故每事實證件元組互異（同 tenant 契約先例）。
/// </summary>
public abstract class PartyStoreContractBase
{
    /// <summary>Fake leg is a no-op; the wired leg applies migrations before the first subject.</summary>
    protected virtual Task WaitForMigrationsAsync() => Task.CompletedTask;

    protected abstract Task<IPartyStore> CreateStoreAsync();

    private static readonly DateTimeOffset Now = new(2026, 10, 18, 3, 4, 5, TimeSpan.Zero);
    private static readonly DateOnly AdaBorn = new(1990, 3, 1);

    private static readonly PartyIdentifier AdaSave = PartyIdentifier.Create("id-card", "A1");
    private static readonly PartyIdentifier IdCardTwo = PartyIdentifier.Create("id-card", "A2");
    private static readonly PartyIdentifier PassportTwo = PartyIdentifier.Create("passport", "P2");
    private static readonly PartyIdentifier SavedNearBase = PartyIdentifier.Create("id-card", "A3");
    private static readonly PartyIdentifier NearNumber = PartyIdentifier.Create("id-card", "B3");
    private static readonly PartyIdentifier NearType = PartyIdentifier.Create("passport", "Q3");
    private static readonly PartyIdentifier ClaimedOnce = PartyIdentifier.Create("id-card", "A4");
    private static readonly PartyIdentifier RoundtripTuple = PartyIdentifier.Create("id-card", "A5");

    private static Party AdaParty(PartyIdentifier tuple)
    {
        return Party.Provision("Ada Chen", AdaBorn, [tuple], Now);
    }

    [Fact]
    public void Party_store_port_exposes_no_listable_or_traversable_api()
    {
        var portType = typeof(IPartyStore);
        var methodNames = portType.GetInterfaces().Prepend(portType)
            .SelectMany(interfaceType => interfaceType.GetMethods())
            .Select(method => method.Name)
            .Distinct()
            .Order()
            .ToArray();

        Assert.Equal((string[])["DisposeAsync", "FindExactAsync", "SaveAsync"], methodNames);
    }

    [Fact]
    public async Task Provision_new_party_persists_and_matches_exactly()
    {
        await WaitForMigrationsAsync();
        await using var store = await CreateStoreAsync();
        var party = AdaParty(AdaSave);
        await store.SaveAsync(party);

        var hit = await store.FindExactAsync(AdaSave);

        Assert.NotNull(hit);
        Assert.Equal(party.PartyId, hit.PartyId);
        Assert.Equal("Ada Chen", hit.DisplayName);
        Assert.Equal(AdaBorn, hit.DateOfBirth);
        Assert.Single(hit.Identifiers);
    }

    [Fact]
    public async Task Any_identifier_tuple_of_a_party_resolves_it()
    {
        await WaitForMigrationsAsync();
        await using var store = await CreateStoreAsync();
        var party = Party.Provision(
            "Ada Chen",
            AdaBorn,
            [IdCardTwo, PassportTwo],
            Now);
        await store.SaveAsync(party);

        var viaIdCard = await store.FindExactAsync(IdCardTwo);
        var viaPassport = await store.FindExactAsync(PassportTwo);

        Assert.NotNull(viaIdCard);
        Assert.NotNull(viaPassport);
        Assert.Equal(viaIdCard.PartyId, viaPassport.PartyId);
    }

    [Fact]
    public async Task Near_match_tuple_never_resolves_or_merges()
    {
        // Same person, different number, or a same-number foreign document: neither tuple is the
        // exact identity — near matches SHALL create a NEW party, never merge into this one.
        await WaitForMigrationsAsync();
        await using var store = await CreateStoreAsync();
        await store.SaveAsync(AdaParty(SavedNearBase));

        Assert.Null(await store.FindExactAsync(NearNumber));
        Assert.Null(await store.FindExactAsync(NearType));
    }

    [Fact]
    public async Task Saving_a_tuple_already_claimed_conflicts_and_retains_the_original()
    {
        await WaitForMigrationsAsync();
        await using var store = await CreateStoreAsync();
        var party = AdaParty(ClaimedOnce);
        await store.SaveAsync(party);

        var intruder = Party.Provision("Ada Clone", AdaBorn, [ClaimedOnce], Now);
        await Assert.ThrowsAsync<PartyIdentifierConflictException>(() => store.SaveAsync(intruder));

        var hit = await store.FindExactAsync(ClaimedOnce);
        Assert.NotNull(hit);
        Assert.Equal(party.PartyId, hit.PartyId);
        Assert.Equal("Ada Chen", hit.DisplayName);
    }

    [Fact]
    public async Task Roundtrip_preserves_role_and_consent_history()
    {
        // Deactivation preserves history (identity/party spec): roles, consent grant + revocation
        // records and original timestamps survive persistence unchanged.
        await WaitForMigrationsAsync();
        await using var store = await CreateStoreAsync();
        var party = AdaParty(RoundtripTuple);
        party.AttachTenantRole("acme", "employee", Now);
        party.GrantConsent("acme", "treatment-data", Now);
        party.RevokeConsent("acme", "treatment-data", Now.AddMinutes(1));
        party.DeactivateTenantRole("acme", "employee", Now.AddMinutes(2));
        await store.SaveAsync(party);

        var hit = await store.FindExactAsync(RoundtripTuple);

        Assert.NotNull(hit);
        Assert.Single(hit.Identifiers);
        Assert.Equal("Ada Chen", hit.DisplayName);

        var role = Assert.Single(hit.Roles);
        Assert.Equal("acme", role.TenantId);
        Assert.Equal("employee", role.RoleType);
        Assert.False(role.IsActive);
        Assert.Equal(Now.AddMinutes(2), role.DeactivatedAt);

        Assert.Equal(2, hit.Consents.Count);
        Assert.Contains(hit.Consents, record => record.Kind == ConsentKind.Grant && record.TenantId == "acme" && record.Purpose == "treatment-data" && record.OccurredAt == Now);
        Assert.Contains(hit.Consents, record => record.Kind == ConsentKind.Revoke && record.OccurredAt == Now.AddMinutes(1));
    }
}
