/**
 * AuthPort (task 7.4): the auth seam — sign-in / refresh only; the app shell composes the returned
 * pair with domain session helpers. Pure TS signatures; transport and token persistence live in
 * adapters inside each app shell.
 * auth 接縫：僅登入與刷新；app 殼以 domain 工作階段輔助組合結果；token 持久化在各 app 的轉接器。
 */
export interface SignInInput {
  readonly tenantId: string;
  readonly loginName: string;
  readonly password: string;
}

export interface RefreshInput {
  readonly refreshToken: string;
}

export interface SessionTokens {
  readonly accessToken: string;
  readonly refreshToken: string;
}

export interface SignInOutput extends SessionTokens {
  readonly userId: string;
  readonly partyId: string;
}

export type SignInRejection = 'authentication_failed';
export type RefreshRejection = 'refresh_rejected';

export interface AuthPort {
  signIn(input: SignInInput): Promise<SignInOutput>;
  refresh(input: RefreshInput): Promise<SessionTokens>;
}
