using ContractSeedwork.Fakes;
using Iam.Application.Parties;
using Iam.Application.Users;
using Iam.ContractTests.Fakes;
using Xunit;

namespace Iam.UnitTests.Users;

/// <summary>
/// ProvisionUserUseCase TDD (tasks 3.4 + 4.1) over the locked store contract fakes: per-tenant unique
/// login is checked BEFORE any party-side effect (conflict leaves zero side effects), the party
/// identity resolves through the canonical party provisioning (exact hit binds / miss creates), the
/// user persists bound to that party, and UserProvisioned publishes through the bus.
/// 用例 TDD：登入名查重先於任何 party 側副作用；party 身分經正典 party 用例解析（命中綁定／否則新建）；
/// 保存綁定後發布 UserProvisioned。
/// </summary>
public sealed class ProvisionUserUseCaseTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private sealed record Fixture(ProvisionUserUseCase UseCase, InMemoryPartyStore PartyStore, InMemoryUserStore UserStore, FakeEventBus Bus, List<string> RequestedTenants);

    private static Fixture NewFixture()
    {
        var partyStore = new InMemoryPartyStore();
        var bus = new FakeEventBus();
        var userStore = new InMemoryUserStore();
        var requestedTenants = new List<string>();
        var partyProvisioning = new ProvisionPartyUseCase(partyStore, new FakeClock(FixedNow), bus);
        var useCase = new ProvisionUserUseCase(partyProvisioning, new SingleUserStoreScope(userStore, requestedTenants), new FakeClock(FixedNow), bus);
        return new Fixture(useCase, partyStore, userStore, bus, requestedTenants);
    }

    private sealed class SingleUserStoreScope(InMemoryUserStore store, List<string> requestedTenants) : IUserStoreScope
    {
        public Iam.Application.Users.IUserStore Member(string tenantId)
        {
            requestedTenants.Add(tenantId);
            return store;
        }
    }

    private static ProvisionUserCommand Command(string tenantId, string loginName, string typeCode = "id-card", string number = "tuple-a") =>
        new(tenantId, loginName, "Ada Chen", new DateOnly(1990, 3, 1), typeCode, number);

    [Fact]
    public async Task Creates_new_party_and_saves_the_active_user()
    {
        var fixture = NewFixture();

        var response = await fixture.UseCase.HandleAsync(Command("acme", "ada"));

        Assert.NotEqual(Guid.Empty, response.UserId);
        Assert.Equal(response.PartyId, (await fixture.PartyStore.FindExactAsync(Iam.Domain.Party.PartyIdentifier.Create("id-card", "tuple-a")))?.PartyId);
        Assert.Equal(1, fixture.UserStore.SavedCount);
        var saved = await fixture.UserStore.FindAsync("acme", "ada");
        Assert.NotNull(saved);
        Assert.Equal(response.UserId, saved!.UserId);
        Assert.True(saved.IsActive);
        Assert.Equal(2, fixture.Bus.Published.Count);
        Assert.IsType<Iam.Domain.Party.PartyProvisioned>(fixture.Bus.Published[0]);
        var userEvent = Assert.IsType<Iam.Domain.Users.UserProvisioned>(fixture.Bus.Published[1]);
        Assert.Equal(response.UserId, userEvent.UserId);
        Assert.Equal("acme", userEvent.TenantId);
    }

    [Fact]
    public async Task Exact_tuple_hit_binds_existing_party_without_a_second_one()
    {
        var fixture = NewFixture();
        var first = await fixture.UseCase.HandleAsync(Command("acme", "ada"));
        var second = await fixture.UseCase.HandleAsync(Command("acme", "bob", number: "tuple-a"));

        Assert.Equal(first.PartyId, second.PartyId);
        var hit = await fixture.PartyStore.FindExactAsync(Iam.Domain.Party.PartyIdentifier.Create("id-card", "tuple-a"));
        Assert.NotNull(hit);
        Assert.Equal(first.PartyId, hit.PartyId);
        // per call one publication: hit binds the party (no second PartyProvisioned), only the new user is announced
        Assert.Equal(3, fixture.Bus.Published.Count);
        Assert.Single(fixture.Bus.Published.OfType<Iam.Domain.Party.PartyProvisioned>());
        var userEvents = fixture.Bus.Published.OfType<Iam.Domain.Users.UserProvisioned>().ToArray();
        Assert.Equal(2, userEvents.Length);
        Assert.NotEqual(userEvents[0].UserId, userEvents[1].UserId);
        Assert.Equal(first.PartyId, userEvents[1].PartyId);
    }

    [Fact]
    public async Task Duplicate_login_conflicts_before_any_party_side_effect()
    {
        var fixture = NewFixture();
        _ = await fixture.UseCase.HandleAsync(Command("acme", "ada"));
        var eventsBefore = fixture.Bus.Published.Count;
        var savedBefore = fixture.UserStore.SavedCount;

        var rejected = await Assert.ThrowsAsync<UserLoginConflictException>(
            () => fixture.UseCase.HandleAsync(Command("acme", "ada", number: "tuple-b")));

        Assert.Equal("acme", rejected.TenantId);
        Assert.Equal("ada", rejected.LoginName);
        Assert.Equal(eventsBefore, fixture.Bus.Published.Count); // new tuple would have created a party — nothing published
        Assert.Equal(savedBefore, fixture.UserStore.SavedCount); // nothing saved on conflict
        Assert.Null(await fixture.PartyStore.FindExactAsync(Iam.Domain.Party.PartyIdentifier.Create("id-card", "tuple-b")));
    }

    [Fact]
    public async Task Same_login_in_another_tenant_is_independent()
    {
        var fixture = NewFixture();

        var inAcme = await fixture.UseCase.HandleAsync(Command("acme", "shared"));
        var inGlobex = await fixture.UseCase.HandleAsync(Command("globex", "shared", number: "tuple-c"));

        Assert.NotEqual(inAcme.UserId, inGlobex.UserId);
        Assert.Equal(["acme", "globex"], fixture.RequestedTenants);
        var acmeUser = await fixture.UserStore.FindAsync("acme", "shared");
        var globexUser = await fixture.UserStore.FindAsync("globex", "shared");
        Assert.NotNull(acmeUser);
        Assert.NotNull(globexUser);
    }
}
