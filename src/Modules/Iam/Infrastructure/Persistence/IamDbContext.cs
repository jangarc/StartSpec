using Iam.Application.Users;
using Iam.Domain.Authorization;
using Iam.Domain.Party;
using Iam.Domain.Tenants;
using Iam.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Iam.Infrastructure.Persistence;

/// <summary>
/// IAM EF Core database context (PostgreSQL, schema <c>iam</c>). The <c>tenant_id</c> global query
/// filter is demand-side construction-level isolation: a scoped context NEVER sees other tenants'
/// organizations, and an unscoped context sees NONE of them (fail-closed). Platform-level tables
/// (<c>tenants</c>, later the Party store) are exempt per identity/party spec.
/// EF 全域過濾為構造級隔離：scoped 上下文絕不見他租戶資料；無 scope 上下文什麼都不見（fail-closed）。
/// </summary>
public class IamDbContext(DbContextOptions<IamDbContext> options) : DbContext(options)
{
    /// <summary>Active tenant resolved server-side (null = unscoped/migrator/tooling context).</summary>
    private readonly string? _activeTenantId = null;

    public IamDbContext(DbContextOptions<IamDbContext> options, string activeTenantId)
        : this(options)
    {
        _activeTenantId = activeTenantId;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<Party> Parties => Set<Party>();

    public DbSet<PartyIdentifier> Identifiers => Set<PartyIdentifier>();

    public DbSet<User> Users => Set<User>();

    public DbSet<StoredRefreshFamily> RefreshFamilies => Set<StoredRefreshFamily>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<RoleAssignment> RoleAssignments => Set<RoleAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("iam");

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenants");
            entity.HasKey(tenant => tenant.TenantId).HasName("pk_tenants");
            entity.Property(tenant => tenant.TenantId).HasColumnName("tenant_id").HasMaxLength(128).IsRequired();
            entity.Property(tenant => tenant.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
            entity.HasMany(tenant => tenant.Organizations)
                .WithOne(organization => organization.Tenant)
                .HasForeignKey("tenant_id")
                .HasPrincipalKey(tenant => tenant.TenantId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.ToTable("organizations");
            entity.HasKey(organization => organization.OrganizationId).HasName("pk_organizations");
            entity.Property(organization => organization.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(organization => organization.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
            entity.HasIndex("tenant_id", nameof(Organization.OrganizationId)).HasDatabaseName("ix_organizations_tenant");
            entity.HasQueryFilter(organization => EF.Property<string>(organization, "tenant_id") == _activeTenantId);
        });

        modelBuilder.Entity<Party>(entity =>
        {
            entity.ToTable("parties");
            entity.HasKey(partyRow => partyRow.PartyId).HasName("pk_parties");
            entity.Property(partyRow => partyRow.PartyId).HasColumnName("party_id").IsRequired();
            entity.Property(partyRow => partyRow.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
            entity.Property(partyRow => partyRow.DateOfBirth).HasColumnName("date_of_birth");

            entity.HasMany(partyRow => partyRow.Identifiers)
                .WithOne()
                .HasForeignKey("party_id")
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(partyRow => partyRow.Roles)
                .WithOne()
                .HasForeignKey("party_id")
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(partyRow => partyRow.Consents)
                .WithOne()
                .HasForeignKey("party_id")
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PartyIdentifier>(entity =>
        {
            entity.ToTable("party_identifier");
            entity.HasKey("party_id", nameof(PartyIdentifier.TypeCode), nameof(PartyIdentifier.Number)).HasName("pk_party_identifier");
            entity.Property(identifier => identifier.TypeCode).HasColumnName("type_code").HasMaxLength(128).IsRequired();
            entity.Property(identifier => identifier.Number).HasColumnName("number").HasMaxLength(256).IsRequired();
            entity.HasIndex(identifier => new { identifier.TypeCode, identifier.Number }).IsUnique().HasDatabaseName("ix_party_identifier_tuple");
        });

        modelBuilder.Entity<PartyRole>(entity =>
        {
            entity.ToTable("party_role");
            entity.HasKey(role => role.PartyRoleId).HasName("pk_party_role");
            entity.Property(role => role.PartyRoleId).HasColumnName("party_role_id").IsRequired();
            entity.Property(role => role.TenantId).HasColumnName("tenant_id").HasMaxLength(128).IsRequired();
            entity.Property(role => role.RoleType).HasColumnName("role_type").HasMaxLength(128).IsRequired();
            entity.Property(role => role.AttachedAt).HasColumnName("attached_at").IsRequired();
            entity.Property(role => role.DeactivatedAt).HasColumnName("deactivated_at");
            entity.HasIndex(role => new { role.TenantId, role.RoleType }).HasDatabaseName("ix_party_role_tenant_role");
        });

        modelBuilder.Entity<ConsentRecord>(entity =>
        {
            entity.ToTable("party_consent");
            entity.HasKey(record => record.ConsentId).HasName("pk_party_consent");
            entity.Property(record => record.ConsentId).HasColumnName("consent_id").IsRequired();
            entity.Property(record => record.Kind).HasColumnName("consent_kind");
            entity.Property(record => record.TenantId).HasColumnName("tenant_id").HasMaxLength(128).IsRequired();
            entity.Property(record => record.Purpose).HasColumnName("purpose").HasMaxLength(256).IsRequired();
            entity.Property(record => record.OccurredAt).HasColumnName("occurred_at").IsRequired();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(user => user.UserId).HasName("pk_users");
            entity.Property(user => user.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(user => user.TenantId).HasColumnName("tenant_id").HasMaxLength(128).IsRequired();
            entity.Property(user => user.LoginName).HasColumnName("login_name").HasMaxLength(128).IsRequired();
            entity.Property(user => user.PartyId).HasColumnName("party_id").IsRequired();
            entity.Property(user => user.IsActive).HasColumnName("is_active").IsRequired();

            // password_hash is a shadow property owned by the auth seam (IUserCredentialStore); the
            // domain aggregate never carries credential material.
            entity.Property<string>("password_hash");

            entity.HasOne<Party>()
                .WithMany()
                .HasForeignKey(user => user.PartyId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(user => new { user.TenantId, user.LoginName }).IsUnique().HasDatabaseName("ix_users_tenant_login");

            entity.HasQueryFilter(user => user.TenantId == _activeTenantId);
        });

        modelBuilder.Entity<StoredRefreshFamily>(entity =>
        {
            // Platform-level reach store (same exemption as parties/tenants): rows belong to a tenant,
            // but the session path addresses a family ONLY by the exact refresh-token SHA — no known
            // tenant at lookup time, no listable or traversal surface. Tenant isolation for the sweep
            // path carries tenant_id explicitly in RevokeAllAsync.
            entity.ToTable("refresh_families");
            entity.HasKey(family => family.FamilyId).HasName("pk_refresh_families");
            entity.Property(family => family.FamilyId).HasColumnName("family_id").IsRequired();
            entity.Property(family => family.TenantId).HasColumnName("tenant_id").HasMaxLength(128).IsRequired();
            entity.Property(family => family.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(family => family.LoginName).HasColumnName("login_name").HasMaxLength(128).IsRequired();
            entity.Property(family => family.CurrentRefreshTokenSha).HasColumnName("current_refresh_token_sha").HasMaxLength(64).IsRequired();
            entity.Property(family => family.CurrentRefreshToken).HasColumnName("current_refresh_token").HasMaxLength(512).IsRequired();
            entity.Property(family => family.CurrentAccessToken).HasColumnName("current_access_token").HasMaxLength(4096).IsRequired();
            entity.Property(family => family.RefreshExpiresAt).HasColumnName("refresh_expires_at").IsRequired();
            entity.Property(family => family.RotatedFromSha).HasColumnName("rotated_from_sha").HasMaxLength(64);
            entity.Property(family => family.RotatedAt).HasColumnName("rotated_at");
            entity.Property(family => family.IsActive).HasColumnName("is_active").IsRequired();

            entity.HasIndex(family => family.CurrentRefreshTokenSha).HasDatabaseName("ix_refresh_families_current_sha");
            entity.HasIndex(family => family.RotatedFromSha).HasDatabaseName("ix_refresh_families_rotated_sha");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(role => role.RoleId).HasName("pk_roles");
            entity.Property(role => role.RoleId).HasColumnName("role_id").IsRequired();
            entity.Property(role => role.TenantId).HasColumnName("tenant_id").HasMaxLength(128).IsRequired();
            entity.Property(role => role.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
            entity.Property(role => role.Permissions).HasColumnName("permission_grants");
            entity.HasIndex(role => new { role.TenantId, role.Name }).IsUnique().HasDatabaseName("ix_roles_tenant_name");
            entity.HasQueryFilter(role => role.TenantId == _activeTenantId);
        });

        modelBuilder.Entity<RoleAssignment>(entity =>
        {
            entity.ToTable("role_assignments");
            entity.HasKey(assignment => assignment.AssignmentId).HasName("pk_role_assignments");
            entity.Property(assignment => assignment.AssignmentId).HasColumnName("assignment_id").IsRequired();
            entity.Property(assignment => assignment.TenantId).HasColumnName("tenant_id").HasMaxLength(128).IsRequired();
            entity.Property(assignment => assignment.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(assignment => assignment.RoleId).HasColumnName("role_id").IsRequired();
            entity.Property(assignment => assignment.IssuedAt).HasColumnName("issued_at").IsRequired();
            entity.Property(assignment => assignment.IsRevoked).HasColumnName("is_revoked").IsRequired();
            entity.Property(assignment => assignment.RevokedAt).HasColumnName("revoked_at");
            entity.HasIndex(assignment => new { assignment.TenantId, assignment.UserId }).HasDatabaseName("ix_role_assignments_tenant_user");
            entity.HasQueryFilter(assignment => assignment.TenantId == _activeTenantId);
        });
    }
}
