import type { AuthPort, RefreshInput, SessionTokens, SignInInput, SignInOutput } from '@startspec/application';
import type { HttpPort } from '@startspec/application';

/**
 * API adapter of AuthPort (task 7.4): drives /iam/auth/sign-in and /iam/auth/refresh through the
 * app's HttpPort; rejections surface as typed errors so screens can render one line per failure
 * family (spec: one body per rejection family, no oracle).
 * AuthPort 的 API 轉接器：經 HttpPort 打登入／刷新；拒絕以型別化錯誤上拋，畫面單行呈現。
 */
export function createApiAuthPort(http: HttpPort): AuthPort {
  return {
    async signIn(input: SignInInput): Promise<SignInOutput> {
      const response = await http.send<SignInOutput & { title?: string }>({
        method: 'POST',
        path: '/iam/auth/sign-in',
        body: { tenantId: input.tenantId, loginName: input.loginName, password: input.password },
      });
      if (response.status !== 200) {
        throw new Error(response.body.title === 'authentication_failed' ? 'authentication_failed' : 'invalid_signin_request');
      }
      return response.body;
    },
    async refresh(input: RefreshInput): Promise<SessionTokens> {
      const response = await http.send<SessionTokens & { title?: string }>({
        method: 'POST',
        path: '/iam/auth/refresh',
        body: { refreshToken: input.refreshToken },
      });
      if (response.status !== 200) {
        throw new Error(response.body.title === 'refresh_rejected' ? 'refresh_rejected' : 'invalid_refresh_request');
      }
      return response.body;
    },
  };
}
