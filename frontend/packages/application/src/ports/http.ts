/**
 * HttpPort (task 7.4): transport seam — org-unit apps inject one adapter (fetch today, Tauri path
 * open). Signatures are pure TS; no framework types and no browser API may appear here (7.5 lint).
 * The authenticated scope is server-side policy: the adapter attaches the access token taken from
 * the app's signed-in session (design decision 2) — the request itself stays transport-agnostic.
 * 傳輸端口：純 TS 簽名；驗證由轉接器附上登入工作階段的權杖（決策 2）；請求本體不涉傳輸細節。
 */
export type HttpPortRequestMethod = 'GET' | 'POST' | 'DELETE';

export interface HttpPortRequest {
  readonly method: HttpPortRequestMethod;
  readonly path: string;
  readonly body?: Readonly<Record<string, unknown>>;
}

export interface HttpPortResponse<TBody> {
  readonly status: number;
  readonly body: TBody;
}

export interface HttpPort {
  send<TBody>(request: HttpPortRequest): Promise<HttpPortResponse<TBody>>;
}
