using System.Net.Http.Json;
using System.Text.Json;
using Reqnroll;
using Xunit;

namespace Iam.Reqnroll;

/// <summary>
/// Step bindings for the tenant API contract scenarios. The caller goes through the real HTTP surface
/// with the interim HostBase scope transport (X-Tenant-Id header); assertions inspect raw JSON bodies
/// so wire-format guarantees (uniform 404 bodies, no totals) are checked byte-level, not re-modeled.
/// </summary>
[Binding]
public sealed class TenantFeatureSteps(ScenarioContext scenarioContext)
{
    private const string TenantHeaderName = "X-Tenant-Id";

    private readonly HttpClient _client = scenarioContext.Get<HttpClient>();

    private string? _suffix;
    private readonly Dictionary<string, (Guid OrganizationId, string OrganizationName)> _organizationByTenant = new();
    private string _firstDeniedBody = "";
    private string _secondDeniedBody = "";

    private string Suffix() => _suffix ??= Harness.NextSuffix();

    private string ScopedTenantId(string tenantName) => $"{tenantName}-{Suffix()}";

    [Given(@"^I provision a tenant ""([^""]*)"" named ""([^""]*)"" with the initial organization ""([^""]*)""$")]
    [When(@"^I provision a tenant ""([^""]*)"" named ""([^""]*)"" with the initial organization ""([^""]*)""$")]
    public async Task WhenIProvisionATenant(string tenantName, string tenantDisplayName, string organizationName)
    {
        var tenantId = ScopedTenantId(tenantName);
        using var response = await _client.PostAsJsonAsync(
            "/iam/tenants",
            new { tenantId, tenantName = tenantDisplayName, initialOrganizationName = organizationName });
        var body = await response.Content.ReadAsStringAsync();
        scenarioContext["provisionStatus"] = (int)response.StatusCode;
        scenarioContext["provisionBody"] = body;

        if ((int)response.StatusCode == 201)
        {
            using var document = JsonDocument.Parse(body);
            var organizationId = document.RootElement.GetProperty("organizationId").GetGuid();
            _organizationByTenant[tenantName] = (organizationId, organizationName);
        }
    }

