using ContractSeedwork.Guards;
using Iam.Application.Tenants;
using Iam.ContractTests.Infrastructure;
using Iam.ContractTests.Tenants;
using Iam.Domain.Tenants;
using Iam.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Iam.ContractTests.Tenants.WiredLegs;

/// <summary>
/// Wired leg: locked scenarios against EF Core 10 / Npgsql over Testcontainers Postgres, migrations
/// applied cleanly via MigrateAsync (no Ensure* shims). Two context faces: GUARDED options carry the
/// tenant-scope query guard (member queries must show a tenant_id predicate); RAW options are invariant
/// probes only (migration bookkeeping, fail-closed filter scans). Extra proofs beyond the contract
/// base: migration clean-apply, fail-closed filter, guard red-on-removal.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class WiredTenantStoreLeg(PostgresFixture fixture) : TenantStoreContractBase
{
    private readonly TenantScopeGuardInterceptor _scopeGuard = new(TenantQueryTables.All.ToHashSet());

    protected override async Task WaitForMigrationsAsync()
    {
        await using var context = new IamDbContext(RawOptions);
        await context.Database.MigrateAsync();

        Assert.NotEmpty(await context.Database.GetAppliedMigrationsAsync());

        var tablesExist = await context.Database
            .SqlQuery<bool>($"SELECT (to_regclass('iam.tenants') IS NOT NULL AND to_regclass('iam.organizations') IS NOT NULL) AS \"Value\"")
            .SingleAsync();
        Assert.True(tablesExist);
    }

    protected override Task<ITenantStore> CreateScopeStoreAsync(string? activeTenantId)
    {
        return Task.FromResult<ITenantStore>(new EfTenantStore(GuardedOptions, activeTenantId));
    }

    private DbContextOptions<IamDbContext> GuardedOptions => new DbContextOptionsBuilder<IamDbContext>()
        .UseNpgsql(fixture.ConnectionString)
        .AddInterceptors(_scopeGuard)
        .Options;

    private DbContextOptions<IamDbContext> RawOptions => new DbContextOptionsBuilder<IamDbContext>()
        .UseNpgsql(fixture.ConnectionString)
        .Options;

    [Fact]
    public async Task Organization_global_filter_is_fail_closed_and_scoped()
    {
        var tenant = Tenant.Provision("acme-filter", "Acme Health", "Acme HQ", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        await using var platform = new EfTenantStore(RawOptions, null);
        await platform.SaveAsync(tenant);

        await using (var unscoped = new IamDbContext(RawOptions))
        {
            Assert.Empty(await unscoped.Organizations.ToListAsync());
        }

        await using (var scoped = new IamDbContext(RawOptions, "acme-filter"))
        {
            Assert.Single(await scoped.Organizations.ToListAsync());
        }

        await using (var other = new IamDbContext(RawOptions, "globex-filter"))
        {
            Assert.Empty(await other.Organizations.ToListAsync());
        }
    }

    [Fact]
    public async Task Query_guard_is_green_with_the_filter_and_red_when_it_is_removed()
    {
        await WaitForMigrationsAsync();

        await using var platform = new EfTenantStore(RawOptions, null);
        var tenant = Tenant.Provision("acme-guard", "Acme Guard", "Acme HQ", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        await platform.SaveAsync(tenant);

        await using (var guarded = new IamDbContext(GuardedOptions, "acme-guard"))
        {
            // Green: the construction-level filter itself predicates the scan.
            Assert.Single(await guarded.Organizations.ToListAsync());
        }

        var redOptions = new DbContextOptionsBuilder<IamDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(_scopeGuard)
            .Options;

        await using (var filterless = new FilterlessTenantScopeContext(redOptions))
        {
            var caught = await Record.ExceptionAsync(() => filterless.Organizations.ToListAsync());
            var violation = FindViolation(caught);
            Assert.NotNull(violation);
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
    private sealed class FilterlessTenantScopeContext(DbContextOptions<IamDbContext> options) : IamDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Organization>()
                .HasQueryFilter((System.Linq.Expressions.Expression<Func<Organization, bool>>?)null);
        }
    }
}
