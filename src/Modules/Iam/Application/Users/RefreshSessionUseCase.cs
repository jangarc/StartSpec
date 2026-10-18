using System.Security.Cryptography;
using System.Text;
using SharedKernel.Ports;

namespace Iam.Application.Users;

/// <summary>{the presented refresh token} — handed over by the Host with no further business validation (G3: business rules in Application).</summary>
public sealed record RefreshCommand(string RefreshToken);

/// <summary>
/// Refresh rotation per identity/user-auth spec: success rotates to a FRESH pair and invalidates the
/// old token; replay within the reuse window returns the CURRENT pair (no new issuance, no
/// revocation); replay beyond the window revokes the family; expired or unknown tokens are rejected;
/// a disabled user's attempt revokes ALL of their families. Every rejection throws the single
/// refresh-rejected shape — all outcomes fail closed.
/// 刷新輪換（identity/user-auth 規格）：成功＝全新配對＋作廢舊 token；窗內重放＝回現行配對（不重簽、不撤銷）；
/// 窗外重放＝撤銷家族；逾期或未知＝拒絕；停用者＝清掃全家族。所有拒絕走單一型別，一律 fail-closed。
/// </summary>
public sealed class RefreshSessionUseCase(
    IRefreshFamilyStore families,
    IUserStoreScope userStores,
    ITokenIssuer issueTokens,
    IClock clock,
    TimeSpan reuseWindow)
{
    public async Task<IssuedTokens> HandleAsync(RefreshCommand command, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var tokenSha = Sha256Hex(command.RefreshToken);
        var family = await families.FindByTokenShaAsync(tokenSha, cancellationToken);
        if (family is null || !family.IsActive)
        {
            throw new TokenRefreshRejectedException("Refresh token rejected.");
        }

        if (family.RefreshExpiresAt < now)
        {
            await families.RevokeAllAsync(family.TenantId, family.UserId, cancellationToken);
            throw new TokenRefreshRejectedException("Refresh token rejected.");
        }

        if (family.RotatedFromSha == tokenSha && family.RotatedAt is { } rotatedAt)
        {
            if (rotatedAt + reuseWindow < now)
            {
                await families.RevokeAllAsync(family.TenantId, family.UserId, cancellationToken);
                throw new TokenRefreshRejectedException("Refresh token rejected.");
            }

            await families.SaveAsync(family with { }, cancellationToken);
            return new IssuedTokens(family.CurrentAccessToken, family.CurrentRefreshToken);
        }

        await using var userStore = userStores.Member(family.TenantId);
        var user = await userStore.FindAsync(family.TenantId, family.LoginName, cancellationToken);
        if (user is null || !user.IsActive)
        {
            await families.RevokeAllAsync(family.TenantId, family.UserId, cancellationToken);
            throw new TokenRefreshRejectedException("Refresh token rejected.");
        }

        var newPair = await issueTokens.IssueAsync(new TokenIdentity(family.TenantId, family.UserId), cancellationToken);
        await families.SaveAsync(family with
        {
            RotatedFromSha = tokenSha,
            RotatedAt = now,
            CurrentRefreshTokenSha = Sha256Hex(newPair.RefreshToken),
            CurrentRefreshToken = newPair.RefreshToken,
            CurrentAccessToken = newPair.AccessToken,
        }, cancellationToken);
        return newPair;
    }

    private static string Sha256Hex(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
