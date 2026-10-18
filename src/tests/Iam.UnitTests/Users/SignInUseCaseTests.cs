using ContractSeedwork.Fakes;
using Iam.Application.Users;
using Iam.ContractTests.Fakes;
using Iam.Domain.Users;
using Xunit;

namespace Iam.UnitTests.Users;

/// <summary>
/// SignInUseCase TDD (task 4.2): valid credentials sign in; wrong password, unknown login, unknown
/// tenant, and a disabled user all collapse into ONE generic failure (no enumeration oracle), and the
/// response is padded to the same minimum duration on success AND failure — an unknown tenant must
/// not fail fast.
/// 登入用例 TDD：有效憑證登入；錯密／未知名／未知租戶／停用四種失敗一律同一通用錯誤；成功與失敗皆墊至同一最短時長，未知租戶不得快失敗。
/// </summary>
public sealed class SignInUseCaseTests
{
    private const string GenericMessage = "Authentication failed.";
    private static readonly DateTimeOffset FixedNow = new(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan TimingFloor = TimeSpan.FromMilliseconds(10);
    private static readonly TimeSpan RefreshTtl = TimeSpan.FromDays(14);
    private const string CorrectPassword = "correct-horse";

    private sealed record Fixture(SignInUseCase UseCase, InMemoryUserCredentialStore Credentials, InMemoryPasswordHasher Hasher, InMemoryUserStore Users, InMemoryTokenIssuer Issuer, InMemoryRefreshFamilyStore Families);

    private static async Task<Fixture> NewFixtureAsync()
    {
        var hasher = new InMemoryPasswordHasher();
        var credentials = new InMemoryUserCredentialStore();
        var users = new InMemoryUserStore();
        var issuer = new InMemoryTokenIssuer();
        var families = new InMemoryRefreshFamilyStore();
        var clock = new FakeClock(FixedNow);
        var useCase = new SignInUseCase(
            credentials,
            new SingleUserStoreScope(users),
            hasher,
            new MinimumSignInTimingNormalizer(TimingFloor, clock),
            clock,
            issuer,
            families,
            RefreshTtl);
        return new Fixture(useCase, credentials, hasher, users, issuer, families);
    }

    private sealed class SingleUserStoreScope(InMemoryUserStore store) : IUserStoreScope
    {
        public Iam.Application.Users.IUserStore Member(string tenantId)
        {
            return store;
        }
    }

    private static async Task SeedUserAsync(InMemoryUserStore users, InMemoryPasswordHasher hasher, InMemoryUserCredentialStore credentials, string tenantId, string loginName, bool disabled = false)
    {
        var user = User.Provision(tenantId, loginName, Guid.NewGuid(), FixedNow);
        if (disabled)
        {
            user.Disable(FixedNow.AddMinutes(1));
        }

        await users.SaveAsync(user);
        await credentials.SavePasswordHashAsync(tenantId, loginName, hasher.Hash(CorrectPassword));
    }

    [Fact]
    public async Task Valid_credentials_sign_in_and_are_padded_to_the_floor()
    {
        var fixture = await NewFixtureAsync();
        await SeedUserAsync(fixture.Users, fixture.Hasher, fixture.Credentials, "acme", "ada");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await fixture.UseCase.HandleAsync(new SignInCommand("acme", "ada", CorrectPassword));
        stopwatch.Stop();

        Assert.NotEqual(Guid.Empty, response.UserId);
        Assert.NotEqual(Guid.Empty, response.PartyId);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
        Assert.True(stopwatch.Elapsed >= TimingFloor, $"padded sign-in took only {stopwatch.Elapsed}");
    }

    [Fact]
    public async Task Wrong_password_fails_generically_without_fast_path()
    {
        var fixture = await NewFixtureAsync();
        await SeedUserAsync(fixture.Users, fixture.Hasher, fixture.Credentials, "acme", "ada");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var rejected = await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => fixture.UseCase.HandleAsync(new SignInCommand("acme", "ada", "wrong-password")));
        stopwatch.Stop();

        Assert.Equal(GenericMessage, rejected.Message);
        Assert.True(stopwatch.Elapsed >= TimingFloor, $"padded failure took only {stopwatch.Elapsed}");
    }

    [Fact]
    public async Task Unknown_login_is_indistinguishable_from_wrong_password()
    {
        var fixture = await NewFixtureAsync();
        await SeedUserAsync(fixture.Users, fixture.Hasher, fixture.Credentials, "acme", "ada");

        var rejected = await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => fixture.UseCase.HandleAsync(new SignInCommand("acme", "ghost", CorrectPassword)));

        Assert.Equal(GenericMessage, rejected.Message);
    }

    [Fact]
    public async Task Unknown_tenant_is_indistinguishable_and_not_fast()
    {
        var fixture = await NewFixtureAsync();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var rejected = await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => fixture.UseCase.HandleAsync(new SignInCommand("ghost-tent", "ada", CorrectPassword)));
        stopwatch.Stop();

        Assert.Equal(GenericMessage, rejected.Message);
        Assert.True(stopwatch.Elapsed >= TimingFloor, $"unknown tenant must not fail fast, took {stopwatch.Elapsed}");
    }

    [Fact]
    public async Task Disabled_user_fails_generically()
    {
        var fixture = await NewFixtureAsync();
        await SeedUserAsync(fixture.Users, fixture.Hasher, fixture.Credentials, "acme", "ada", disabled: true);

        var rejected = await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => fixture.UseCase.HandleAsync(new SignInCommand("acme", "ada", CorrectPassword)));

        Assert.Equal(GenericMessage, rejected.Message);
    }
}
