using System.Collections.Concurrent;
using Iam.Domain.Authorization;
using SharedKernel.Ports;

namespace Iam.Application.Authorization;

/// <summary>
/// Server-side enforcer (design decision 7): each node keeps a per-tenant policy snapshot and decides
/// locally — no per-request policy database query. The snapshot is torn down when RoleRevoked arrives
/// over the event bus, so revoked permissions deny on every node within the propagation bound. Tear-down
/// is clearing a map entry: redelivered events (at-least-once) are idempotent no-ops.
/// 伺服器端執行器（決策 7）：每節點持有租戶級政策快照、本地判定；RoleRevoked 到站即撕除快照，
/// 撤銷權限於上限內在所有節點拒絕；撕除為清 map——at-least-once 重投遞為冪等無操作。
/// </summary>
public sealed class PolicyEnforcer : IDisposable
{
    private readonly IRoleStoreScope _roleStores;

    private readonly IRoleAssignmentStoreScope _assignmentStores;

    private readonly IDisposable _subscription;

    private readonly object _snapshotLock = new();

    private Dictionary<string, PolicySnapshot> _snapshots = [];

    public PolicyEnforcer(IRoleStoreScope roleStores, IRoleAssignmentStoreScope assignmentStores, IEventBus eventBus)
    {
        _roleStores = roleStores;
        _assignmentStores = assignmentStores;
        _subscription = eventBus.Subscribe<RoleRevoked>((_, _) =>
        {
            InvalidateAll();
            return Task.CompletedTask;
        });
    }

    public void Dispose() => _subscription.Dispose();

    /// <summary>Decides using the tenant's cached snapshot; the first decision per tenant loads it from the policy stores.</summary>
    /// <summary>以租戶快照判定；每租戶首次判定自政策庫載入。</summary>
    public async Task<PermissionDecision> DecideAsync(AuthorizeCommand command, CancellationToken cancellationToken = default)
    {
        var snapshot = await SnapshotForAsync(command.TenantId, cancellationToken);
        return new PermissionDecision(TenantPermissionDecider.Decide(
            command.TenantId,
            command.Permission,
            command.TenantId,
            command.UserId,
            snapshot.Roles,
            snapshot.Assignments));
    }

    /// <summary>Drops one tenant's snapshot (next decision reloads fresh policy data); idempotent.</summary>
    /// <summary>撕除單一租戶快照（下次判定重新載入）；冪等。</summary>
    public void Invalidate(string tenantId) => Evict(tenantId);

    /// <summary>Drops every cached snapshot; idempotent and redelivery-safe (RoleRevoked carries no tenant id, so the watch is coarse).</summary>
    /// <summary>撕除全部快照；冪等、重投遞安全（RoleRevoked 不帶租戶 id，watch 採粗粒度）。</summary>
    public void InvalidateAll() => Evict(null);

    private async Task<PolicySnapshot> SnapshotForAsync(string tenantId, CancellationToken cancellationToken)
    {
        if (_snapshots.TryGetValue(tenantId, out var cached))
        {
            return cached;
        }

        var loaded = await LoadAsync(tenantId, cancellationToken);
        lock (_snapshotLock)
        {
            if (_snapshots.TryGetValue(tenantId, out var raced))
            {
                // A concurrent load won the race; both loads reflect the same store state at that moment.
                return raced;
            }

            _snapshots[tenantId] = loaded;
            return loaded;
        }
    }

    private async Task<PolicySnapshot> LoadAsync(string tenantId, CancellationToken cancellationToken)
    {
        await using var roleStore = _roleStores.Member(tenantId);
        var roles = await roleStore.ListAsync(tenantId, cancellationToken);
        await using var assignmentStore = _assignmentStores.Member(tenantId);
        var assignments = await assignmentStore.ListAsync(tenantId, cancellationToken);
        return new PolicySnapshot(roles, assignments);
    }

    private void Evict(string? tenantId)
    {
        lock (_snapshotLock)
        {
            if (tenantId is null)
            {
                _snapshots = [];
                return;
            }

            _snapshots.Remove(tenantId, out _);
        }
    }

    private sealed record PolicySnapshot(IReadOnlyList<Role> Roles, IReadOnlyList<RoleAssignment> Assignments);
}
