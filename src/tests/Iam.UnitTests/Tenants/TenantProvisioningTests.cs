using ContractSeedwork;
using Iam.Domain.Tenants;
using Xunit;

namespace Iam.UnitTests.Tenants;

/// <summary>
/// Spec-derived unit expectations for <c>Tenant provisioning</c> (identity/tenant spec).
/// 契約場景由 spec 派生；本組事實先於實作撰寫（TDD 紅→綠）。
/// </summary>
public class TenantProvisioningTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 1, 2, 3, TimeSpan.Zero);

    [Fact]
    public void Provision_Creates_Tenant_With_Initial_Organization()
    {
        var tenant = Tenant.Provision("acme", "Acme Health", "Acme HQ", Now);

        Assert.Equal("acme", tenant.TenantId);
        Assert.Equal("Acme Health", tenant.Name);

        var organization = Assert.Single(tenant.Organizations);
        Assert.Equal("Acme HQ", organization.Name);
    }

    [Fact]
    public void Provision_Raises_TenantProvisioned_With_Complete_Envelope()
    {
        var tenant = Tenant.Provision("acme", "Acme Health", "Acme HQ", Now);

        var domainEvent = Assert.Single(tenant.PullDomainEvents());
        var provisioned = Assert.IsType<TenantProvisioned>(domainEvent);

        // ADR-009 standard envelope completeness.
        Assert.NotEqual(Guid.Empty, provisioned.EventId);
        Assert.Equal(Now, provisioned.OccurredAt);
        Assert.Equal("acme", provisioned.TenantId);
        Assert.Equal(1, provisioned.Version);

        Assert.Equal(tenant.Organizations[0].OrganizationId, provisioned.OrganizationId);
        Assert.Equal("Acme Health", provisioned.TenantName);
    }

    [Fact]
    public void PullDomainEvents_Is_Exhausting()
    {
        var tenant = Tenant.Provision("acme", "Acme Health", "Acme HQ", Now);

        Assert.Single(tenant.PullDomainEvents());
        Assert.Empty(tenant.PullDomainEvents());
    }

    [Fact]
    public void Provision_Rejects_Blank_Tenant_Identifier()
    {
        Assert.Throws<ArgumentException>(() => Tenant.Provision(" ", "Acme Health", "Acme HQ", Now));
    }

    [Fact]
    public void Provision_Rejects_Blank_Tenant_Name()
    {
        Assert.Throws<ArgumentException>(() => Tenant.Provision("acme", "", "Acme HQ", Now));
    }

    [Fact]
    public void Provision_Rejects_Blank_Initial_Organization_Name()
    {
        Assert.Throws<ArgumentException>(() => Tenant.Provision("acme", "Acme Health", "  ", Now));
    }
}
