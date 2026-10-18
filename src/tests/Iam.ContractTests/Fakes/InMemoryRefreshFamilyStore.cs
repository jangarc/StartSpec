using Iam.Application.Users;

namespace Iam.ContractTests.Fakes;

/// <summary>
/// In-memory fake for the <see cref="IRefreshFamilyStore"/> session seam port: families are keyed by
/// the current AND rotated-from token SHA (both lookup paths observable) and revocation sweeps by
/// (tenant, user) are counted, so the disabled-user revocation obligation is assertable.
/// 家族儲存 fake：以現行與旋轉前 token 雙路徑可尋址；撤銷清掃與次數皆可斷言。
/// </summary>
public sealed class InMemoryRefreshFamilyStore : IRefreshFamilyStore
{
    private readonly List<StoredRefreshFamily> _families = [];

    public IReadOnlyList<StoredRefreshFamily> Families => _families;

    public int RevokedAllCount { get; private set; }

    public Task<StoredRefreshFamily?> FindByTokenShaAsync(string tokenSha256, CancellationToken cancellationToken = default)
    {
        var family = _families.FirstOrDefault(candidate =>
            candidate.IsActive
            && (candidate.CurrentRefreshTokenSha == tokenSha256 || candidate.RotatedFromSha == tokenSha256));
        return Task.FromResult(family);
    }

    public Task SaveAsync(StoredRefreshFamily family, CancellationToken cancellationToken = default)
    {
        _families.RemoveAll(existing => existing.FamilyId == family.FamilyId);
        _families.Add(family);
        return Task.CompletedTask;
    }

    public Task RevokeAllAsync(string tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        RevokedAllCount++;
        for (var index = 0; index < _families.Count; index++)
        {
            var family = _families[index];
            if (family.TenantId == tenantId && family.UserId == userId && family.IsActive)
            {
                _families[index] = family with { IsActive = false };
            }
        }

        return Task.CompletedTask;
    }
}
