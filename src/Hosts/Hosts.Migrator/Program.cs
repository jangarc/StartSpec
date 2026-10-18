using Hosts.Base;
using Iam.Infrastructure;
using Iam.Infrastructure.Persistence;
using Iam.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// The ONLY migration runner in the platform (G4 / ADR-003): fixed order, idempotent, explicit.
// Connection string: arg 0, else env ConnectionStrings__Iam. Exit 0 = applied; 2 = missing config.
var connectionString = args.FirstOrDefault() ?? Environment.GetEnvironmentVariable("ConnectionStrings__Iam");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Hosts.Migrator: connection string missing (arg 0 or env ConnectionStrings__Iam).");
    return 2;
}

var options = new DbContextOptionsBuilder<IamDbContext>()
    .UseNpgsql(connectionString)
    .Options;

await using var context = new IamDbContext(options);
await context.Database.MigrateAsync();
Console.WriteLine("Hosts.Migrator: migrations applied.");

// Idempotent bootstrap seeding (design §deployment): initial tenant + operator through the canonical
// application use cases. The operator's plaintext password arrives via IAM_SEED_OPERATOR_PASSWORD;
// without it NO seeding runs (dev must set it; production must inject a secret). Second runs add nothing.
var operatorPassword = Environment.GetEnvironmentVariable("IAM_SEED_OPERATOR_PASSWORD");
if (string.IsNullOrWhiteSpace(operatorPassword))
{
    Console.WriteLine("Hosts.Migrator: seeding skipped (IAM_SEED_OPERATOR_PASSWORD not set).");
    return 0;
}

var services = new ServiceCollection()
    .AddHostBase()
    .AddIam(connectionString, "migrator-deployment-only-signing-key");
await using var provider = services.BuildServiceProvider();
var seeder = provider.GetRequiredService<IamSeeder>();
var result = await seeder.SeedAsync(new IamSeedPlan(
    TenantId: "hq",
    TenantName: "Headquarters",
    InitialOrganizationName: "HQ",
    OperatorLoginName: "admin",
    OperatorDisplayName: "HQ Operator",
    OperatorTypeCode: "id-card",
    OperatorNumber: "hq-operator-0001",
    OperatorPassword: operatorPassword));
Console.WriteLine($"Hosts.Migrator: seed done (tenantCreated={result.TenantCreated}, operatorCreated={result.OperatorCreated}).");
return 0;
