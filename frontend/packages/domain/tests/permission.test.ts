import { describe, expect, it } from 'vitest';
import { evaluatePermission } from '../src/permission';
import type { TenantScope } from '../src/tenantScope';

const scope: TenantScope = { tenantId: 't-1' };

describe('evaluatePermission', () => {
  it('allows an exact grant inside the active tenant', () => {
    const grants = [
      { tenantId: 't-1', permissionKey: 'tenant.provision' },
      { tenantId: 't-1', permissionKey: 'user.disable' },
    ];
    expect(evaluatePermission(scope, grants, 'user.disable')).toEqual({ outcome: 'allow', permissionKey: 'user.disable' });
  });

  it('denies by default when nothing is granted', () => {
    expect(evaluatePermission(scope, [], 'tenant.provision')).toEqual({
      outcome: 'deny',
      reason: 'not-granted',
      permissionKey: 'tenant.provision',
    });
  });

  it('refuses a key granted under another tenant (cross-tenant, not borrowable)', () => {
    const grants = [{ tenantId: 't-2', permissionKey: 'tenant.provision' }];
    expect(evaluatePermission(scope, grants, 'tenant.provision')).toEqual({
      outcome: 'deny',
      reason: 'cross-tenant',
      permissionKey: 'tenant.provision',
    });
  });

  it('denies a revoked-style near match (exact key semantics only)', () => {
    const grants = [{ tenantId: 't-1', permissionKey: 'tenant.provision' }];
    expect(evaluatePermission(scope, grants, 'tenant.provision.all')).toEqual({
      outcome: 'deny',
      reason: 'not-granted',
      permissionKey: 'tenant.provision.all',
    });
  });
});
