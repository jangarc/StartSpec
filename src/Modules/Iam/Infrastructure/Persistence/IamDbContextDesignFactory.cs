using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Iam.Infrastructure.Persistence;

/// <summary>Design-time context factory for dotnet-ef tooling (no runtime behavior).</summary>
public sealed class IamDbContextDesignFactory : IDesignTimeDbContextFactory<IamDbContext>
{
    public IamDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IamDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=startspec_design;Username=postgres;Password=postgres")
            .Options;

        return new IamDbContext(options);
    }
}
