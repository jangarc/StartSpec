using Iam.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using Testcontainers.PostgreSql;

namespace Iam.Reqnroll;

/// <summary>
/// Test-run-wide state: one Postgres container per run, migrations applied exactly once via
/// MigrateAsync; one in-memory host per scenario serving the real HTTP surface of Hosts.HQ, bound
/// to the container connection string. Tenant identifiers are suffixed with a run-wide scenario
/// counter so the shared wired database and scenario reruns stay conflict-free.
/// 全測試程式共用 Postgres 容器；每場景一個 WebApplicationFactory；租戶識別加場景計數後綴避免衝突。
/// </summary>
public static class Harness
{
    public static PostgreSqlContainer? Container { get; private set; }

    public static string ConnectionString { get; private set; } = "";

    private static int ScenarioSequence;

    /// <summary>Unique within a test run; step bindings cache it once per scenario.</summary>
    public static string NextSuffix() => Interlocked.Increment(ref ScenarioSequence).ToString();

    public static void Start(PostgreSqlContainer container) => Container = container;

    public static void Bind(string connectionString) => ConnectionString = connectionString;
}

[Binding]
public sealed class HarnessHooks
{
    [BeforeTestRun]
    public static async Task StartPostgresAsync()
    {
        // CS0618 workaround: parameterless PostgreSqlBuilder() is obsolete.
        Harness.Start(new PostgreSqlBuilder("postgres:17-alpine").Build());
        await Harness.Container!.StartAsync();
        Harness.Bind(Harness.Container.GetConnectionString());

        var options = new DbContextOptionsBuilder<IamDbContext>()
            .UseNpgsql(Harness.ConnectionString)
            .Options;
        await using var context = new IamDbContext(options);
        await context.Database.MigrateAsync();
    }

    [AfterTestRun]
    public static async Task StopPostgresAsync()
    {
        if (Harness.Container is { } container)
        {
            await container.DisposeAsync();
        }
    }
}

[Binding]
public sealed class HostScenarioHooks(ScenarioContext scenarioContext)
{
    [BeforeScenario]
    public void StartHost()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("ConnectionStrings:Iam", Harness.ConnectionString));
        scenarioContext.Set(factory);
        scenarioContext.Set(factory.CreateClient());
        scenarioContext.Set(factory.Services.CreateScope());
    }

    [AfterScenario]
    public async Task StopHost()
    {
        await ((IAsyncDisposable)scenarioContext.Get<IServiceScope>().ServiceProvider).DisposeAsync();
        scenarioContext.Get<WebApplicationFactory<Program>>().Dispose();
    }
}
