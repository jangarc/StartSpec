using System.Text;
using Iam.Application.Authorization;
using Iam.Application.Parties;
using Iam.Application.Tenants;
using Iam.Application.Users;
using Iam.Infrastructure.Persistence;
using Iam.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Ports;

namespace Iam.Infrastructure;

/// <summary>
/// The module's single composition entry (design G4): hosts call <c>AddIam</c> and touch nothing else
/// in this layer; ArchTests whitelist exactly this type.
/// 模組唯一組合入口：Host 僅經此組裝，ArchTests 白名單明文豁免。
/// </summary>
public static class IamComposition
{
    public static IServiceCollection AddIam(this IServiceCollection services, string connectionString, string jwtSigningKey, AuthorizationOptions? authorizationOptions = null)
    {
        var options = new DbContextOptionsBuilder<IamDbContext>().UseNpgsql(connectionString).Options;

        services.AddSingleton(options);
        services.AddSingleton<ITenantStoreScope, TenantStoreScope>();
        services.AddScoped<ITenantStore>(sp => sp.GetRequiredService<ITenantStoreScope>().Platform());
        services.AddScoped<ProvisionTenantUseCase>();
        services.AddScoped<IPartyStore, EfPartyStore>();
        services.AddScoped<ProvisionPartyUseCase>();
        services.AddScoped<AttachPartyRoleUseCase>();
        services.AddScoped<DeactivatePartyRoleUseCase>();
        services.AddScoped<GrantConsentUseCase>();
        services.AddScoped<RevokeConsentUseCase>();
        services.AddSingleton<IUserStoreScope, UserStoreScope>();
        services.AddScoped<ProvisionUserUseCase>();
        services.AddScoped<DisableUserUseCase>();
        services.AddScoped<IamSeeder>();
        services.AddSingleton<IRoleStoreScope, RoleStoreScope>();
        services.AddSingleton<IRoleAssignmentStoreScope, RoleAssignmentStoreScope>();
        services.AddSingleton(authorizationOptions ?? new AuthorizationOptions());
        services.AddSingleton<PolicyEnforcer>();
        services.AddScoped<ProvisionRoleUseCase>();
        services.AddScoped<AssignRoleUseCase>();
        services.AddScoped<RevokeRoleUseCase>();
        services.AddScoped<AuthorizeUseCase>();

        // Signature normalization floor: 150 milliseconds (design decision 4 — same magnitude on every
        // sign-in path so an unknown tenant cannot fail fast).
        services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
        services.AddScoped<IUserCredentialStore, EfUserCredentialStore>();
        services.AddSingleton<ISignInTimingNormalizer>(
            sp => new MinimumSignInTimingNormalizer(TimeSpan.FromMilliseconds(150), sp.GetRequiredService<IClock>()));

        services.AddSingleton<ITokenIssuer>(sp => new JwtTokenIssuer(
            Encoding.UTF8.GetBytes(jwtSigningKey),
            sp.GetRequiredService<IClock>(),
            TimeSpan.FromMinutes(5))); // JwtTtlMinutes = 5 (design decision 7; deployment-level, not authorization input)
        services.AddScoped<IRefreshFamilyStore, EfRefreshFamilyStore>();
        services.AddScoped<SignInUseCase>(sp => new SignInUseCase(
            sp.GetRequiredService<IUserCredentialStore>(),
            sp.GetRequiredService<IUserStoreScope>(),
            sp.GetRequiredService<IPasswordHasher>(),
            sp.GetRequiredService<ISignInTimingNormalizer>(),
            sp.GetRequiredService<IClock>(),
            sp.GetRequiredService<ITokenIssuer>(),
            sp.GetRequiredService<IRefreshFamilyStore>(),
            TimeSpan.FromDays(14))); // refresh family TTL = 14 days (design decision 3)
        services.AddScoped<RefreshSessionUseCase>(sp => new RefreshSessionUseCase(
            sp.GetRequiredService<IRefreshFamilyStore>(),
            sp.GetRequiredService<IUserStoreScope>(),
            sp.GetRequiredService<ITokenIssuer>(),
            sp.GetRequiredService<IClock>(),
            TimeSpan.FromSeconds(30))); // RefreshReuseWindowSeconds = 30 (design decision 3)

        return services;
    }
}
