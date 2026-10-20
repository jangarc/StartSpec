using System.Globalization;
using System.Net.Http.Json;
using Iam.Application.Parties;
using Iam.Domain.Party;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using Xunit;

namespace Iam.Reqnroll;

/// <summary>
/// Step bindings for the party identity contract scenarios (task 3.3). Party identity exposes no HTTP
/// surface by design (exact-identifier reach only), so steps resolve the real Application use cases
/// from the hosted app's scoped container (wired to Testcontainers Postgres) and assert through the
/// same scoped IPartyStore the use cases share. Identifier tuple numbers carry the run-wide suffix so
/// the shared wired database stays conflict-free across scenarios.
/// 步驟綁定：經宿主 scoped 容器驅動真實 use case；元組證號帶全域場景字尾避免共用庫衝突。
/// </summary>
[Binding]
public sealed class PartyFeatureSteps(ScenarioContext scenarioContext)
{
    private string? _suffix;
    private static readonly DateOnly AdaDateOfBirth = new(1990, 3, 1);

    private readonly IServiceScope _scope = scenarioContext.Get<IServiceScope>();
    private readonly HttpClient _client = scenarioContext.Get<HttpClient>();
    private (string TypeCode, string Number) _currentTuple;
    private Guid _currentPartyId;
    private ProvisionPartyResponse? _lastProvision;
    private AttachPartyRoleResponse? _lastAttach;
    private Guid _previousPartyId;
    private DateTimeOffset _grantedAt;
    private readonly Dictionary<string, Guid> _partyIdByNumber = new();
    private readonly Dictionary<string, string> _scopedTenantIds = new();
    private readonly HashSet<(string TenantId, string RoleType)> _attachedRoles = [];

    private string Suffix() => _suffix ??= Harness.NextSuffix();

    private string ScopedTenantId(string tenantLabel)
    {
        return _scopedTenantIds.TryGetValue(tenantLabel, out var scoped)
            ? scoped
            : _scopedTenantIds[tenantLabel] = $"{tenantLabel}-{Suffix()}";
    }

    private (string TypeCode, string Number) Tuple(string typeCode, string label)
    {
        _currentTuple = (typeCode, $"{label}-{Suffix()}");
        return _currentTuple;
    }

    private IPartyStore Store() => _scope.ServiceProvider.GetRequiredService<IPartyStore>();

    private async Task<Party?> ResolveCurrentPartyAsync()
    {
        return await Store().FindExactAsync(PartyIdentifier.Create(_currentTuple.TypeCode, _currentTuple.Number));
    }

    [Given(@"^no party has ever been resolved with the unique identifier tuple$")]
    public async Task GivenNoPartyExistsForTuple()
    {
        _ = Tuple("id-card", "one");
        Assert.Null(await ResolveCurrentPartyAsync());
    }

    [Given(@"^a party exists carrying the unique identifier tuple$")]
    public async Task GivenPartyExistsWithTuple()
    {
        var tuple = Tuple("id-card", "one");
        _currentPartyId = (await ProvisionPartyAsync("Ada Chen", AdaDateOfBirth, tuple)).PartyId;
    }

    [Given(@"^the tenant ""([^""]*)"" is prepared named ""([^""]*)"" with the initial organization ""([^""]*)""$")]
    public async Task GivenTenantIsPrepared(string tenantLabel, string tenantDisplayName, string organizationName)
    {
        var bearer = await CallerAuth.ReadyBootstrapCallerAsync(scenarioContext);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/iam/tenants");
        request.Headers.Add("Authorization", bearer);
        request.Content = JsonContent.Create(new { tenantId = ScopedTenantId(tenantLabel), tenantName = tenantDisplayName, initialOrganizationName = organizationName });
        using var response = await _client.SendAsync(request);
        Assert.Equal(201, (int)response.StatusCode);
    }

    [Given(@"^a party carrying the unique identifier tuple plays the ""([^""]*)"" role in tenant ""([^""]*)""$")]
    public async Task GivenPartyPlaysRoleInTenant(string roleType, string tenantLabel)
    {
        var tuple = Tuple("id-card", "one");
        _currentPartyId = (await ProvisionPartyAsync("Ada Chen", AdaDateOfBirth, tuple)).PartyId;
        await AttachRoleAsync(ScopedTenantId(tenantLabel), roleType);
    }

    [Given(@"^that party holds consent ""([^""]*)"" granted in tenant ""([^""]*)""$")]
    public async Task GivenPartyHoldsConsent(string purpose, string tenantLabel)
    {
        await Services.GetRequiredService<GrantConsentUseCase>().HandleAsync(
            new GrantConsentCommand(ScopedTenantId(tenantLabel), purpose, _currentTuple.TypeCode, _currentTuple.Number));

        var party = await ResolveCurrentPartyAsync();
        Assert.NotNull(party);
        _grantedAt = party.Consents.Single(record => record.Kind == ConsentKind.Grant).OccurredAt;
    }

