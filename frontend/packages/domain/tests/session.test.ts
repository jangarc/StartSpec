import { describe, expect, it } from 'vitest';
import { isAccessUsable, sessionNextStep, type WorkingSession } from '../src/session';

const session: WorkingSession = {
  tenantId: 't-1',
  loginName: 'operator',
  accessExpiresAtMillis: 5_000,
};

describe('isAccessUsable', () => {
  it('usable strictly before expiry', () => {
    expect(isAccessUsable(session, 4_999)).toBe(true);
  });

  it('expired at expiry instant', () => {
    expect(isAccessUsable(session, 5_000)).toBe(false);
  });

  it('skew shrinks the usable window', () => {
    expect(isAccessUsable(session, 3_000, 2_000)).toBe(false);
    expect(isAccessUsable(session, 3_000, 1_999)).toBe(true);
  });
});

describe('sessionNextStep', () => {
  it('refresh while inside the refresh family window', () => {
    expect(sessionNextStep(session, 5_000, 60_000)).toBe('refresh');
  });

  it('sign-in after the refresh family window closed', () => {
    expect(sessionNextStep(session, 60_000, 60_000)).toBe('sign-in');
  });
});
