/**
 * Tenant scope (task 7.2): a verified session carries EXACTLY one active tenant; anything else is a
 * cross-tenant refusal, mirroring the server-side invariant (作用中租戶僅由已驗證工作階段解析).
 * 租戶範圍：已驗證工作階段恰帶一個作用中租戶；其他一律為跨租戶拒絕，鏡射伺服器端不變式。
 */
export interface TenantScope {
  readonly tenantId: string;
}

export type ScopeDecision =
  | { readonly kind: 'allowed' }
  | { readonly kind: 'denied'; readonly reason: 'cross-tenant' | 'malformed' };

/** Blank / whitespace tenant id is malformed; any other tenant than the active one is cross-tenant. */
export function resolveTenantScope(scope: TenantScope, requestedTenantId: string): ScopeDecision {
  const requested = requestedTenantId.trim();
  if (requested.length === 0) {
    return { kind: 'denied', reason: 'malformed' };
  }
  if (requested !== scope.tenantId) {
    return { kind: 'denied', reason: 'cross-tenant' };
  }
  return { kind: 'allowed' };
}
