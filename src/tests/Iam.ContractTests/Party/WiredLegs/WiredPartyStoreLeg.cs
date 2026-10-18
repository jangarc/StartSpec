using Iam.Application.Parties;
using Iam.ContractTests.Infrastructure;
using Iam.ContractTests.Parties;
using Iam.Domain.Party;
using Iam.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Iam.ContractTests.Parties.WiredLegs;

/// <summary>
/// Wired leg: locked scenarios against EF Core / Npgsql over Testcontainers Postgres, migrations
/// applied cleanly via MigrateAsync. Party tables ride OUTSIDE the tenant-scope guard (platform store,
/// no tenant_id, never filtered) — the guard framework simply has nothing to assert on these tables.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class WiredPartyStoreLeg(PostgresFixture fixture) : PartyStoreContractBase
{
    protected override async Task WaitForMigrationsAsync()
    {
        await using var context = new IamDbContext(new DbContextOptionsBuilder<IamDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options);
        await context.Database.MigrateAsync();

        Assert.NotEmpty(await context.Database.GetAppliedMigrationsAsync());

        var tablesExist = await context.Database
            .SqlQuery<bool>($"SELECT (to_regclass('iam.parties') IS NOT NULL AND to_regclass('iam.party_identifier') IS NOT NULL AND to_regclass('iam.party_role') IS NOT NULL AND to_regclass('iam.party_consent') IS NOT NULL) AS \"Value\"")
            .SingleAsync();
        Assert.True(tablesExist);
    }

    protected override Task<IPartyStore> CreateStoreAsync()
    {
        return Task.FromResult<IPartyStore>(new EfPartyStore(new DbContextOptionsBuilder<IamDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options));
    }
}
