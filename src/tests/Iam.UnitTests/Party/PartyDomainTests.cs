using Iam.Domain.Party;
using Xunit;

namespace Iam.UnitTests.PartyDomain;

/// <summary>
/// Spec-derived unit expectations for the Party aggregate (identity/party spec):
/// provisioning / exact-tuple binding / near-match creation / fuzzy-merge impossibility, plus role
/// and consent invariants. Authored BEFORE the implementation (TDD red → green).
/// 契約場景由 spec 派生；本組事實先於實作撰寫（TDD 紅→綠）。
/// </summary>
public class PartyDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 2, 3, 4, TimeSpan.Zero);

    private static readonly DateOnly AdaBorn = new(1990, 3, 1);

    private static PartyIdentifier AdaIdCard()
    {
        return PartyIdentifier.Create("id-card", "A123456789");
    }

    [Fact]
    public void Provision_Creates_Party_From_A_Never_Seen_Identifier_Tuple()
    {
        var party = Party.Provision("Ada Chen", AdaBorn, [AdaIdCard()], Now);

        Assert.NotEqual(Guid.Empty, party.PartyId);
        Assert.Equal("Ada Chen", party.DisplayName);
        Assert.Equal(AdaBorn, party.DateOfBirth);

        var identifier = Assert.Single(party.Identifiers);
        Assert.Equal("id-card", identifier.TypeCode);
        Assert.Equal("A123456789", identifier.Number);
    }

    [Fact]
    public void Provision_Raises_PartyProvisioned_With_Complete_Envelope()
    {
        var party = Party.Provision("Ada Chen", AdaBorn, [AdaIdCard()], Now);

        var domainEvent = Assert.Single(party.PullDomainEvents());
        var provisioned = Assert.IsType<PartyProvisioned>(domainEvent);

        // ADR-009 standard envelope completeness; a Party is platform-scoped, so TenantId is null.
        Assert.NotEqual(Guid.Empty, provisioned.EventId);
        Assert.Equal(Now, provisioned.OccurredAt);
        Assert.Equal(1, provisioned.Version);
        Assert.Null(provisioned.TenantId);

        Assert.Equal(party.PartyId, provisioned.PartyId);
        Assert.Equal("Ada Chen", provisioned.DisplayName);
    }

    [Fact]
    public void PullDomainEvents_Is_Exhausting()
    {
        var party = Party.Provision("Ada Chen", AdaBorn, [AdaIdCard()], Now);

        Assert.Single(party.PullDomainEvents());
        Assert.Empty(party.PullDomainEvents());
    }

    [Fact]
    public void Near_Match_Creates_A_Distinct_Party_Instead_Of_Merging()
    {
        // Same name + birthdate, different identifier number: the tuples do NOT match, so these are
        // two separate parties — fuzzy merge has no representation at identity level.
        const string differentNumber = "B187654321";
        var first = Party.Provision("Ada Chen", AdaBorn, [AdaIdCard()], Now);
        var second = Party.Provision("Ada Chen", AdaBorn, [PartyIdentifier.Create("id-card", differentNumber)], Now);

        Assert.NotEqual(first.PartyId, second.PartyId);
        Assert.NotEqual(first.Identifiers[0], second.Identifiers[0]);
    }

    [Fact]
    public void Identifier_Tuple_Matches_Only_Exact_Type_And_Number()
    {
        var tuple = AdaIdCard();

        Assert.Equal(PartyIdentifier.Create("id-card", " A123456789 "), tuple);
        Assert.NotEqual(tuple, PartyIdentifier.Create("id-card", "B123456789"));
        Assert.NotEqual(tuple, PartyIdentifier.Create("passport", "A123456789"));
    }

    [Fact]
    public void Exact_Match_Binds_A_New_Tenant_Role_To_The_Existing_Party()
    {
        // "Exact match binds without duplication": the SAME Party object gains the new tenant's role;
        // no merge artifact, identifiers untouched.
        var party = Party.Provision("Ada Chen", AdaBorn, [AdaIdCard()], Now);
        party.AttachTenantRole("acme", "employee", Now);

        party.AttachTenantRole("globex", "examinee", Now);

        Assert.Equal(2, party.Roles.Count);
        Assert.Single(party.Identifiers);
    }

    [Fact]
    public void One_Human_Plays_Multiple_Roles_Within_The_Same_Tenant()
    {
        var party = Party.Provision("Ada Chen", AdaBorn, [AdaIdCard()], Now);

        party.AttachTenantRole("acme", "employee", Now);
        party.AttachTenantRole("acme", "examinee", Now);

        Assert.Equal(2, party.Roles.Count);
        Assert.All(party.Roles, role => Assert.Equal("acme", role.TenantId));
    }

    [Fact]
    public void AttachTenantRole_Rejects_A_Duplicate_Active_Role()
    {
        var party = Party.Provision("Ada Chen", AdaBorn, [AdaIdCard()], Now);
        party.AttachTenantRole("acme", "employee", Now);

        Assert.Throws<InvalidOperationException>(() => party.AttachTenantRole("acme", "employee", Now));
    }

    [Fact]
    public void Deactivating_A_Role_Preserves_Party_Identifier_And_Consent_History()
    {
        var party = Party.Provision("Ada Chen", AdaBorn, [AdaIdCard()], Now);
        party.AttachTenantRole("acme", "employee", Now);
        party.GrantConsent("acme", "treatment-data", Now);

        party.DeactivateTenantRole("acme", "employee", Now.AddMinutes(5));

        var role = Assert.Single(party.Roles);
        Assert.False(role.IsActive);
        Assert.Equal(Now.AddMinutes(5), role.DeactivatedAt);
        Assert.Single(party.Identifiers);
        Assert.Single(party.Consents);
    }

    [Fact]
    public void DeactivateTenantRole_Rejects_An_Unknown_Active_Role()
    {
        var party = Party.Provision("Ada Chen", AdaBorn, [AdaIdCard()], Now);

        Assert.Throws<InvalidOperationException>(() => party.DeactivateTenantRole("acme", "employee", Now));
    }

    [Fact]
    public void GrantConsent_Appends_Timestamped_Immutable_Records()
    {
        var party = Party.Provision("Ada Chen", AdaBorn, [AdaIdCard()], Now);

        party.GrantConsent("acme", "treatment-data", Now);
        party.GrantConsent("acme", "treatment-data", Now.AddMinutes(1));

        Assert.Equal(2, party.Consents.Count);
        Assert.All(party.Consents, record =>
        {
            Assert.Equal(ConsentKind.Grant, record.Kind);
            Assert.Equal("acme", record.TenantId);
            Assert.Equal("treatment-data", record.Purpose);
        });
        Assert.Equal(Now, party.Consents[0].OccurredAt);
        Assert.Equal(Now.AddMinutes(1), party.Consents[1].OccurredAt);
    }

    [Fact]
    public void Revoking_Consent_Appends_A_Record_And_Retains_The_Original()
    {
        var party = Party.Provision("Ada Chen", AdaBorn, [AdaIdCard()], Now);
        party.GrantConsent("acme", "treatment-data", Now);

        party.RevokeConsent("acme", "treatment-data", Now.AddMinutes(2));

        Assert.Equal(2, party.Consents.Count);
        Assert.Equal(ConsentKind.Grant, party.Consents[0].Kind);
        Assert.Equal(Now, party.Consents[0].OccurredAt);
        Assert.Equal(ConsentKind.Revoke, party.Consents[1].Kind);
        Assert.Equal(Now.AddMinutes(2), party.Consents[1].OccurredAt);
    }

    [Fact]
    public void Provision_Rejects_Blank_Display_Name()
    {
        Assert.Throws<ArgumentException>(() => Party.Provision(" ", AdaBorn, [AdaIdCard()], Now));
    }

    [Fact]
    public void Provision_Rejects_A_Missing_Identifier()
    {
        Assert.Throws<ArgumentException>(() => Party.Provision("Ada Chen", AdaBorn, [], Now));
    }

    [Fact]
    public void Provision_Rejects_Duplicate_Identifier_Tuples()
    {
        var identifiers = new[] { AdaIdCard(), PartyIdentifier.Create("id-card", "A123456789") };

        Assert.Throws<ArgumentException>(() => Party.Provision("Ada Chen", AdaBorn, identifiers, Now));
    }

    [Fact]
    public void PartyIdentifier_Create_Rejects_Blank_Fields()
    {
        Assert.Throws<ArgumentException>(() => PartyIdentifier.Create(" ", "A123456789"));
        Assert.Throws<ArgumentException>(() => PartyIdentifier.Create("id-card", " "));
    }
}