    [When(@"^the operator provisions a party ""([^""]*)"" born ([0-9\-]+) with that identifier tuple$")]
    public async Task WhenOperatorProvisionsPartyWithThatTuple(string displayName, string born)
    {
        var response = await ProvisionPartyAsync(displayName, DateOnly.Parse(born, CultureInfo.InvariantCulture), _currentTuple);
        _lastProvision = response;
    }

    [When(@"^the operator provisions a party ""([^""]*)"" born ([0-9\-]+) with the same identifier tuple$")]
    public async Task WhenOperatorProvisionsPartyWithSameTuple(string displayName, string born)
    {
        _lastProvision = await ProvisionPartyAsync(displayName, DateOnly.Parse(born, CultureInfo.InvariantCulture), _currentTuple);
    }

    [When(@"^the operator provisions a party ""([^""]*)"" born ([0-9\-]+) with a different identifier tuple$")]
    public async Task WhenOperatorProvisionsPartyWithDifferentTuple(string displayName, string born)
    {
        _previousPartyId = _partyIdByNumber[_currentTuple.Number];
        var nearTuple = Tuple(_currentTuple.TypeCode, "near");
        _lastProvision = await ProvisionPartyAsync(displayName, DateOnly.Parse(born, CultureInfo.InvariantCulture), nearTuple);
    }

    [When(@"^the caller scoped to tenant ""([^""]*)"" requests party listings$")]
    public async Task WhenCallerRequestsPartyListings(string tenantLabel)
    {
        var bearer = await CallerAuth.BearerForAsync(scenarioContext, ScopedTenantId(tenantLabel));
        using var request = new HttpRequestMessage(HttpMethod.Get, "/iam/parties");
        request.Headers.Add("Authorization", bearer);
        using var response = await _client.SendAsync(request);
        scenarioContext["partyListStatus"] = (int)response.StatusCode;
    }

    [When(@"^the caller registers that identifier tuple as an ""([^""]*)"" in tenant ""([^""]*)""$")]
    public async Task WhenCallerRegistersTupleAsRole(string roleType, string tenantLabel)
    {
        await AttachRoleAsync(ScopedTenantId(tenantLabel), roleType);
    }

    [When(@"^the caller records consent ""([^""]*)"" granted by that party in tenant ""([^""]*)""$")]
    public async Task WhenCallerRecordsConsent(string purpose, string tenantLabel)
    {
        await Services.GetRequiredService<GrantConsentUseCase>().HandleAsync(
            new GrantConsentCommand(ScopedTenantId(tenantLabel), purpose, _currentTuple.TypeCode, _currentTuple.Number));
    }

    [When(@"^the ""([^""]*)"" role of the party in tenant ""([^""]*)"" is deactivated$")]
    public async Task WhenRoleOfPartyInTenantIsDeactivated(string roleType, string tenantLabel)
    {
        await Services.GetRequiredService<DeactivatePartyRoleUseCase>().HandleAsync(
            new DeactivatePartyRoleCommand(ScopedTenantId(tenantLabel), roleType, _currentTuple.TypeCode, _currentTuple.Number));
    }

    [When(@"^the caller records that the party revokes the consent ""([^""]*)"" in tenant ""([^""]*)""$")]
    public async Task WhenCallerRecordsConsentRevocation(string purpose, string tenantLabel)
    {
        await Services.GetRequiredService<RevokeConsentUseCase>().HandleAsync(
            new RevokeConsentCommand(ScopedTenantId(tenantLabel), purpose, _currentTuple.TypeCode, _currentTuple.Number));
    }

    [Then(@"^the system creates the party$")]
    public void ThenTheSystemCreatesTheParty()
    {
        Assert.NotNull(_lastProvision);
        Assert.True(_lastProvision!.CreatedNew);
        Assert.NotEqual(Guid.Empty, _lastProvision.PartyId);
    }

    [Then(@"^the system binds to the existing party and creates no duplicate$")]
    public async Task ThenTheSystemBindsWithoutDuplication()
    {
        Assert.NotNull(_lastProvision);
        Assert.False(_lastProvision!.CreatedNew);
        Assert.Equal(_currentPartyId, _lastProvision.PartyId);

        var party = await ResolveCurrentPartyAsync();
        Assert.NotNull(party);
        Assert.Equal(_currentPartyId, party.PartyId);
        Assert.Single(party.Identifiers);
    }

