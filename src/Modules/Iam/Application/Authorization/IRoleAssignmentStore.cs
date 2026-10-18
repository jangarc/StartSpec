using Iam.Domain.Authorization;

namespace Iam.Application.Authorization;

/// <summary>
/// Persistence port for tenant-scoped role assignments (Application-owned contract). Addressing is
/// tenant-scoped only; an unknown assignment resolves to NOTHING (denied-as-not-found). The store owns
/// its resources: call sites SHALL dispose.
/// 角色指派持久 port（契約歸 Application）：以租戶範圍尋址；未知指派回 null；呼叫端須 dispose。
/// </summary>
public interface IRoleAssignmentStore : IAsyncDisposable
{
    /// <summary>Persists the assignment; issue inserts, lifecycle transitions update in place.</summary>
    Task SaveAsync(RoleAssignment assignment, CancellationToken cancellationToken = default);

    /// <summary>Single lookup by exact tenant + assignment pair.</summary>
    Task<RoleAssignment?> FindAsync(string tenantId, Guid assignmentId, CancellationToken cancellationToken = default);

    /// <summary>Tenant-scoped listing for evaluation snapshots (parameter-level tenant scope).</summary>
    Task<IReadOnlyList<RoleAssignment>> ListAsync(string tenantId, CancellationToken cancellationToken = default);
}
