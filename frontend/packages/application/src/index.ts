/**
 * Application ports (task 7.4, shared across app shells): platform capabilities reach app shells ONLY
 * through these signatures — no framework types ever appear here (7.5 lint assert).
 * 共用應用端口：平台能力只經 ports 進殼；port 簽名禁框架型別（7.5 lint 斷言）。
 */
export { type AuthPort, type RefreshInput, type RefreshRejection, type SessionTokens, type SignInInput, type SignInOutput, type SignInRejection } from './ports/auth';
export { type HttpPort, type HttpPortRequest, type HttpPortRequestMethod, type HttpPortResponse } from './ports/http';
