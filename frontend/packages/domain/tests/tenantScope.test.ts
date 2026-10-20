import { describe, expect, it } from 'vitest';
import { resolveTenantScope, type TenantScope } from '../src/tenantScope';

const scope: TenantScope = { tenantId: 't-1' };

describe('resolveTenantScope', () => {
  it('allows the active tenant', () => {
    expect(resolveTenantScope(scope, 't-1')).toEqual({ kind: 'allowed' });
  });

  it('denies a different tenant as cross-tenant', () => {
    expect(resolveTenantScope(scope, 't-2')).toEqual({ kind: 'denied', reason: 'cross-tenant' });
  });

  it('denies a blank tenant id as malformed', () => {
    expect(resolveTenantScope(scope, '   ')).toEqual({ kind: 'denied', reason: 'malformed' });
  });
});
