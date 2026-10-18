using Iam.Application.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Iam.Infrastructure.Persistence;

/// <summary>
/// Scope factory over shared <see cref="IamDbContext"/> options: role assignments are member-only —
/// every store face is born with the resolved tenant (construction-level filter, fail-closed).
/// 指派範圍工廠：一律攜已解析租戶構造（fail-closed）。
/// </summary>
public sealed class RoleAssignmentStoreScope(DbContextOptions<IamDbContext> options) : IRoleAssignmentStoreScope
{
    public IRoleAssignmentStore Member(string tenantId) => new EfRoleAssignmentStore(options, tenantId);
}
