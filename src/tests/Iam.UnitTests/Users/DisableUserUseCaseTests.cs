using ContractSeedwork.Fakes;
using Iam.Application.Users;
using Iam.Domain.Users;
using Xunit;

namespace Iam.UnitTests.Users;

/// <summary>
/// <see cref="DisableUserUseCase"/> (task 6.1): the one-way disable transition persists the aggregate
/// and publishes UserDisabled over the event bus EXACTLY once; a second disable rejects without
/// another event; an unknown login denies as not-found. No host surface in this slice (no spec
/// scenarios) — the publisher itself is the 6.1 deliverable.
/// 一-way 停用轉換：持久化聚合並經匯流排恰發布一枚 UserDisabled；重複停用被逐、不再發布；未知 login 以
/// not-found 拒絕。本期無 host 面（規格無場景）——發布方本身即 6.1 交付物。
/// </summary>
public sealed class DisableUserUseCaseTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-alpha";
    private const string LoginName = "alice";

    [Fact]
    public async Task Disable_publishes_user_disabled_once_and_persists_the_transition()
    {
        var fixture = Fixture.WithUser();

        await fixture.UseCase.HandleAsync(new DisableUserCommand(TenantId, LoginName));

        var userDisabled = Assert.IsType<UserDisabled>(Assert.Single(fixture.Bus.Published));
        Assert.Equal(fixture.UserId, userDisabled.UserId);
        Assert.Equal(TenantId, userDisabled.TenantId);
        Assert.Equal(FixedNow, userDisabled.OccurredAt);
        Assert.Equal(1, userDisabled.Version);
        Assert.False(fixture.Store.Find(TenantId, LoginName)!.IsActive);
    }

    [Fact]
    public async Task Disabling_twice_rejects_without_a_second_event()
    {
        var fixture = Fixture.WithUser();
        await fixture.UseCase.HandleAsync(new DisableUserCommand(TenantId, LoginName));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.UseCase.HandleAsync(new DisableUserCommand(TenantId, LoginName)));

        Assert.Single(fixture.Bus.Published);
    }

    [Fact]
    public async Task Disabling_unknown_login_is_denied_as_not_found()
    {
        var fixture = Fixture.WithUser();

        await Assert.ThrowsAsync<UserUnknownException>(
            () => fixture.UseCase.HandleAsync(new DisableUserCommand(TenantId, "who")));

        Assert.Empty(fixture.Bus.Published);
    }

    [Fact]
    public async Task Disable_sweeps_all_refresh_families_eagerly()
    {
        var fixture = Fixture.WithUser();

        await fixture.UseCase.HandleAsync(new DisableUserCommand(TenantId, LoginName));

        var revocation = Assert.Single(fixture.Families.Revocations);
        Assert.Equal(TenantId, revocation.TenantId);
        Assert.Equal(fixture.UserId, revocation.UserId);
    }

    private sealed class Fixture
    {
        public FakeEventBus Bus { get; } = new();

        public InMemoryUserStore Store { get; } = new();

        public InMemoryRefreshFamilies Families { get; } = new();

        public Guid UserId { get; private set; }

        public DisableUserUseCase UseCase { get; private set; } = null!;

        public static Fixture WithUser()
        {
            var fixture = new Fixture();
            var user = Iam.Domain.Users.User.Provision(TenantId, LoginName, Guid.NewGuid(), FixedNow);
            user.PullDomainEvents();
            fixture.UserId = user.UserId;
            fixture.Store.Save(user);
            fixture.UseCase = new DisableUserUseCase(new SingleUserStoreScope(fixture.Store), fixture.Families, new FakeClock(FixedNow), fixture.Bus);
            return fixture;
        }
    }

    private sealed class InMemoryRefreshFamilies : IRefreshFamilyStore
    {
        public List<StoredRevoke> Revocations { get; } = [];

        public readonly record struct StoredRevoke(string TenantId, Guid UserId);

        public Task<StoredRefreshFamily?> FindByTokenShaAsync(string tokenSha256, CancellationToken cancellationToken = default)
            => Task.FromResult<StoredRefreshFamily?>(null);

        public Task SaveAsync(StoredRefreshFamily family, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RevokeAllAsync(string tenantId, Guid userId, CancellationToken cancellationToken = default)
        {
            Revocations.Add(new StoredRevoke(tenantId, userId));
            return Task.CompletedTask;
        }
    }

    private sealed class SingleUserStoreScope(InMemoryUserStore store) : IUserStoreScope
    {
        public IUserStore Member(string tenantId) => store;
    }

    private sealed class InMemoryUserStore : IUserStore
    {
        private readonly Dictionary<(string TenantId, string LoginName), Iam.Domain.Users.User> _rows = [];

        public void Save(Iam.Domain.Users.User user) => _rows[(user.TenantId, user.LoginName)] = user;

        public Iam.Domain.Users.User? Find(string tenantId, string loginName) =>
            _rows.TryGetValue((tenantId, loginName), out var user) ? user : null;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Task<Iam.Domain.Users.User?> FindAsync(string tenantId, string loginName, CancellationToken cancellationToken = default) =>
            Task.FromResult(Find(tenantId, loginName));

        public Task SaveAsync(Iam.Domain.Users.User user, CancellationToken cancellationToken = default)
        {
            Save(user);
            return Task.CompletedTask;
        }
    }
}
