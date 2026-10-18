using Iam.Domain.Users;
using Xunit;

namespace Iam.UnitTests.Users;

/// <summary>
/// User aggregate TDD (tasks 3.4 + 4.1): creation invariants — born active, tenant-scoped, bound to
/// the resolved Party; lifecycle — disable deactivates once and raises UserDisabled, repeated disable
/// is rejected; events — provisioning raises UserProvisioned on the ADR-009 envelope and
/// PullDomainEvents drains exhaustively.
/// User 聚合 TDD：建立即啟用、租戶範圍、綁定 Party；停用＝一次轉換並發 UserDisabled、重複停用拒絕；建立事件符合標準信封、PullDomainEvents 泄流。
/// </summary>
public sealed class UserDomainTests
{
    private static readonly DateTimeOffset ProvisionedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void New_user_starts_active_and_bound_to_its_party()
    {
        var partyId = Guid.NewGuid();

        var user = User.Provision("acme", "ada", partyId, ProvisionedAt);

        Assert.NotEqual(Guid.Empty, user.UserId);
        Assert.Equal("acme", user.TenantId);
        Assert.Equal("ada", user.LoginName);
        Assert.Equal(partyId, user.PartyId);
        Assert.True(user.IsActive);
    }

    [Theory]
    [InlineData(null, "ada")]
    [InlineData("  ", "ada")]
    [InlineData("acme", null)]
    [InlineData("acme", " ")]
    public void Provision_rejects_blank_tenant_or_login(string? tenantId, string? loginName)
    {
        Assert.Throws<ArgumentException>(() => User.Provision(tenantId!, loginName!, Guid.NewGuid(), ProvisionedAt));
    }

    [Fact]
    public void Provision_rejects_an_empty_party_binding()
    {
        Assert.Throws<ArgumentException>(() => User.Provision("acme", "ada", Guid.Empty, ProvisionedAt));
    }

    [Fact]
    public void Provision_trims_the_supplied_names()
    {
        var user = User.Provision("  acme ", " ada ", Guid.NewGuid(), ProvisionedAt);

        Assert.Equal("acme", user.TenantId);
        Assert.Equal("ada", user.LoginName);
    }

    [Fact]
    public void Provisioning_raises_UserProvisioned_on_the_standard_envelope()
    {
        var partyId = Guid.NewGuid();

        var user = User.Provision("acme", "ada", partyId, ProvisionedAt);

        var provisioned = Assert.IsType<UserProvisioned>(Assert.Single(user.PullDomainEvents()));
        Assert.Equal(user.UserId, provisioned.UserId);
        Assert.Equal(partyId, provisioned.PartyId);
        Assert.Equal("ada", provisioned.LoginName);
        Assert.Equal("acme", provisioned.TenantId);
        Assert.Equal(ProvisionedAt, provisioned.OccurredAt);
    }

    [Fact]
    public void Pulling_events_drains_them_exhaustively()
    {
        var user = User.Provision("acme", "ada", Guid.NewGuid(), ProvisionedAt);
        _ = user.PullDomainEvents();

        Assert.Empty(user.PullDomainEvents());
    }

    [Fact]
    public void Disable_deactivates_once_and_raises_UserDisabled()
    {
        var user = User.Provision("acme", "ada", Guid.NewGuid(), ProvisionedAt);
        _ = user.PullDomainEvents();
        var disabledAt = ProvisionedAt.AddHours(1);

        user.Disable(disabledAt);

        Assert.False(user.IsActive);
        var disabled = Assert.IsType<UserDisabled>(Assert.Single(user.PullDomainEvents()));
        Assert.Equal(user.UserId, disabled.UserId);
        Assert.Equal("acme", disabled.TenantId);
        Assert.Equal(disabledAt, disabled.OccurredAt);
    }

    [Fact]
    public void Disabling_an_already_disabled_user_is_rejected()
    {
        var user = User.Provision("acme", "ada", Guid.NewGuid(), ProvisionedAt);
        user.Disable(ProvisionedAt.AddHours(1));

        Assert.Throws<InvalidOperationException>(() => user.Disable(ProvisionedAt.AddHours(2)));
    }
}
