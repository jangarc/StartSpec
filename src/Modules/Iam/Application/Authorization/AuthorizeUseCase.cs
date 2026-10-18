using Iam.Domain.Authorization;

namespace Iam.Application.Authorization;

/// <summary>{tenant scope, user, permission} — handed over by the Host with no further business validation (G3: business rules in Application).</summary>
public sealed record AuthorizeCommand(string TenantId, Guid UserId, string Permission);

/// <summary>Allow/deny only — never a reason, never other tenants' data (G1: contract owned by the use case).</summary>
public sealed record PermissionDecision(bool Allowed);

/// <summary>
/// Server-side decision (design decision 7): evaluation rides the node's enforcer snapshot — data
/// outside the acting tenant's scope never loads, so cross-tenancy resolves to deny with no special
/// branch.
/// 伺服器端判定（決策 7）：經本節點執行器快照判定；範圍外資料不載入，跨租戶自動 deny。
/// </summary>
public sealed class AuthorizeUseCase(PolicyEnforcer enforcer)
{
    public Task<PermissionDecision> HandleAsync(AuthorizeCommand command, CancellationToken cancellationToken = default)
    {
        return enforcer.DecideAsync(command, cancellationToken);
    }
}
