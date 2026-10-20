import type { HttpPort, HttpPortRequest, HttpPortResponse } from '@startspec/application';

/**
 * Web adapter of HttpPort (task 7.4 + design decision 2): fetch with the signed-in session's Bearer
 * access token; the active tenant scope is server-side from validated claims. Lives in the app shell
 * — the shared packages never touch browser APIs (7.5 lint); swapping to a Tauri adapter only
 * replaces this file.
 * HttpPort 的 Web 轉接器：fetch＋登入工作階段的 Bearer；範圍伺服器端解析；swap 只動本檔（Tauri 路徑敞開）。
 */
export function createFetchHttpPort(getAccessToken: () => string | null, baseUrl = ''): HttpPort {
  return {
    async send<TBody>(request: HttpPortRequest): Promise<HttpPortResponse<TBody>> {
      const headers: Record<string, string> = { 'content-type': 'application/json' };
      const accessToken = getAccessToken();
      if (accessToken) {
        headers['authorization'] = `Bearer ${accessToken}`;
      }
      const response = await fetch(`${baseUrl}${request.path}`, {
        method: request.method,
        headers,
        body: request.body === undefined ? undefined : JSON.stringify(request.body),
      });
      const text = await response.text();
      const body = (text.length > 0 ? JSON.parse(text) : {}) as TBody;
      return { status: response.status, body };
    },
  };
}
