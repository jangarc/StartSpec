using System.Net.Http.Json;
using System.Text.Json;
using Iam.Application.Users;
using Iam.Domain.Users;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using Xunit;

namespace Iam.Reqnroll;

/// <summary>
/// Step bindings for the UserAuth API contract scenarios (task 4.4). All assertions ride the real
/// HTTP surface of Hosts.HQ over wired Postgres; the disabled-user precondition rides the real
/// Application seam (IUserStoreScope/User.Disable) through the hosted app's DI.
/// UserAuth 場景步驟：全走真實 HTTP 面；停用前置經宿主 DI 的 Application 接縫完成。
/// </summary>
[Binding]
public sealed class UserAuthFeatureSteps(ScenarioContext scenarioContext)
{
    private readonly HttpClient _client = scenarioContext.Get<HttpClient>();

    private string? _suffix;
    private const string Password = "Correct-Operator-0000";
    private const string TenantHeaderName = "X-Tenant-Id";
    private readonly Dictionary<string, string> _numberByTenant = new();
    private readonly Dictionary<string, Guid> _userIdByTenant = new();
    private readonly Dictionary<string, Guid> _partyByTenant = new();

    private Guid _priorUserId;
    private Guid _priorPartyId;
    private Guid _boundUserId;
    private Guid _boundPartyId;

    private int _provisionStatus;
    private string _provisionBody = "";

    private int _lastSignInStatus;
    private string _lastSignInBody = "";
    private int _firstRejectionStatus;
    private string _firstRejectionBody = "";
    private int _secondRejectionStatus;
    private string _secondRejectionBody = "";

    private string? _currentAccessToken;
    private string? _currentRefreshToken;
    private string? _previousAccessToken;
    private string? _previousRefreshToken;
    private string? _invalidatedRefreshToken;

    private int _refreshStatus;
    private string _refreshBody = "";

    private string Suffix() => _suffix ??= Harness.NextSuffix();

    private string ScopedTenantId(string tenantName) => $"{tenantName}-{Suffix()}";

    [Given(@"^the tenant ""([^""]*)"" is provisioned named ""([^""]*)"" with the initial organization ""([^""]*)""$")]
    public async Task GivenPreparedTenant(string tenantName, string tenantDisplayName, string organizationName)
    {
        await GivenPreparedTenantForId(ScopedTenantId(tenantName), tenantDisplayName, organizationName);
    }

    private async Task GivenPreparedTenantForId(string scopedTenant, string tenantDisplayName, string organizationName)
    {
        using var response = await _client.PostAsJsonAsync(
            "/iam/tenants",
            new { tenantId = scopedTenant, tenantName = tenantDisplayName, initialOrganizationName = organizationName });
        response.EnsureSuccessStatusCode();
    }

    [Given(@"^a user ""([^""]*)"" is provisioned displayed ""([^""]*)"" born ([0-9-]+) in tenant ""([^""]*)""$")]
    public async Task GivenProvisionedUser(string loginName, string displayName, string dateText, string tenantKey)
    {
        await ProvisionAsync(tenantKey, ScopedTenantId(tenantKey), loginName, displayName, dateText, $"{loginName}-identity-{Suffix()}");
    }

    [When(@"^I provision a user ""([^""]*)"" displayed ""([^""]*)"" born ([0-9-]+) with the identifier ""([^""]*)"" in tenant ""([^""]*)""$")]
    public async Task WhenProvisionWithExplicitIdentifier(string loginName, string displayName, string dateText, string identifierBase, string tenantKey)
    {
        await ProvisionAsync(tenantKey, ScopedTenantId(tenantKey), loginName, displayName, dateText, $"{identifierBase}-{Suffix()}");
    }

    [When(@"^I provision a user ""([^""]*)"" displayed ""([^""]*)"" born ([0-9-]+) with that user's identifier in tenant ""([^""]*)""$")]
    public async Task WhenProvisionWithThatUsersIdentifier(string loginName, string displayName, string dateText, string tenantKey)
    {
        var scopedTenant = ScopedTenantId(tenantKey);
        _priorUserId = _userIdByTenant[tenantKey];
        _priorPartyId = _partyByTenant[tenantKey];
        await ProvisionAsync(tenantKey, scopedTenant, loginName, displayName, dateText, _numberByTenant[tenantKey]);
    }

