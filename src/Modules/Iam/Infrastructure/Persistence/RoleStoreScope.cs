using Iam.Application.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Iam.Infrastructure.Persistence;

/// <summary>
/// Scope factory over shared <see cref="IamDbContext"/> options: role definitions are member-only —
/// every store face is born with the resolved tenant (construction-level filter, fail-closed).
/// 角色範圍工廠：一律攜已解析租戶構造（fail-closed）。
/// </summary>
public sealed class RoleStoreScope(DbContextOptions<IamDbContext> options) : IRoleStoreScope
{
    public IRoleStore Member(string tenantId) => new EfRoleStore(options, tenantId);
}
