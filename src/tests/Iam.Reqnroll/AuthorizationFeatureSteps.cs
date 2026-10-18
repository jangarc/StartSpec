using System.Net.Http.Json;
using System.Text.Json;
using Reqnroll;
using Xunit;

namespace Iam.Reqnroll;

/// <summary>
/// Step bindings for the Authorization API contract scenarios (task 5.2). All assertions ride the
/// real HTTP surface of Hosts.HQ over wired Postgres; sentence patterns are unique to this class so
/// bindings stay unambiguous across the assembly.
/// 授權場景步驟：全走真實 HTTP 面；句式為本類別專屬，避免跨類別綁定歧義。
/// </summary>
[Binding]
public sealed class AuthorizationFeatureSteps(ScenarioContext scenarioContext)
{
    private readonly HttpClient _client = scenarioContext.Get<HttpClient>();

    private string? _suffix;
    private const string TenantHeaderName = "X-Tenant-Id";
    private string _currentTenantKey = "";
    private readonly Dictionary<string, Guid> _userIdByTenant = new();
    private readonly Dictionary<string, Guid> _roleIdByTenant = new();

    private Guid _assignmentId;
    private int _assignmentStatus;
    private string _assignmentBody = "";
    private int _revokeStatus;
    private int _evaluateStatus;
    private string _evaluateBody = "";
    private bool _lastAllowed;

    private string Suffix() => _suffix ??= Harness.NextSuffix();

    private string ScopedTenantId(string tenantKey) => $"{tenantKey}-{Suffix()}";

    [Given(@"^the tenant ""([^""]*)"" is readied named ""([^""]*)"" with the initial organization ""([^""]*)""$")]
    public async Task GivenReadiedTenant(string tenantKey, string tenantDisplayName, string organizationName)
    {
        _currentTenantKey = tenantKey;
        using var response = await _client.PostAsJsonAsync(
            "/iam/tenants",
            new { tenantId = ScopedTenantId(tenantKey), tenantName = tenantDisplayName, initialOrganizationName = organizationName });
        response.EnsureSuccessStatusCode();
    }

    [Given(@"^a user ""([^""]*)"" is provisioned in tenant ""([^""]*)""$")]
    public async Task GivenUserProvisioned(string loginName, string tenantKey)
    {
        _currentTenantKey = tenantKey;
        using var request = new HttpRequestMessage(HttpMethod.Post, "/iam/users");
        request.Headers.Add(TenantHeaderName, ScopedTenantId(tenantKey));
        request.Content = JsonContent.Create(new
        {
            loginName,
            displayName = $"{loginName} Operator",
            dateOfBirth = (DateOnly?)new DateOnly(1990, 1, 1),
            typeCode = "id-card",
            number = $"{loginName}-authz-{Suffix()}",
        });
        using var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        _userIdByTenant[tenantKey] = document.RootElement.GetProperty("userId").GetGuid();
    }

    [Given(@"^the role ""([^""]*)"" granting ""([^""]*)"" is provisioned in tenant ""([^""]*)""$")]
    public async Task GivenRoleProvisioned(string roleName, string permission, string tenantKey)
    {
        _currentTenantKey = tenantKey;
        using var request = new HttpRequestMessage(HttpMethod.Post, "/iam/roles");
        request.Headers.Add(TenantHeaderName, ScopedTenantId(tenantKey));
        request.Content = JsonContent.Create(new { name = roleName, permissions = new[] { permission } });
        using var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        _roleIdByTenant[tenantKey] = document.RootElement.GetProperty("roleId").GetGuid();
    }

    [When(@"^the administrator assigns that role to that user in the tenant$")]
    public async Task WhenAssignThatRoleToThatUser()
    {
        var tenantKey = _currentTenantKey;
        await AssignAsync(tenantKey, _roleIdByTenant[tenantKey], _userIdByTenant[tenantKey]);
    }

    [Given(@"^the role is assigned to the user$")]
    public async Task GivenRoleAssignedToUser()
    {
        var tenantKey = _currentTenantKey;
        await AssignAsync(tenantKey, _roleIdByTenant[tenantKey], _userIdByTenant[tenantKey]);
    }

