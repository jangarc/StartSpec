using System.Net.Http.Json;
using System.Text.Json;
using Iam.Application.Tenants;
using Iam.Application.Users;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

namespace Iam.Reqnroll;

/// <summary>
/// Authenticated-caller helper for the API-contract step bindings (design decision 2 gate fix): the
/// tenant scope resolves server-side from validated claims, so scoped HTTP steps ride a Bearer
/// session instead of the former tenant header. The bootstrap identity is provisioned once per
/// scenario through the canonical application use cases (the deployment path — tenant + operator +
/// password hash), then signs in over the real HTTP surface; every scoped tenant gets its own
/// auto-provisioned operator the same way. State caches under scenario-scoped keys.
/// 場景呼叫者輔助：範圍一律伺服器端自 claims 解析；每租戶操作員經部署路徑建立，登入行走真實 HTTP 面。
/// </summary>
internal static class CallerAuth
{
    public const string ScenarioPassword = "Correct-Operator-0000";

    private const string BootstrapLogin = "poi";
    private const string SuffixKey = "caller-suffix";

    public static async Task<string> ReadyBootstrapCallerAsync(ScenarioContext scenarioContext)
    {
        if (scenarioContext.TryGetValue("caller-bearer:bootstrap", out string? cached) && cached is not null)
        {
            return cached;
        }

        var bootstrapTenant = $"poi-{SuffixOf(scenarioContext)}";
        var services = scenarioContext.Get<IServiceScope>().ServiceProvider;
        await services.GetRequiredService<ProvisionTenantUseCase>().HandleAsync(
            new ProvisionTenantCommand(bootstrapTenant, "Scenario Bootstrap", "Bootstrap Org"));
        await ProvisionOperatorAsync(scenarioContext, bootstrapTenant, BootstrapLogin);

        var bearer = await SignInAsync(scenarioContext, bootstrapTenant, BootstrapLogin, ScenarioPassword);
        scenarioContext["caller-bearer:bootstrap"] = bearer;
        return bearer;
    }

    public static async Task<string> BearerForAsync(ScenarioContext scenarioContext, string scopedTenant)
    {
        if (scenarioContext.TryGetValue($"caller-bearer:{scopedTenant}", out string? cached) && cached is not null)
        {
            return cached;
        }

        await ProvisionOperatorAsync(scenarioContext, scopedTenant, $"op-{scopedTenant}");
        var bearer = await SignInAsync(scenarioContext, scopedTenant, $"op-{scopedTenant}", ScenarioPassword);
        scenarioContext[$"caller-bearer:{scopedTenant}"] = bearer;
        return bearer;
    }

    private static async Task ProvisionOperatorAsync(ScenarioContext scenarioContext, string scopedTenant, string loginName)
    {
        var services = scenarioContext.Get<IServiceScope>().ServiceProvider;
        await services.GetRequiredService<ProvisionUserUseCase>().HandleAsync(new ProvisionUserCommand(
            scopedTenant, loginName, $"{loginName} Operator", null, "id-card", $"{scopedTenant}-{loginName}"));
        var hasher = services.GetRequiredService<IPasswordHasher>();
        var credentials = services.GetRequiredService<IUserCredentialStore>();
        await credentials.SavePasswordHashAsync(scopedTenant, loginName, hasher.Hash(ScenarioPassword));
    }

    private static async Task<string> SignInAsync(ScenarioContext scenarioContext, string scopedTenant, string loginName, string password)
    {
        var client = scenarioContext.Get<HttpClient>();
        using var response = await client.PostAsJsonAsync("/iam/auth/sign-in", new { tenantId = scopedTenant, loginName, password });
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        return $"Bearer {document.RootElement.GetProperty("accessToken").GetString()}";
    }

    private static string SuffixOf(ScenarioContext scenarioContext)
    {
        if (!scenarioContext.TryGetValue(SuffixKey, out string? suffix) || suffix is null)
        {
            suffix = Harness.NextSuffix();
            scenarioContext[SuffixKey] = suffix;
        }

        return suffix;
    }
}
