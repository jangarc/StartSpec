using System.Diagnostics;
using ContractSeedwork.Fakes;
using Iam.Application.Authorization;
using Iam.Domain.Authorization;
using Xunit;

namespace Iam.UnitTests.Authorization;

/// <summary>
/// Policy-watch revocation propagation (design decision 7, task 5.3): nodes keep a locally refreshed
/// enforcer snapshot; a RoleRevoked on the event bus tears the snapshot down, so every node denies
/// within the propagation bound, and redelivered events stay idempotent.
/// policy-watch 撤銷傳播：各節點本地快照，RoleRevoked 經匯流撕除快照，於上限內全部節點拒絕；重投遞冪等。
/// </summary>
public sealed class PolicyWatchTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-alpha";
    private const string Permission = "orders.refund";
    private static readonly TimeSpan TestBound = TimeSpan.FromMilliseconds(300);

    [Fact]
    public async Task Revocation_propagates_to_all_nodes_within_test_bound()
    {
        var fixture = BuildFixture(withSeedAssignment: true);
        var nodeA = fixture.NewNode();
        var nodeB = fixture.NewNode();

        Assert.True((await nodeA.DecideAsync(fixture.Evaluation())).Allowed);
        Assert.True((await nodeB.DecideAsync(fixture.Evaluation())).Allowed);

        var bound = Stopwatch.StartNew();
        await fixture.Revoke.HandleAsync(new RevokeRoleCommand(TenantId, fixture.Assignment!.AssignmentId));

        Assert.False((await nodeA.DecideAsync(fixture.Evaluation())).Allowed);
        Assert.False((await nodeB.DecideAsync(fixture.Evaluation())).Allowed);
        Assert.True(bound.Elapsed < TestBound);
    }

    [Fact]
    public async Task Redelivered_revocation_is_idempotent_across_nodes()
    {
        var fixture = BuildFixture(withSeedAssignment: true);
        var nodeA = fixture.NewNode();
        var nodeB = fixture.NewNode();
        await fixture.Revoke.HandleAsync(new RevokeRoleCommand(TenantId, fixture.Assignment!.AssignmentId));
        var baseline = (Deliveries: fixture.Bus.DeliveredCount, Publications: fixture.Bus.Published.Count);

        await fixture.Bus.SimulateRedeliveryAsync();
        await fixture.Bus.SimulateRedeliveryAsync();

        Assert.Equal(baseline.Publications, fixture.Bus.Published.Count);
        Assert.True(fixture.Bus.DeliveredCount > baseline.Deliveries);
        Assert.False((await nodeA.DecideAsync(fixture.Evaluation())).Allowed);
        Assert.False((await nodeB.DecideAsync(fixture.Evaluation())).Allowed);
    }

    [Fact]
    public async Task New_assignment_becomes_visible_to_the_same_node_enforcer()
    {
        var fixture = BuildFixture(withSeedAssignment: false);
        var node = fixture.NewNode();

        Assert.True((await node.DecideAsync(fixture.Evaluation())).Allowed is false);

        await new AssignRoleUseCase(
            fixture.RoleStores, fixture.AssignmentStores, fixture.Clock, node)
            .HandleAsync(new AssignRoleCommand(TenantId, fixture.UserId, fixture.Role.RoleId));

        Assert.True((await node.DecideAsync(fixture.Evaluation())).Allowed);
    }

    [Fact]
    public void AuthorizationOptions_rejects_non_positive_knobs()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AuthorizationOptions { PropagationBoundSeconds = 0 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new AuthorizationOptions { WatcherInterval = TimeSpan.Zero });
        var defaults = new AuthorizationOptions();
        Assert.Equal(5, defaults.PropagationBoundSeconds);
        Assert.Equal(TimeSpan.FromSeconds(1), defaults.WatcherInterval);
    }

    private static Fixture BuildFixture(bool withSeedAssignment)
    {
        var roleStore = new InMemoryRoleStore();
        var assignmentStore = new InMemoryRoleAssignmentStore();
        var clock = new FakeClock(FixedNow);
        var bus = new FakeEventBus();
        var roleStores = new SingleRoleStoreScope(roleStore);
        var assignmentStores = new SingleRoleAssignmentStoreScope(assignmentStore);

        var role = Role.Provision(TenantId, "refunder", [Permission]);
        roleStore.Save(role);
        Guid userId = Guid.NewGuid();
        RoleAssignment? assignment = null;
        if (withSeedAssignment)
        {
            assignment = RoleAssignment.Issue(role, TenantId, userId, clock.UtcNow);
            assignmentStore.Save(assignment);
        }

        return new Fixture(
            Bus: bus,
            Role: role,
            UserId: userId,
            Assignment: assignment,
            RoleStores: roleStores,
            AssignmentStores: assignmentStores,
            Clock: clock,
            Revoke: new RevokeRoleUseCase(assignmentStores, clock, bus),
            NewNode: () =>
            {
                var node = new PolicyEnforcer(roleStores, assignmentStores, bus);
                return node;
            },
            Evaluation: () => new AuthorizeCommand(TenantId, userId, Permission),
            Seed: (roleStore, assignmentStore));
    }

    private sealed record Fixture(
        FakeEventBus Bus,
        Role Role,
        Guid UserId,
        RoleAssignment? Assignment,
        IRoleStoreScope RoleStores,
        IRoleAssignmentStoreScope AssignmentStores,
        FakeClock Clock,
        RevokeRoleUseCase Revoke,
        Func<PolicyEnforcer> NewNode,
        Func<AuthorizeCommand> Evaluation,
        (InMemoryRoleStore Roles, InMemoryRoleAssignmentStore Assignments) Seed);

    private sealed class SingleRoleStoreScope(InMemoryRoleStore store) : IRoleStoreScope
    {
        public IRoleStore Member(string tenantId) => store;
    }

    private sealed class SingleRoleAssignmentStoreScope(InMemoryRoleAssignmentStore store) : IRoleAssignmentStoreScope
    {
        public IRoleAssignmentStore Member(string tenantId) => store;
    }

    private sealed class InMemoryRoleStore : IRoleStore
    {
        private readonly Dictionary<(string TenantId, Guid RoleId), Role> _rows = [];

        public void Save(Role role) => _rows[(role.TenantId, role.RoleId)] = role;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Task<Role?> FindAsync(string tenantId, Guid roleId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_rows.TryGetValue((tenantId, roleId), out var role) ? role : null);

        public Task<IReadOnlyList<Role>> ListAsync(string tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Role>>([.. _rows.Values.Where(role => role.TenantId == tenantId)]);

        public Task SaveAsync(Role role, CancellationToken cancellationToken = default)
        {
            Save(role);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryRoleAssignmentStore : IRoleAssignmentStore
    {
        private readonly Dictionary<(string TenantId, Guid AssignmentId), RoleAssignment> _rows = [];

        public void Save(RoleAssignment assignment) => _rows[(assignment.TenantId, assignment.AssignmentId)] = assignment;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Task<RoleAssignment?> FindAsync(string tenantId, Guid assignmentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_rows.TryGetValue((tenantId, assignmentId), out var row) ? row : null);

        public Task<IReadOnlyList<RoleAssignment>> ListAsync(string tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RoleAssignment>>([.. _rows.Values.Where(assignment => assignment.TenantId == tenantId)]);

        public Task SaveAsync(RoleAssignment assignment, CancellationToken cancellationToken = default)
        {
            Save(assignment);
            return Task.CompletedTask;
        }
    }
}