    [When(@"^the administrator assigns the ""([^""]*)"" role to the ""([^""]*)"" user in tenant ""([^""]*)""$")]
    public async Task WhenAssignExplicit(string roleTenantKey, string userTenantKey, string scopeTenantKey)
    {
        await AssignAsync(scopeTenantKey, _roleIdByTenant[roleTenantKey], _userIdByTenant[userTenantKey]);
    }

    private async Task AssignAsync(string scopeTenantKey, Guid roleId, Guid userId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/iam/roles/assignments");
        request.Headers.Add(TenantHeaderName, ScopedTenantId(scopeTenantKey));
        request.Content = JsonContent.Create(new { userId, roleId });
        using var response = await _client.SendAsync(request);
        _assignmentStatus = (int)response.StatusCode;
        _assignmentBody = await response.Content.ReadAsStringAsync();
        if (_assignmentStatus == 201)
        {
            using var document = JsonDocument.Parse(_assignmentBody);
            _assignmentId = document.RootElement.GetProperty("assignmentId").GetGuid();
        }
    }

    [When(@"^the administrator revokes the assignment and propagation completes$")]
    public async Task WhenAdminRevokesAssignment()
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/iam/roles/assignments/{_assignmentId}");
        request.Headers.Add(TenantHeaderName, ScopedTenantId(_currentTenantKey));
        using var response = await _client.SendAsync(request);
        _revokeStatus = (int)response.StatusCode;
        response.EnsureSuccessStatusCode();
    }

    private async Task EvaluateAsync(string tenantKey, Guid userId, string permission)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/iam/authorization/evaluations");
        request.Headers.Add(TenantHeaderName, ScopedTenantId(tenantKey));
        request.Content = JsonContent.Create(new { userId, permission });
        using var response = await _client.SendAsync(request);
        _evaluateStatus = (int)response.StatusCode;
        _evaluateBody = await response.Content.ReadAsStringAsync();
        if (_evaluateStatus == 200)
        {
            using var document = JsonDocument.Parse(_evaluateBody);
            _lastAllowed = document.RootElement.GetProperty("allowed").GetBoolean();
        }
    }

    [When(@"^the system evaluates ""([^""]*)"" for that user in tenant ""([^""]*)""$")]
    public async Task WhenEvaluateForCurrentUser(string permission, string tenantKey)
    {
        await EvaluateAsync(tenantKey, _userIdByTenant[_currentTenantKey], permission);
    }

    [When(@"^the system evaluates ""([^""]*)"" for the ""([^""]*)"" user in tenant ""([^""]*)""$")]
    public async Task WhenEvaluateForExplicitUser(string permission, string userTenantKey, string tenantKey)
    {
        await EvaluateAsync(tenantKey, _userIdByTenant[userTenantKey], permission);
    }

    [Then(@"^the system records the assignment$")]
    public void ThenRecordsAssignment()
    {
        Assert.Equal(201, _assignmentStatus);
        Assert.NotEqual(default, _assignmentId);
    }

    [Then(@"^the system rejects the assignment and records nothing$")]
    public void ThenAssignmentRejected()
    {
        Assert.Equal(400, _assignmentStatus);
        using var document = JsonDocument.Parse(_assignmentBody);
        Assert.Equal("invalid_assignment_request", document.RootElement.GetProperty("title").GetString());
        Assert.Equal(default, _assignmentId);
    }

    [Then(@"^the decision is allow$")]
    public void ThenDecisionIsAllow()
    {
        Assert.Equal(200, _evaluateStatus);
        Assert.True(_lastAllowed);
    }

    [Then(@"^the decision is deny$")]
    public void ThenDecisionIsDeny()
    {
        Assert.Equal(200, _evaluateStatus);
        Assert.False(_lastAllowed);
    }

    [Then(@"^the decision for ""([^""]*)"" is deny$")]
    public async Task ThenDecisionForPermissionIsDeny(string permission)
    {
        var tenantKey = _currentTenantKey;
        await EvaluateAsync(tenantKey, _userIdByTenant[tenantKey], permission);
        ThenDecisionIsDeny();
    }
}
