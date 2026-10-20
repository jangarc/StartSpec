/**
 * Permission evaluation (task 7.2): deny-by-default, exact permission-key match inside the active
 * tenant; a key granted under ANOTHER tenant MUST NOT satisfy here (跨租戶拒絕鏡射後端判定矩陣).
 * 權限判定：預設拒絕、作用中租戶內精確 permission key 命中；他租戶的授權不得借用。
 */
import type { TenantScope } from './tenantScope';

export interface PermissionGrant {
  readonly tenantId: string;
  readonly permissionKey: string;
}

export type PermissionDecision =
  | { readonly outcome: 'allow'; readonly permissionKey: string }
  | { readonly outcome: 'deny'; readonly reason: 'not-granted' | 'cross-tenant'; readonly permissionKey: string };

export function evaluatePermission(
  scope: TenantScope,
  grants: readonly PermissionGrant[],
  permissionKey: string,
): PermissionDecision {
  const inScope = grants.filter((grant) => grant.tenantId === scope.tenantId && grant.permissionKey === permissionKey);
  if (inScope.length > 0) {
    return { outcome: 'allow', permissionKey };
  }
  const cross = grants.some((grant) => grant.tenantId !== scope.tenantId && grant.permissionKey === permissionKey);
  if (cross) {
    return { outcome: 'deny', reason: 'cross-tenant', permissionKey };
  }
  return { outcome: 'deny', reason: 'not-granted', permissionKey };
}
