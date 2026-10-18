using ContractSeedwork.Guards;
using Iam.Application.Users;
using Iam.ContractTests.Auth;
using Iam.ContractTests.Infrastructure;
using Iam.Domain.Party;
using Iam.Domain.Users;
using Iam.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Iam.ContractTests.Auth.WiredLegs;

/// <summary>
/// Wired leg of the credential store port contract: EF Core / Npgsql over Testcontainers Postgres,
/// migrations applied cleanly via MigrateAsync; GUARDED options carry the tenant-scope query guard
/// (every users query must show a tenant_id predicate). Facts reuse the SAME constants-free addresses
/// (nina-c*) so one container serves the whole collection without cross-leg interference.
/// 真實腿：Testcontainers Postgres＋MigrateAsync；受守衛 options 斷言每條 users 查詢含 tenant_id 述詞；常數定址 nina-c* 避免跨腿干擾。
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class WiredUserCredentialStoreLeg(PostgresFixture fixture) : UserCredentialStoreContractBase
{
    private readonly TenantScopeGuardInterceptor _scopeGuard = new(TenantQueryTables.All.ToHashSet());
    private static readonly DateTimeOffset SeededAt = new(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

    protected override async Task WaitForSeamAsync()
    {
        await using var context = new IamDbContext(RawOptions);
        await context.Database.MigrateAsync();

        Assert.NotEmpty(await context.Database.GetAppliedMigrationsAsync());

        var tablesExist = await context.Database
            .SqlQuery<bool>($"SELECT to_regclass('iam.users') IS NOT NULL AS \"Value\"")
            .SingleAsync();
        Assert.True(tablesExist);
    }

    protected override Task<IUserCredentialStore> CreateCredentialStoreAsync()
    {
        return Task.FromResult<IUserCredentialStore>(new EfUserCredentialStore(GuardedOptions));
    }

    protected override async Task SeedUserAsync(string tenantId, string loginName)
    {
        var partyStore = new EfPartyStore(GuardedOptions);
        var party = Party.Provision($"Party {tenantId}", null, [PartyIdentifier.Create("id-card", $"tup-{tenantId}")], SeededAt);
        await partyStore.SaveAsync(party);

        var user = User.Provision(tenantId, loginName, party.PartyId, SeededAt);
        await new EfUserStore(GuardedOptions, tenantId).SaveAsync(user);
    }

    private DbContextOptions<IamDbContext> GuardedOptions => new DbContextOptionsBuilder<IamDbContext>()
        .UseNpgsql(fixture.ConnectionString)
        .AddInterceptors(_scopeGuard)
        .Options;

    private DbContextOptions<IamDbContext> RawOptions => new DbContextOptionsBuilder<IamDbContext>()
        .UseNpgsql(fixture.ConnectionString)
        .Options;
}
