using Iam.Domain.Authorization;

namespace Iam.Application.Authorization;

/// <summary>
/// Persistence port for tenant-scoped role definitions (Application-owned contract; implemented by
/// module Infrastructure). Addressing is tenant-scoped only; an unknown role resolves to NOTHING
/// (denied-as-not-found), so foreign-tenant roles are unreachable with no distinguishable error shape.
/// The store owns its resources: call sites SHALL dispose.
/// 角色定義持久 port（契約歸 Application）：以租戶範圍尋址；未知角色回 null（denied-as-not-found）；呼叫端須 dispose。
/// </summary>
public interface IRoleStore : IAsyncDisposable
{
    /// <summary>Single lookup by exact tenant + role pair.</summary>
    Task<Role?> FindAsync(string tenantId, Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>Tenant-scoped listing for evaluation snapshots (parameter-level tenant scope).</summary>
    Task<IReadOnlyList<Role>> ListAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>Persists the role definition; duplicate (tenant, name) SHALL conflict at the store.</summary>
    Task SaveAsync(Role role, CancellationToken cancellationToken = default);
}
