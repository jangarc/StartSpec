/**
 * Working session (task 7.2): pure data + decision over the short-lived access token.
 * Time is ALWAYS passed in as millis — the domain layer never reads the system clock (7.5 lint).
 * 工作階段：短效存取權杖之上的純資料與判定；時間一律以毫秒參數傳入（禁直取系統時鐘，7.5 lint 斷言）。
 */
export interface WorkingSession {
  readonly tenantId: string;
  readonly loginName: string;
  readonly accessExpiresAtMillis: number;
}

/** Usable iff `now + skew` is strictly before the access expiry（權杖於 exp 前可用、含 skew 餘裕） */
export function isAccessUsable(session: WorkingSession, nowMillis: number, skewMillis = 0): boolean {
  return nowMillis + skewMillis < session.accessExpiresAtMillis;
}

/** Outcome when the access token can no longer be used（存取權杖失效時的下一步判定） */
export type SessionNextStep = 'refresh' | 'sign-in';

/** Expired access still inside the refresh family lifetime → refresh; otherwise sign in again. */
export function sessionNextStep(session: WorkingSession, nowMillis: number, refreshFamilyEndsAtMillis: number): SessionNextStep {
  return nowMillis < refreshFamilyEndsAtMillis ? 'refresh' : 'sign-in';
}
