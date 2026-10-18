using System.Security.Cryptography;
using System.Text;
using SharedKernel.Ports;

namespace Iam.Application.Users;

/// <summary>{tenant, login, password} — handed over by the Host with no further business validation (G3: business rules in Application).</summary>
public sealed record SignInCommand(string TenantId, string LoginName, string Password);

/// <summary>Exposes the signed-in user identity, its bound party, and the issued session pair (G1: contract owned by the use case).</summary>
public sealed record SignInResponse(Guid UserId, Guid PartyId, string AccessToken, string RefreshToken);

/// <summary>
/// Credential sign-in per identity/user-auth spec: wrong password, unknown login, unknown tenant,
/// and a disabled user all raise the SAME generic failure (no enumeration oracle); the response is
/// padded to a shared minimum on EVERY path (timing normalization — an unknown tenant must not fail
/// fast). Verification runs only when a stored hash exists; the pad absorbs the cost gap. Success
/// issues the access–refresh pair and seeds the user's refresh family.
/// 登入驗證：錯密／未知名／未知租戶／停用四種一律同一通用錯誤（防枚舉 oracle）；每條路徑墊至共同最短時長（計時歸一化——未知租戶不得快失敗）；僅存有雜湊時驗證，時間差由墊底吸收；成功即簽發配對並建立該使用者的刷新家族。
/// </summary>
public sealed class SignInUseCase(
    IUserCredentialStore credentialStore,
    IUserStoreScope userStores,
    IPasswordHasher hasher,
    ISignInTimingNormalizer normalizer,
    IClock clock,
    ITokenIssuer issueTokens,
    IRefreshFamilyStore families,
    TimeSpan refreshTtl)
{
    public async Task<SignInResponse> HandleAsync(SignInCommand command, CancellationToken cancellationToken = default)
    {
        var startedAt = clock.UtcNow;

        var passwordHash = await credentialStore.FindPasswordHashAsync(command.TenantId, command.LoginName, cancellationToken);
        if (passwordHash is not null)
        {
            await using var userStore = userStores.Member(command.TenantId);
            var user = await userStore.FindAsync(command.TenantId, command.LoginName, cancellationToken);
            if (user is not null && user.IsActive && hasher.Verify(command.Password, passwordHash))
            {
                var issued = await issueTokens.IssueAsync(new TokenIdentity(user.TenantId, user.UserId), cancellationToken);
                await families.SaveAsync(new StoredRefreshFamily(
                    Guid.NewGuid(),
                    user.TenantId,
                    user.UserId,
                    user.LoginName,
                    Sha256Hex(issued.RefreshToken),
                    issued.RefreshToken,
                    issued.AccessToken,
                    clock.UtcNow + refreshTtl), cancellationToken);
                await normalizer.PadAsync(startedAt, cancellationToken);
                return new SignInResponse(user.UserId, user.PartyId, issued.AccessToken, issued.RefreshToken);
            }
        }

        await normalizer.PadAsync(startedAt, cancellationToken);
        throw new AuthenticationFailedException("Authentication failed.");
    }

    private static string Sha256Hex(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
