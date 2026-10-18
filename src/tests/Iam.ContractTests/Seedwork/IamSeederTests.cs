using ContractSeedwork.Fakes;
using Iam.Application.Tenants;
using Iam.Application.Users;
using Iam.ContractTests.Infrastructure;
using Iam.Infrastructure;
using Iam.Infrastructure.Persistence;
using Iam.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Ports;
using Xunit;

namespace Iam.ContractTests.Seedwork;

/// <summary>
/// Wired leg for the idempotent seeder over Testcontainers Postgres: migrations applied cleanly,
/// the seed runs TWICE against the same database, and the second run adds nothing. Row counts are
/// probed per plan-keyed slice so the shared container stays conflict-free with the other legs.
/// seeder 整合腿：同庫連跑兩次、第二次零新增；計數以 plan 鍵切片避免與其他腿衝突。
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class IamSeederTests(PostgresFixture fixture)
{
    private static readonly IamSeedPlan Plan = new(
        TenantId: "hq-seed-1",
        TenantName: "HQ Freight",
        InitialOrganizationName: "HQ Ops",
        OperatorLoginName: "root-op",
        OperatorDisplayName: "Platform Operator",
        OperatorTypeCode: "id-card",
        OperatorNumber: "hq-seed-op-0001",
        OperatorPassword: "Seed-Operator-0000");

    private DbContextOptions<IamDbContext> RawOptions => new DbContextOptionsBuilder<IamDbContext>()
        .UseNpgsql(fixture.ConnectionString)
        .Options;

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IClock>(new FakeClock(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)));
        services.AddSingleton<IEventBus, FakeEventBus>();
        services.AddIam(fixture.ConnectionString, "test-only-seed-signing-key");
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Seeder_is_idempotent_second_run_adds_nothing()
    {
        await using (var context = new IamDbContext(RawOptions))
        {
            await context.Database.MigrateAsync();
        }

        using ServiceProvider provider = BuildProvider();

        var first = await SeedAsync(provider);
        var afterFirst = await CountPlanRowsAsync();
        var second = await SeedAsync(provider);
        var afterSecond = await CountPlanRowsAsync();

        Assert.True(first.TenantCreated);
        Assert.True(first.OperatorCreated);
        Assert.False(second.TenantCreated);
        Assert.False(second.OperatorCreated);

        // Plan-keyed row slice after run 1: exactly [1 tenant, 1 organization, 1 identifier tuple, 1 user, 0 families].
        Assert.Equal([1, 1, 1, 1, 0], afterFirst);
        Assert.Equal(afterFirst, afterSecond);

        await using var seederScope = provider.GetRequiredService<IServiceScopeFactory>().CreateAsyncScope();
        var hasher = seederScope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var credentials = seederScope.ServiceProvider.GetRequiredService<IUserCredentialStore>();
        var storedHash = await credentials.FindPasswordHashAsync(Plan.TenantId, Plan.OperatorLoginName);
        Assert.NotNull(storedHash);
        Assert.True(hasher.Verify(Plan.OperatorPassword, storedHash!));
    }

    private static async Task<IamSeedResult> SeedAsync(ServiceProvider provider)
    {
        // Async scope: scoped stores are IAsyncDisposable-only — a sync scope dispose would throw.
        await using var scope = provider.GetRequiredService<IServiceScopeFactory>().CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IamSeeder>();
        return await seeder.SeedAsync(Plan);
    }

    /// <summary>Plan-keyed row slice: [tenants, organizations, identifier tuples, users, refresh families].</summary>
    private async Task<int[]> CountPlanRowsAsync()
    {
        await using var unscoped = new IamDbContext(RawOptions);
        var tenants = await unscoped.Tenants.CountAsync(tenant => tenant.TenantId == Plan.TenantId);
        var identifiers = await unscoped.Identifiers.CountAsync(identifier
            => identifier.TypeCode == Plan.OperatorTypeCode && identifier.Number == Plan.OperatorNumber);
        var families = await unscoped.RefreshFamilies.CountAsync(family => family.TenantId == Plan.TenantId);

        await using var scoped = new IamDbContext(RawOptions, Plan.TenantId);
        var organizations = await scoped.Organizations.CountAsync();
        var users = await scoped.Users.CountAsync();

        return [tenants, organizations, identifiers, users, families];
    }
}