    [When(@"^I provision a user ""([^""]*)"" displayed ""([^""]*)"" born ([0-9-]+) with a fresh identifier in tenant ""([^""]*)""$")]
    public async Task WhenProvisionWithFreshIdentifier(string loginName, string displayName, string dateText, string tenantKey)
    {
        await ProvisionAsync(tenantKey, ScopedTenantId(tenantKey), loginName, displayName, dateText, $"{loginName}-fresh-{Suffix()}");
    }

    private async Task ProvisionAsync(string tenantKey, string scopedTenant, string loginName, string displayName, string dateText, string number)
    {
        _ = DateOnly.TryParse(dateText, out var dateOfBirth);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/iam/users");
        request.Headers.Add(TenantHeaderName, scopedTenant);
        request.Content = JsonContent.Create(new { loginName, displayName, dateOfBirth, typeCode = "id-card", number });
        using var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        _provisionStatus = (int)response.StatusCode;
        _provisionBody = body;

        if ((int)response.StatusCode == 201)
        {
            using var document = JsonDocument.Parse(body);
            _boundUserId = document.RootElement.GetProperty("userId").GetGuid();
            _boundPartyId = document.RootElement.GetProperty("partyId").GetGuid();
            _userIdByTenant[tenantKey] = _boundUserId;
            _partyByTenant[tenantKey] = _boundPartyId;
            _numberByTenant.TryAdd(tenantKey, number);
        }
    }

    [Given(@"^the operator ""([^""]*)"" with the password ""([^""]*)"" exists in tenant ""([^""]*)"" named ""([^""]*)"" with the initial organization ""([^""]*)""$")]
    public async Task GivenOperatorExistsWithPassword(string loginName, string password, string tenantKey, string tenantDisplayName, string organizationName)
    {
        var scopedTenant = ScopedTenantId(tenantKey);
        await GivenPreparedTenantForId(scopedTenant, tenantDisplayName, organizationName);
        await ProvisionAsync(tenantKey, scopedTenant, loginName, $"{loginName} Operator", "1990-01-01", $"{loginName}-identity-{Suffix()}");

        var services = scenarioContext.Get<IServiceScope>().ServiceProvider;
        var hasher = services.GetRequiredService<IPasswordHasher>();
        var credentials = services.GetRequiredService<IUserCredentialStore>();
        await credentials.SavePasswordHashAsync(scopedTenant, loginName, hasher.Hash(password));
    }

    private async Task SignInAsync(string tenantId, string loginName, string password, bool rejectionSlot)
    {
        using var response = await _client.PostAsJsonAsync("/iam/auth/sign-in", new { tenantId, loginName, password });
        var status = (int)response.StatusCode;
        var body = await response.Content.ReadAsStringAsync();
        if (rejectionSlot)
        {
            if (_firstRejectionStatus == 0)
            {
                _firstRejectionStatus = status;
                _firstRejectionBody = body;
            }
            else
            {
                _secondRejectionStatus = status;
                _secondRejectionBody = body;
            }
        }
        else
        {
            _lastSignInStatus = status;
            _lastSignInBody = body;
        }

        if (status == 200)
        {
            using var document = JsonDocument.Parse(body);
            _previousAccessToken = _currentAccessToken;
            _previousRefreshToken = _currentRefreshToken;
            _currentAccessToken = document.RootElement.GetProperty("accessToken").GetString();
            _currentRefreshToken = document.RootElement.GetProperty("refreshToken").GetString();
        }
    }

    [When(@"^""([^""]*)"" signs in to tenant ""([^""]*)"" with that password$")]
    public async Task WhenSignInWithThatPassword(string loginName, string tenantKey)
    {
        await SignInAsync(ScopedTenantId(tenantKey), loginName, Password, false);
    }

    [When(@"^""([^""]*)"" attempts sign-in to tenant ""([^""]*)"" with a wrong password$")]
    public async Task WhenSignInWithWrongPassword(string loginName, string tenantKey)
    {
        await SignInAsync(ScopedTenantId(tenantKey), loginName, "Incorrect-Password-999", true);
    }

    [When(@"^an unknown login ""([^""]*)"" attempts sign-in to tenant ""([^""]*)""$")]
    public async Task WhenUnknownLoginSignIn(string loginName, string tenantKey)
    {
        await SignInAsync(ScopedTenantId(tenantKey), loginName, Password, true);
    }

