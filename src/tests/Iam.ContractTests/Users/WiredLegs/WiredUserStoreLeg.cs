using System.Linq.Expressions;
using ContractSeedwork.Guards;
using Iam.Application.Parties;
using Iam.Application.Users;
using Iam.ContractTests.Infrastructure;
using Iam.ContractTests.Users;
using Iam.Domain.Users;
using Iam.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Iam.ContractTests.Users.WiredLegs;

/// <summary>
/// Wired leg: locked scenarios against EF Core / Npgsql over Testcontainers Postgres, migrations
/// applied cleanly via MigrateAsync (no Ensure* shims). GUARDED options carry the tenant-scope query
/// guard (user queries must show a tenant_id predicate); RAW options are invariant probes only
/// (migration bookkeeping, fail-closed filter scans). Extra proofs beyond the contract base: users
/// register as a guarded tenant-scoped seam, the filter is fail-closed, and it is red-on-removal.
/// Wired 腿：auth＋raw 雙面；users 為受守衛之租戶級表；失敗封閉＋移除即紅。
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class WiredUserStoreLeg(PostgresFixture fixture) : UserStoreContractBase
{
    private readonly TenantScopeGuardInterceptor _scopeGuard = new(TenantQueryTables.All.ToHashSet());

    protected override async Task WaitForMigrationsAsync()
    {
        await using var context = new IamDbContext(RawOptions);
        await context.Database.MigrateAsync();

        Assert.NotEmpty(await context.Database.GetAppliedMigrationsAsync());

        var tablesExist = await context.Database
            .SqlQuery<bool>($"SELECT to_regclass('iam.users') IS NOT NULL AS \"Value\"")
            .SingleAsync();
        Assert.True(tablesExist);
    }

    protected override Task<IUserStore> CreateUserStoreAsync(string activeTenantId)
    {
        return Task.FromResult<IUserStore>(new EfUserStore(GuardedOptions, activeTenantId));
    }

    protected override Task<IPartyStore> CreatePartyStoreAsync()
    {
        return Task.FromResult<IPartyStore>(new EfPartyStore(GuardedOptions));
    }

    private DbContextOptions<IamDbContext> GuardedOptions => new DbContextOptionsBuilder<IamDbContext>()
        .UseNpgsql(fixture.ConnectionString)
        .AddInterceptors(_scopeGuard)
        .Options;

    private DbContextOptions<IamDbContext> RawOptions => new DbContextOptionsBuilder<IamDbContext>()
        .UseNpgsql(fixture.ConnectionString)
        .Options;

    [Fact]
    public async Task User_global_filter_is_fail_closed_and_scoped()
    {
        const string tenantId = "nina-g1";
        await WaitForMigrationsAsync();
        await SavedUserAsync(tenantId, "ada-g1", "id-card", "nina-tup-g1");

        await using (var unscoped = new IamDbContext(RawOptions))
        {
            Assert.Empty(await unscoped.Users.ToListAsync());
        }

        await using (var scoped = new IamDbContext(RawOptions, tenantId))
        {
            Assert.Single(await scoped.Users.ToListAsync());
        }

        await using (var other = new IamDbContext(RawOptions, "globex-g"))
        {
            Assert.Empty(await other.Users.ToListAsync());
        }
    }

    [Fact]
    public async Task User_query_guard_is_green_with_the_filter_and_red_when_it_is_removed()
    {
        const string tenantId = "nina-g2";
        await WaitForMigrationsAsync();
        await SavedUserAsync(tenantId, "ada-g2", "id-card", "nina-tup-g2");

        await using (var guarded = new IamDbContext(GuardedOptions, tenantId))
        {
            Assert.Single(await guarded.Users.ToListAsync());
        }

        var redOptions = GuardedOptions;

        await using (var filterless = new FilterlessUserScopeContext(redOptions))
        {
            var caught = await Record.ExceptionAsync(() => filterless.Users.ToListAsync());
            Assert.NotNull(FindViolation(caught));
        }
    }

    private static TenantScopeViolationException? FindViolation(Exception? exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is TenantScopeViolationException violation)
            {
                return violation;
            }
        }

        return null;
    }

    /// <summary>Filter-stripped twin of IamDbContext for the guard's red demonstration (test-only).</summary>
    private sealed class FilterlessUserScopeContext(DbContextOptions<IamDbContext> options) : IamDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>()
                .HasQueryFilter((Expression<Func<User, bool>>?)null);
        }
    }
}
