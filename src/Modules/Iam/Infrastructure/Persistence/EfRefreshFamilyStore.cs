using Iam.Application.Users;
using Microsoft.EntityFrameworkCore;

namespace Iam.Infrastructure.Persistence;

/// <summary>
/// EF store for refresh families (session material). Families are resolved ONLY by the exact token
/// SHA; a save replaces the whole family row; revocation flags every active family of one user.
/// EF 家族儲存：僅以精確 token SHA 定址；儲存即整列替換；撤銷以 (tenant,user) 全量旗標。
/// </summary>
public sealed class EfRefreshFamilyStore(DbContextOptions<IamDbContext> options) : IRefreshFamilyStore
{
    public async Task<StoredRefreshFamily?> FindByTokenShaAsync(string tokenSha256, CancellationToken cancellationToken = default)
    {
        await using var context = new IamDbContext(options);
        return await context.RefreshFamilies.SingleOrDefaultAsync(family =>
            family.IsActive
            && (family.CurrentRefreshTokenSha == tokenSha256 || family.RotatedFromSha == tokenSha256), cancellationToken);
    }

    public async Task SaveAsync(StoredRefreshFamily family, CancellationToken cancellationToken = default)
    {
        await using var context = new IamDbContext(options);
        var existing = await context.RefreshFamilies
            .SingleOrDefaultAsync(candidate => candidate.FamilyId == family.FamilyId, cancellationToken);
        if (existing is null)
        {
            context.Add(family);
        }
        else
        {
            context.Entry(existing).CurrentValues.SetValues(family);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllAsync(string tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        await using var context = new IamDbContext(options);
        var actives = await context.RefreshFamilies
            .Where(family => family.TenantId == tenantId && family.UserId == userId && family.IsActive)
            .ToListAsync(cancellationToken);
        foreach (var family in actives)
        {
            context.Entry(family).CurrentValues.SetValues(family with { IsActive = false });
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