    [When(@"^an unknown tenant attempts sign-in as ""([^""]*)"" with the same password$")]
    public async Task WhenUnknownTenantSignIn(string loginName)
    {
        await SignInAsync($"unknown-t-{Suffix()}", loginName, Password, true);
    }

    [Given(@"^a session pair was issued for ""([^""]*)"" in tenant ""([^""]*)""$")]
    public async Task GivenSessionPairIssued(string loginName, string tenantKey)
    {
        await SignInAsync(ScopedTenantId(tenantKey), loginName, Password, false);
    }

    [Given(@"^a session pair was issued and rotated once for ""([^""]*)"" in tenant ""([^""]*)""$")]
    public async Task GivenSessionPairRotated(string loginName, string tenantKey)
    {
        await SignInAsync(ScopedTenantId(tenantKey), loginName, Password, false);
        await ExchangeCurrentPairAsync();
        _invalidatedRefreshToken = _previousRefreshToken;
    }

    private async Task ExchangeCurrentPairAsync()
    {
        await PostRefreshAsync(_currentRefreshToken!);
        if (_refreshStatus == 200)
        {
            using var document = JsonDocument.Parse(_refreshBody);
            _previousAccessToken = _currentAccessToken;
            _previousRefreshToken = _currentRefreshToken;
            _currentAccessToken = document.RootElement.GetProperty("accessToken").GetString();
            _currentRefreshToken = document.RootElement.GetProperty("refreshToken").GetString();
        }
    }

    private async Task PostRefreshAsync(string refreshToken)
    {
        using var response = await _client.PostAsJsonAsync("/iam/auth/refresh", new { refreshToken });
        _refreshStatus = (int)response.StatusCode;
        _refreshBody = await response.Content.ReadAsStringAsync();
    }

    [Given(@"^the operator ""([^""]*)"" in tenant ""([^""]*)"" is disabled$")]
    public async Task GivenOperatorDisabled(string loginName, string tenantKey)
    {
        var scopedTenant = ScopedTenantId(tenantKey);
        var services = scenarioContext.Get<IServiceScope>().ServiceProvider;
        var userStores = services.GetRequiredService<IUserStoreScope>();
        await using var store = userStores.Member(scopedTenant);
        var user = await store.FindAsync(scopedTenant, loginName)
            ?? throw new InvalidOperationException($"User {loginName} was not provisioned in {scopedTenant}.");
        user.Disable(DateTimeOffset.UtcNow);
        await store.SaveAsync(user);
    }

    [When(@"^the recorded refresh token is replayed$")]
    public async Task WhenRecordedRefreshReplayed()
    {
        await PostRefreshAsync(_currentRefreshToken!);
    }

    [When(@"^the recorded refresh token is exchanged$")]
    public async Task WhenRecordedRefreshExchanged()
    {
        await ExchangeCurrentPairAsync();
    }

    [When(@"^the invalidated refresh token is replayed immediately$")]
    public async Task WhenInvalidatedRefreshReplayedImmediately()
    {
        await PostRefreshAsync(_invalidatedRefreshToken!);
    }

    [When(@"^the invalidated refresh token is replayed after the reuse window$")]
    public async Task WhenInvalidatedRefreshReplayedAfterWindow()
    {
        await Task.Delay(31_000);
        await PostRefreshAsync(_invalidatedRefreshToken!);
    }

    [When(@"^an unknown refresh token is presented$")]
    public async Task WhenUnknownRefreshPresented()
    {
        await PostRefreshAsync($"unknown-refresh-{Suffix()}");
    }

    [Then(@"^the system creates the user and returns the user and party identifiers$")]
    public void ThenUserCreated()
    {
        Assert.Equal(201, _provisionStatus);
        Assert.NotEqual(default, _boundUserId);
        Assert.NotEqual(default, _boundPartyId);
    }

    [Then(@"^the new user binds the existing user's party$")]
    public void ThenBindsExistingParty()
    {
        Assert.Equal(201, _provisionStatus);
        Assert.Equal(_priorPartyId, _boundPartyId);
        Assert.NotEqual(_priorUserId, _boundUserId);
    }