    [When(@"^the caller scoped to tenant ""([^""]*)"" lists organizations$")]
    public async Task WhenTheCallerScopedToTenantListsOrganizations(string tenantName)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/iam/organizations?limit=50");
        request.Headers.Add(TenantHeaderName, ScopedTenantId(tenantName));
        using var response = await _client.SendAsync(request);
        scenarioContext["listStatus"] = (int)response.StatusCode;
        scenarioContext["listBody"] = await response.Content.ReadAsStringAsync();
    }

    [When(@"^the caller without a tenant scope requests organization listings$")]
    public async Task WhenTheCallerWithoutATenantScopeRequestsOrganizationListings()
    {
        using var response = await _client.GetAsync("/iam/organizations?limit=50");
        scenarioContext["listStatus"] = (int)response.StatusCode;
        scenarioContext["listBody"] = await response.Content.ReadAsStringAsync();
    }

    [When(@"^the caller scoped to tenant ""([^""]*)"" reads the last provisioned organization of tenant ""([^""]*)""$")]
    public async Task WhenTheCallerScopedToTenantReadsTheLastProvisionedOrganizationOfTenant(string scopedTenantName, string ownerTenantName)
    {
        _firstDeniedBody = await ReadOrganizationAsync(scopedTenantName, _organizationByTenant[ownerTenantName].OrganizationId);
    }

    [When(@"^the caller scoped to tenant ""([^""]*)"" also requests a truly unknown organization$")]
    public async Task WhenTheCallerScopedToTenantAlsoRequestsATrulyUnknownOrganization(string scopedTenantName)
    {
        _secondDeniedBody = await ReadOrganizationAsync(scopedTenantName, Guid.NewGuid());
    }

    private async Task<string> ReadOrganizationAsync(string scopedTenantName, Guid organizationId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/iam/organizations/{organizationId}");
        request.Headers.Add(TenantHeaderName, ScopedTenantId(scopedTenantName));
        using var response = await _client.SendAsync(request);
        scenarioContext["readStatus"] = (int)response.StatusCode;
        return await response.Content.ReadAsStringAsync();
    }

    [Then(@"^the system creates the tenant ""([^""]*)"" with its initial organization and returns the tenant identifier$")]
    public void ThenTheSystemCreatesTheTenantWithItsInitialOrganizationAndReturnsTheTenantIdentifier(string tenantName)
    {
        Assert.Equal(201, scenarioContext.Get<int>("provisionStatus"));
        using var document = JsonDocument.Parse(scenarioContext.Get<string>("provisionBody"));
        Assert.Equal(ScopedTenantId(tenantName), document.RootElement.GetProperty("tenantId").GetString());
        Assert.True(document.RootElement.TryGetProperty("organizationId", out _));
    }

    [Then(@"^the system rejects the request with a conflict error$")]
    public void ThenTheSystemRejectsTheRequestWithAConflictError()
    {
        Assert.Equal(409, scenarioContext.Get<int>("provisionStatus"));
        using var document = JsonDocument.Parse(scenarioContext.Get<string>("provisionBody"));
        Assert.Equal("tenant_conflict", document.RootElement.GetProperty("title").GetString());
    }

    [Then(@"^the tenant ""([^""]*)"" keeps its single original organization$")]
    public async Task ThenTheTenantKeepsItsSingleOriginalOrganization(string tenantName)
    {
        await WhenTheCallerScopedToTenantListsOrganizations(tenantName);
        Assert.Equal(200, scenarioContext.Get<int>("listStatus"));

        using var document = JsonDocument.Parse(scenarioContext.Get<string>("listBody"));
        var items = document.RootElement.GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal(_organizationByTenant[tenantName].OrganizationId, items[0].GetProperty("organizationId").GetGuid());
        Assert.Equal(_organizationByTenant[tenantName].OrganizationName, items[0].GetProperty("name").GetString());
    }

    [Then(@"^the system resolves tenant ""([^""]*)"" as the active tenant for the operation$")]
    public void ThenTheSystemResolvesTenantAsTheActiveTenantForTheOperation(string tenantName)
    {
        Assert.Equal(200, scenarioContext.Get<int>("listStatus"));
        using var document = JsonDocument.Parse(scenarioContext.Get<string>("listBody"));
        var items = document.RootElement.GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal(_organizationByTenant[tenantName].OrganizationId, items[0].GetProperty("organizationId").GetGuid());
    }

    [Then(@"^the system rejects the request without executing business logic$")]
    public void ThenTheSystemRejectsTheRequestWithoutExecutingBusinessLogic()
    {
        Assert.Equal(401, scenarioContext.Get<int>("listStatus"));
        using var document = JsonDocument.Parse(scenarioContext.Get<string>("listBody"));
        Assert.Equal("tenant_scope_missing", document.RootElement.GetProperty("title").GetString());
    }

    [Then(@"^the system denies access as if the record does not exist$")]
    public void ThenTheSystemDeniesAccessAsIfTheRecordDoesNotExist()
    {
        Assert.Equal(404, scenarioContext.Get<int>("readStatus"));
        using var document = JsonDocument.Parse(_firstDeniedBody);
        Assert.Equal("not_found", document.RootElement.GetProperty("title").GetString());
    }

    [Then(@"^the system responds exactly as if the record does not exist$")]
    public void ThenTheSystemRespondsExactlyAsIfTheRecordDoesNotExist()
    {
        Assert.Equal(404, scenarioContext.Get<int>("readStatus"));
        Assert.Equal(_firstDeniedBody, _secondDeniedBody);
    }

    [Then(@"^the system returns only records owned by tenant ""([^""]*)""$")]
    public void ThenTheSystemReturnsOnlyRecordsOwnedByTenant(string tenantName)
    {
        Assert.Equal(200, scenarioContext.Get<int>("listStatus"));
        using var document = JsonDocument.Parse(scenarioContext.Get<string>("listBody"));
        var items = document.RootElement.GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal(_organizationByTenant[tenantName].OrganizationId, items[0].GetProperty("organizationId").GetGuid());
    }

    [Then(@"^the system returns a has-next indicator without exact totals$")]
    public void ThenTheSystemReturnsAHasNextIndicatorWithoutExactTotals()
    {
        Assert.Equal(200, scenarioContext.Get<int>("listStatus"));
        using var document = JsonDocument.Parse(scenarioContext.Get<string>("listBody"));
        var root = document.RootElement;
        Assert.True(root.TryGetProperty("hasNext", out var hasNext));
        Assert.True(hasNext.ValueKind is JsonValueKind.True or JsonValueKind.False);
        Assert.False(root.TryGetProperty("total", out _));
        Assert.False(root.TryGetProperty("count", out _));
        Assert.False(root.TryGetProperty("totalCount", out _));
    }
}
