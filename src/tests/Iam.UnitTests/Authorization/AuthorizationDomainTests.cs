using Iam.Domain.Authorization;
using Xunit;

namespace Iam.UnitTests.Authorization;

/// <summary>
/// Spec-derived unit expectations for the authorization domain (identity/authorization spec):
/// assignment / allow / deny / cross-tenant matrix. Facts authored before implementation (TDD red→green).
/// 授權 domain 契約場景由 spec 派生；本組事實先於實作撰寫（TDD 紅→綠）。
/// </summary>
public class AuthorizationDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 19, 1, 2, 3, TimeSpan.Zero);

    private static Role ReaderRoleOf(string tenantId) =>
        Role.Provision(tenantId, "reader", ["documents:read"]);

    [Fact]
    public void Provision_Builds_Tenant_Scoped_Role_With_Permission_Set()
    {
        var role = ReaderRoleOf("acme");

        Assert.NotEqual(Guid.Empty, role.RoleId);
        Assert.Equal("acme", role.TenantId);
        Assert.Equal("reader", role.Name);
        Assert.Equal(["documents:read"], role.Permissions);
    }

    [Fact]
    public void Provision_Rejects_Blank_Tenant_Or_Name_Or_Empty_Permissions()
    {
        Assert.Throws<ArgumentException>(() => Role.Provision(" ", "reader", ["documents:read"]));
        Assert.Throws<ArgumentException>(() => Role.Provision("acme", " ", ["documents:read"]));
        Assert.Throws<ArgumentException>(() => Role.Provision("acme", "reader", []));
        Assert.Throws<ArgumentException>(() => Role.Provision("acme", "reader", [" "]));
    }

    [Fact]
    public void Issue_Records_Assignment_Within_Tenant()
    {
        var role = ReaderRoleOf("acme");

        var assignment = RoleAssignment.Issue(role, "acme", Guid.NewGuid(), Now);

        Assert.NotEqual(Guid.Empty, assignment.AssignmentId);
        Assert.Equal("acme", assignment.TenantId);
        Assert.Equal(role.RoleId, assignment.RoleId);
        Assert.False(assignment.IsRevoked);
        Assert.Empty(assignment.PullDomainEvents());
    }

    [Fact]
    public void Issue_Rejects_CrossTenant_Assignment_And_Records_Nothing()
    {
        var role = ReaderRoleOf("acme");

        Assert.Throws<ArgumentException>(() => RoleAssignment.Issue(role, "globex", Guid.NewGuid(), Now));
    }

    [Fact]
    public void Revoke_Is_One_Way()
    {
        var assignment = RoleAssignment.Issue(ReaderRoleOf("acme"), "acme", Guid.NewGuid(), Now);

        assignment.Revoke(Now.AddSeconds(1));
        Assert.True(assignment.IsRevoked);
        Assert.Throws<InvalidOperationException>(() => assignment.Revoke(Now.AddSeconds(2)));
    }

    [Fact]
    public void Revoke_Raises_RoleRevoked_With_Complete_Envelope()
    {
        var role = ReaderRoleOf("acme");
        var userId = Guid.NewGuid();
        var assignment = RoleAssignment.Issue(role, "acme", userId, Now);
        var revokedAt = Now.AddSeconds(1);

        assignment.Revoke(revokedAt);

        var domainEvent = Assert.Single(assignment.PullDomainEvents());
        var revoked = Assert.IsType<RoleRevoked>(domainEvent);

        // ADR-009 standard envelope completeness.
        Assert.NotEqual(Guid.Empty, revoked.EventId);
        Assert.Equal(revokedAt, revoked.OccurredAt);
        Assert.Equal("acme", revoked.TenantId);
        Assert.Equal(1, revoked.Version);

        Assert.Equal(userId, revoked.UserId);
        Assert.Equal(role.RoleId, revoked.RoleId);
    }

    [Fact]
    public void Decider_Allows_Granted_Permission_Within_Member_Tenant()
    {
        var role = ReaderRoleOf("acme");
        var userId = Guid.NewGuid();
        var assignment = RoleAssignment.Issue(role, "acme", userId, Now);

        var decision = TenantPermissionDecider.Decide(
            evaluatedTenantId: "acme", permission: "documents:read", userTenantId: "acme", userId: userId,
            [role], [assignment]);

        Assert.True(decision);
    }

    [Fact]
    public void Decider_Denies_Missing_Grant()
    {
        var role = ReaderRoleOf("acme");
        var userId = Guid.NewGuid();
        var assignment = RoleAssignment.Issue(role, "acme", userId, Now);

        var decision = TenantPermissionDecider.Decide(
            evaluatedTenantId: "acme", permission: "billing:write", userTenantId: "acme", userId: userId,
            [role], [assignment]);

        Assert.False(decision);
    }

    [Fact]
    public void Decider_Denies_User_Without_Assignment()
    {
        var role = ReaderRoleOf("acme");

        var decision = TenantPermissionDecider.Decide(
            evaluatedTenantId: "acme", permission: "documents:read", userTenantId: "acme", userId: Guid.NewGuid(),
            [role], []);

        Assert.False(decision);
    }

    [Fact]
    public void Decider_Denies_CrossTenant_Evaluation_Regardless_Of_Grants()
    {
        var role = ReaderRoleOf("globex");
        var userId = Guid.NewGuid();
        var assignment = RoleAssignment.Issue(role, "globex", userId, Now);

        // A user of tenant "globex" evaluated against tenant "acme" scope is denied even though
        // the role itself grants the permission and the definitions list carries it.
        var decision = TenantPermissionDecider.Decide(
            evaluatedTenantId: "acme", permission: "documents:read", userTenantId: "globex", userId: userId,
            [ReaderRoleOf("acme"), role], [assignment]);

        Assert.False(decision);
    }

    [Fact]
    public void Decider_Denies_After_Assignment_Revocation()
    {
        var role = ReaderRoleOf("acme");
        var userId = Guid.NewGuid();
        var assignment = RoleAssignment.Issue(role, "acme", userId, Now);
        assignment.Revoke(Now.AddSeconds(1));

        var decision = TenantPermissionDecider.Decide(
            evaluatedTenantId: "acme", permission: "documents:read", userTenantId: "acme", userId: userId,
            [role], [assignment]);

        Assert.False(decision);
    }

    [Fact]
    public void Decider_Denies_Role_From_Another_Tenant_Even_With_Matching_Assignment_Id()
    {
        // Defense in depth: a definitions list that misses the member tenant's own role (here the
        // definitions list only carries globex's copy) must never enable a grant inside acme.
        var userId = Guid.NewGuid();
        var assignment = RoleAssignment.Issue(ReaderRoleOf("acme"), "acme", userId, Now);

        var decision = TenantPermissionDecider.Decide(
            evaluatedTenantId: "acme", permission: "documents:read", userTenantId: "acme", userId: userId,
            [ReaderRoleOf("globex")], [assignment]);

        Assert.False(decision);
    }
}