    [Then(@"^the system rejects the provisioning with a conflict error$")]
    public void ThenProvisioningConflict()
    {
        Assert.Equal(409, _provisionStatus);
        using var document = JsonDocument.Parse(_provisionBody);
        Assert.Equal("user_login_conflict", document.RootElement.GetProperty("title").GetString());
    }

    [Then(@"^the system returns the operator user identity and a fresh session pair$")]
    public void ThenSignInSuccess()
    {
        Assert.Equal(200, _lastSignInStatus);
        using var document = JsonDocument.Parse(_lastSignInBody);
        Assert.True(document.RootElement.TryGetProperty("userId", out _));
        Assert.True(document.RootElement.TryGetProperty("partyId", out _));
        Assert.False(string.IsNullOrEmpty(_currentAccessToken));
        Assert.False(string.IsNullOrEmpty(_currentRefreshToken));
    }

    [Then(@"^both sign-in failures share the identical generic rejection body$")]
    public void ThenBothSignInsGeneric()
    {
        Assert.Equal(401, _firstRejectionStatus);
        Assert.Equal(401, _secondRejectionStatus);
        Assert.Equal(_firstRejectionBody, _secondRejectionBody);
        using var document = JsonDocument.Parse(_firstRejectionBody);
        Assert.Equal("authentication_failed", document.RootElement.GetProperty("title").GetString());
    }

    [Then(@"^the rejection is indistinguishable from a wrong-password failure$")]
    public void ThenRejectionIndistinguishable()
    {
        Assert.Equal(401, _firstRejectionStatus);
        Assert.Equal(401, _secondRejectionStatus);
        Assert.Equal(_firstRejectionBody, _secondRejectionBody);
        using var document = JsonDocument.Parse(_firstRejectionBody);
        Assert.Equal("authentication_failed", document.RootElement.GetProperty("title").GetString());
    }

    [Then(@"^the system rejects the sign-in with the generic rejection$")]
    public void ThenSignInGenericRejection()
    {
        Assert.Equal(401, _lastSignInStatus);
        using var document = JsonDocument.Parse(_lastSignInBody);
        Assert.Equal("authentication_failed", document.RootElement.GetProperty("title").GetString());
    }

    [Then(@"^the refresh is rejected and no new tokens are returned$")]
    public void ThenRefreshRejectedNoTokens()
    {
        Assert.Equal(401, _refreshStatus);
        using var document = JsonDocument.Parse(_refreshBody);
        Assert.Equal("refresh_rejected", document.RootElement.GetProperty("title").GetString());
        Assert.False(document.RootElement.TryGetProperty("accessToken", out _));
        Assert.False(document.RootElement.TryGetProperty("refreshToken", out _));
    }

    [Then(@"^the system returns a new pair of tokens for the same scope$")]
    public void ThenNewPairReturned()
    {
        Assert.Equal(200, _refreshStatus);
        Assert.NotEqual(_previousRefreshToken, _currentRefreshToken);
        Assert.NotEqual(_previousAccessToken, _currentAccessToken);
    }

    [Then(@"^the system returns the current pair without revocation$")]
    public void ThenCurrentPairReturned()
    {
        Assert.Equal(200, _refreshStatus);
        using var document = JsonDocument.Parse(_refreshBody);
        Assert.Equal(_currentAccessToken, document.RootElement.GetProperty("accessToken").GetString());
        Assert.Equal(_currentRefreshToken, document.RootElement.GetProperty("refreshToken").GetString());
    }

    [Then(@"^the refresh is rejected and the current token is revoked as well$")]
    public async Task ThenCurrentRevokedToo()
    {
        Assert.Equal(401, _refreshStatus);
        await PostRefreshAsync(_currentRefreshToken!);
        Assert.Equal(401, _refreshStatus);
        using var document = JsonDocument.Parse(_refreshBody);
        Assert.Equal("refresh_rejected", document.RootElement.GetProperty("title").GetString());
    }

    [Then(@"^the refresh is rejected and no token is returned$")]
    public void ThenUnknownRefreshRejected()
    {
        Assert.Equal(401, _refreshStatus);
        using var document = JsonDocument.Parse(_refreshBody);
        Assert.Equal("refresh_rejected", document.RootElement.GetProperty("title").GetString());
        Assert.False(document.RootElement.TryGetProperty("accessToken", out _));
    }
}