    [Then(@"^the system creates a new party instead of merging$")]
    public void ThenTheSystemCreatesNewPartyInsteadOfMerging()
    {
        Assert.NotNull(_lastProvision);
        Assert.True(_lastProvision!.CreatedNew);
        Assert.NotEqual(_previousPartyId, _lastProvision.PartyId);
    }

    [Then(@"^the system denies the request because parties cannot be listed or traversed$")]
    public void ThenTheSystemDeniesPartyListing()
    {
        Assert.Equal(404, scenarioContext.Get<int>("partyListStatus"));
    }

    [Then(@"^the system binds the new tenant role to that party without exposing other data$")]
    public void ThenTheSystemBindsRoleWithoutExposure()
    {
        Assert.Equal(_currentPartyId, _lastAttach!.PartyId);

        var exposedMembers = typeof(AttachPartyRoleResponse)
            .GetProperties()
            .Select(property => property.Name)
            .Order()
            .ToArray();
        Assert.Equal((string[])["PartyId"], exposedMembers);
    }

    [Then(@"^the party keeps its identifier tuple$")]
    public async Task ThenThePartyKeepsItsIdentifierTuple()
    {
        var party = await ResolveCurrentPartyAsync();
        Assert.NotNull(party);
        Assert.Single(party.Identifiers);
        var identifier = party.Identifiers.Single();
        Assert.Equal(_currentTuple.TypeCode, identifier.TypeCode);
        Assert.Equal(_currentTuple.Number, identifier.Number);
    }

    [Then(@"^its consent history remains intact$")]
    public async Task ThenConsentHistoryRemainsIntact()
    {
        var party = await ResolveCurrentPartyAsync();
        Assert.NotNull(party);
        Assert.Single(party.Consents);
        Assert.Equal(ConsentKind.Grant, party.Consents[0].Kind);
        Assert.Equal(_grantedAt, party.Consents[0].OccurredAt);
        Assert.All(party.Roles, role => Assert.False(role.IsActive));
    }

    [Then(@"^the ""([^""]*)"" role is added to the same party$")]
    public void ThenRoleIsAddedToSameParty(string roleType)
    {
        Assert.Equal(_currentPartyId, _lastAttach!.PartyId);
    }

    [Then(@"^deactivating each of the party's roles succeeds$")]
    public async Task ThenDeactivatingEachRoleSucceeds()
    {
        var deactivate = _scope.ServiceProvider.GetRequiredService<DeactivatePartyRoleUseCase>();
        Assert.NotEmpty(_attachedRoles);
        foreach (var (tenantId, roleType) in _attachedRoles)
        {
            await deactivate.HandleAsync(new DeactivatePartyRoleCommand(tenantId, roleType, _currentTuple.TypeCode, _currentTuple.Number));
        }
    }

    [Then(@"^the system stores a timestamped immutable consent record$")]
    public async Task ThenConsentRecordIsTimestamped()
    {
        var party = await ResolveCurrentPartyAsync();
        Assert.NotNull(party);
        var record = Assert.Single(party.Consents);
        Assert.Equal(ConsentKind.Grant, record.Kind);
        Assert.True(record.OccurredAt > DateTimeOffset.MinValue);
    }

    [Then(@"^the system appends a revocation record and retains the original$")]
    public async Task ThenRevocationAppendsAndRetainsOriginal()
    {
        var party = await ResolveCurrentPartyAsync();
        Assert.NotNull(party);
        Assert.Equal(2, party.Consents.Count);
        Assert.Equal(ConsentKind.Grant, party.Consents[0].Kind);
        Assert.Equal(_grantedAt, party.Consents[0].OccurredAt);
        Assert.Equal(ConsentKind.Revoke, party.Consents[1].Kind);
    }

    private IServiceProvider Services => _scope.ServiceProvider;

    private async Task<ProvisionPartyResponse> ProvisionPartyAsync(string displayName, DateOnly dateOfBirth, (string TypeCode, string Number) tuple)
    {
        var response = await Services.GetRequiredService<ProvisionPartyUseCase>().HandleAsync(
            new ProvisionPartyCommand(displayName, dateOfBirth, tuple.TypeCode, tuple.Number));
        _partyIdByNumber[tuple.Number] = response.PartyId;
        return response;
    }

    private async Task AttachRoleAsync(string tenantId, string roleType)
    {
        _lastAttach = await Services.GetRequiredService<AttachPartyRoleUseCase>().HandleAsync(
            new AttachPartyRoleCommand(tenantId, roleType, _currentTuple.TypeCode, _currentTuple.Number));
        _attachedRoles.Add((tenantId, roleType));
    }
}
