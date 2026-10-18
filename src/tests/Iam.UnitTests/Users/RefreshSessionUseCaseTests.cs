using System.Security.Cryptography;
using System.Text;
using ContractSeedwork.Fakes;
using Iam.Application.Users;
using Iam.ContractTests.Fakes;
using Iam.Domain.Users;
using Xunit;

namespace Iam.UnitTests.Users;

/// <summary>
/// RefreshSessionUseCase TDD (task 4.3): an unexpired current token rotates to a fresh pair and
/// invalidates the old token; replay within the reuse window hands back the CURRENT pair with no new
/// issuance and no revocation; replay beyond the window revokes the family; expired tokens are
/// rejected; two families (two sign-ins / devices) rotate independently; a disabled user's refresh
/// is rejected and sweeps ALL of their families. Sign-in issues the initial pair and seeds the family.
/// 刷新用例 TDD：現行未逾期 token 輪換至全新配對並作廢舊 token；窗內重放回現行配對（不重簽、不撤銷）；
/// 窗外重放撤銷家族；逾期拒絕；兩家族各自獨立輪換；停用者刷新拒絕並清掃其全家族；登入簽出首組配對並建家族。
/// </summary>
public sealed class RefreshSessionUseCaseTests
{
    private const string CorrectPassword = "correct-horse";
    private static readonly DateTimeOffset FixedNow = new(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan ReuseWindow = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan RefreshTtl = TimeSpan.FromDays(14);

    private sealed record Fixture(
        SignInUseCase SignIn,
        RefreshSessionUseCase Refresh,
        InMemoryRefreshFamilyStore Families,
        InMemoryTokenIssuer Issuer,
        InMemoryUserCredentialStore Credentials,
        InMemoryUserStore Users,
        FakeClock Clock,
        InMemoryPasswordHasher Hasher);

    private static async Task<Fixture> NewFixtureAsync()
    {
        var clock = new FakeClock(FixedNow);
        var hasher = new InMemoryPasswordHasher();
        var credentials = new InMemoryUserCredentialStore();
        var users = new InMemoryUserStore();
        var issuer = new InMemoryTokenIssuer();
        var families = new InMemoryRefreshFamilyStore();
        var userStores = new SingleUserStoreScope(users);
        var signIn = new SignInUseCase(
            credentials,
            userStores,
            hasher,
            new MinimumSignInTimingNormalizer(TimeSpan.Zero, clock),
            clock,
            issuer,
            families,
            RefreshTtl);
        var refresh = new RefreshSessionUseCase(families, userStores, issuer, clock, ReuseWindow);
        return new Fixture(signIn, refresh, families, issuer, credentials, users, clock, hasher);
    }

    private sealed class SingleUserStoreScope(InMemoryUserStore store) : IUserStoreScope
    {
        public Iam.Application.Users.IUserStore Member(string tenantId)
        {
            return store;
        }
    }

    private static async Task SeedUserAsync(Fixture fixture, string tenantId, string loginName)
    {
        var user = User.Provision(tenantId, loginName, Guid.NewGuid(), FixedNow);
        await fixture.Users.SaveAsync(user);
        await fixture.Credentials.SavePasswordHashAsync(tenantId, loginName, fixture.Hasher.Hash(CorrectPassword));
    }

    private static async Task<SignInResponse> SignInAsync(Fixture fixture, string tenantId, string loginName)
    {
        return await fixture.SignIn.HandleAsync(new SignInCommand(tenantId, loginName, CorrectPassword));
    }

    private static string ShaHex(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    [Fact]
    public async Task Valid_refresh_rotates_to_a_fresh_pair_and_invalidates_the_old_token()
    {
        var fixture = await NewFixtureAsync();
        await SeedUserAsync(fixture, "acme", "ada");
        var initial = await SignInAsync(fixture, "acme", "ada");
        var family = Assert.Single(fixture.Families.Families);
        Assert.Equal(ShaHex(initial.RefreshToken), family.CurrentRefreshTokenSha);
        Assert.Equal(RefreshTtl, family.RefreshExpiresAt - FixedNow);

        var rotated = await fixture.Refresh.HandleAsync(new RefreshCommand(initial.RefreshToken));

        Assert.NotEqual(initial.AccessToken, rotated.AccessToken);
        Assert.NotEqual(initial.RefreshToken, rotated.RefreshToken);
        var storedFamily = Assert.Single(fixture.Families.Families);
        Assert.Equal(ShaHex(initial.RefreshToken), storedFamily.RotatedFromSha);
        Assert.Equal(ShaHex(rotated.RefreshToken), storedFamily.CurrentRefreshTokenSha);
        Assert.Equal(2, fixture.Issuer.IssuedCount);
    }

    [Fact]
    public async Task Replay_within_the_window_returns_the_current_pair_without_new_issuance()
    {
        var fixture = await NewFixtureAsync();
        await SeedUserAsync(fixture, "acme", "ada");
        var initial = await SignInAsync(fixture, "acme", "ada");
        var current = await fixture.Refresh.HandleAsync(new RefreshCommand(initial.RefreshToken));
        var issuedBefore = fixture.Issuer.IssuedCount;

        var replayed = await fixture.Refresh.HandleAsync(new RefreshCommand(initial.RefreshToken));

        Assert.Equal(current.AccessToken, replayed.AccessToken);
        Assert.Equal(current.RefreshToken, replayed.RefreshToken);
        Assert.Equal(issuedBefore, fixture.Issuer.IssuedCount);
        Assert.True(fixture.Families.Families.Single(f => f.RotatedFromSha == ShaHex(initial.RefreshToken)).IsActive);
    }

    [Fact]
    public async Task Replay_beyond_the_window_revokes_the_family()
    {
        var fixture = await NewFixtureAsync();
        await SeedUserAsync(fixture, "acme", "ada");
        var initial = await SignInAsync(fixture, "acme", "ada");
        _ = await fixture.Refresh.HandleAsync(new RefreshCommand(initial.RefreshToken));

        fixture.Clock.Advance(ReuseWindow.Add(TimeSpan.FromSeconds(1)));

        await Assert.ThrowsAsync<TokenRefreshRejectedException>(
            () => fixture.Refresh.HandleAsync(new RefreshCommand(initial.RefreshToken)));
        Assert.False(fixture.Families.Families.Single(f => f.RotatedFromSha == ShaHex(initial.RefreshToken)).IsActive);
    }

    [Fact]
    public async Task Expired_refresh_is_rejected()
    {
        var fixture = await NewFixtureAsync();
        await SeedUserAsync(fixture, "acme", "ada");
        var initial = await SignInAsync(fixture, "acme", "ada");

        fixture.Clock.Advance(RefreshTtl.Add(TimeSpan.FromDays(1)));

        await Assert.ThrowsAsync<TokenRefreshRejectedException>(
            () => fixture.Refresh.HandleAsync(new RefreshCommand(initial.RefreshToken)));
        Assert.False(fixture.Families.Families.Single().IsActive);
    }

    [Fact]
    public async Task Two_families_from_separate_sign_ins_rotate_independently()
    {
        var fixture = await NewFixtureAsync();
        await SeedUserAsync(fixture, "acme", "ada");
        var first = await SignInAsync(fixture, "acme", "ada");
        var second = await SignInAsync(fixture, "acme", "ada");
        Assert.Equal(2, fixture.Families.Families.Count);

        var rotatedFirst = await fixture.Refresh.HandleAsync(new RefreshCommand(first.RefreshToken));
        var rotatedSecond = await fixture.Refresh.HandleAsync(new RefreshCommand(second.RefreshToken));

        Assert.NotEqual(rotatedFirst.RefreshToken, rotatedSecond.RefreshToken);
        Assert.Single(fixture.Families.Families, family => family.RotatedFromSha == ShaHex(first.RefreshToken));
        Assert.Single(fixture.Families.Families, family => family.RotatedFromSha == ShaHex(second.RefreshToken));
        Assert.Equal(4, fixture.Issuer.IssuedCount);
    }

    [Fact]
    public async Task Disabled_users_refresh_sweeps_all_families_and_rejects()
    {
        var fixture = await NewFixtureAsync();
        await SeedUserAsync(fixture, "acme", "ada");
        var initial = await SignInAsync(fixture, "acme", "ada");
        _ = await SignInAsync(fixture, "acme", "ada");
        var storedUser = (await fixture.Users.FindAsync("acme", "ada"))!;
        storedUser.Disable(FixedNow.AddMinutes(1));

        await Assert.ThrowsAsync<TokenRefreshRejectedException>(
            () => fixture.Refresh.HandleAsync(new RefreshCommand(initial.RefreshToken)));

        Assert.Equal(1, fixture.Families.RevokedAllCount);
        Assert.All(fixture.Families.Families, family => Assert.False(family.IsActive));
    }
}
