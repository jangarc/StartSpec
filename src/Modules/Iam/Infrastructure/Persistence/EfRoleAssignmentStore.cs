using Iam.Application.Authorization;
using Iam.Domain.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Iam.Infrastructure.Persistence;

/// <summary>
/// EF-backed <see cref="IRoleAssignmentStore"/> implementation. Member-scoped BY CONSTRUCTION: the
/// context is born with the resolved tenant (fail-closed global filter) plus an explicit tenant_id
/// predicate. The store owns its context; call sites SHALL dispose.
/// 指派 EF 持久實作：構造級租戶範圍（fail-closed 全域過濾＋明式 tenant_id 謂詞）；呼叫端須 dispose。
/// </summary>
public sealed class EfRoleAssignmentStore(DbContextOptions<IamDbContext> options, string activeTenantId)
    : IRoleAssignmentStore, IAsyncDisposable
{
    private readonly IamDbContext _db = new(options, activeTenantId);

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    public Task<RoleAssignment?> FindAsync(string tenantId, Guid assignmentId, CancellationToken cancellationToken = default) =>
        _db.RoleAssignments
            .Where(assignment => assignment.TenantId == tenantId && assignment.AssignmentId == assignmentId)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<RoleAssignment>> ListAsync(string tenantId, CancellationToken cancellationToken = default) =>
        await _db.RoleAssignments
            .Where(assignment => assignment.TenantId == tenantId)
            .ToListAsync(cancellationToken);

    public async Task SaveAsync(RoleAssignment assignment, CancellationToken cancellationToken = default)
    {
        // Issue inserts; revocation updates the SAME row in place so assignment_id stays the audit pivot.
        var existing = await _db.RoleAssignments.SingleOrDefaultAsync(candidate => candidate.AssignmentId == assignment.AssignmentId, cancellationToken);
        if (existing is null)
        {
            _db.Add(assignment);
        }
        else
        {
            _db.Entry(existing).CurrentValues.SetValues(assignment);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
