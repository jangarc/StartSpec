# Spec Delta

## Purpose

Covers user lifecycle and credential-based authentication with short-lived sessions and refresh, backing platform sign-in for all downstream contexts. 涵蓋使用者生命週期與憑證驗證（含短效工作階段與刷新），支撐全平台登入與下游上下文使用。

## ADDED Requirements

### Requirement: User provisioning within tenant

The system SHALL provision users scoped to a single tenant with a per-tenant unique login name. Provisioning SHALL bind to the existing Party on exact identifier match, or create a new Party otherwise. Duplicate login names within the same tenant SHALL be rejected. 系統 SHALL 在單一租戶內建立使用者，登入名稱在租戶內唯一；建立時 SHALL 以精確識別命中既有 Party 並綁定，否則新建 Party；同租戶重複名稱 SHALL 拒絕。

#### Scenario: Provision user

- **WHEN** an administrator provisions a user with a login name not used in that tenant（管理員以該租戶未使用的登入名稱建立使用者）
- **THEN** the system creates the user in active state within that tenant（系統在該租戶內建立啟用狀態的使用者）

#### Scenario: Exact identifier match binds existing party

- **WHEN** provisioning presents an identifier tuple matching an existing Party（建立時出示命中既有 Party 的識別元組）
- **THEN** the system binds the new user to that Party without duplicating identity data（系統將新使用者綁定該 Party，不重複身份資料）

#### Scenario: Duplicate login rejected

- **WHEN** an administrator provisions a user with a login name already used in that tenant（管理員以該租戶已使用的登入名稱建立使用者）
- **THEN** the system rejects the request with a conflict error（系統以衝突錯誤拒絕）

### Requirement: Credential authentication issues session tokens

The system SHALL authenticate credentials presented with a tenant identifier (`tenant_id`, `login_name`, `password`) and issue a short-lived access token plus a refresh token. Invalid credentials, unknown tenants, and disabled users SHALL all be rejected with an identical generic failure, with response timing normalized to prevent oracles. 系統 SHALL 驗證攜租戶識別的憑證並簽發短效存取權杖與刷新權杖；無效憑證、未知租戶、停用帳號 SHALL 一律以相同通用失敗拒絕，並歸一化回應時間以防 oracle。

#### Scenario: Successful sign-in

- **WHEN** a user signs in with valid credentials（使用者以有效憑證登入）
- **THEN** the system returns an access token and a refresh token for that user's tenant scope（系統回傳該使用者租戶範圍的存取權杖與刷新權杖）

#### Scenario: Invalid credentials rejected generically

- **WHEN** a user signs in with a wrong password or unknown login name（使用者以錯誤密碼或未知名稱登入）
- **THEN** the system rejects the sign-in with a generic authentication failure（系統以通用驗證失敗拒絕登入）

#### Scenario: Unknown tenant rejected indistinguishably

- **WHEN** a user signs in with an unknown tenant identifier（使用者以未知租戶識別登入）
- **THEN** the system rejects with a failure indistinguishable from wrong-password（系統拒絕，其失敗與密碼錯誤無區別）

### Requirement: Disabled users cannot authenticate

The system SHALL reject sign-in for disabled users and SHALL reject any token refresh presenting a disabled user's session. 系統 SHALL 拒絕已停用使用者的登入，且 SHALL 拒絕出示已停用使用者工作階段的刷新請求。

#### Scenario: Disabled user sign-in rejected

- **WHEN** a disabled user signs in with otherwise valid credentials（已停用使用者以原本有效的憑證登入）
- **THEN** the system rejects the sign-in（系統拒絕登入）

#### Scenario: Disabled user refresh rejected

- **WHEN** a refresh request presents a session belonging to a disabled user（刷新請求出示已停用使用者的工作階段）
- **THEN** the system rejects the refresh and issues no new token（系統拒絕刷新且不簽發新權杖）

### Requirement: Token refresh

Refresh families are issued per sign-in (per device). A valid refresh SHALL issue a new pair and invalidate the old token; replay within the reuse window SHALL return the current pair; replay beyond it SHALL revoke the family. Disabling or credential change SHALL revoke all families; expired or unknown tokens are rejected. 刷新家族按次登入簽發（按裝置獨立）；有效刷新換新配對並作廢舊權杖；窗內重放回傳現行配對，窗外重放撤銷家族；停用或改憑證撤銷全家族；逾期或未知拒絕。

#### Scenario: Refresh succeeds

- **WHEN** a caller presents a valid unexpired refresh token（呼叫者出示有效且未逾期的刷新權杖）
- **THEN** the system returns a new access token for the same user and tenant scope（系統回傳同一使用者與租戶範圍的新存取權杖）

#### Scenario: Replay within window returns current pair

- **WHEN** a caller replays an invalidated token within the reuse window, e.g. on network retry（呼叫者於寬限窗內重放已作廢權杖，例如網路重試）
- **THEN** the system returns the current pair without revocation（系統回傳現行配對且不撤銷）

#### Scenario: Replay beyond window revokes family

- **WHEN** a caller replays an invalidated token beyond the reuse window（呼叫者於寬限窗外重放已作廢權杖）
- **THEN** the system revokes the token family including the current pair（系統撤銷權杖家族，含現行配對）

#### Scenario: Expired refresh rejected

- **WHEN** a caller presents an expired or unknown refresh token（呼叫者出示逾期或未知的刷新權杖）
- **THEN** the system rejects the request and issues nothing（系統拒絕請求且不簽發任何權杖）
