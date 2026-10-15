# Spec Delta

## Purpose

Defines the platform-scoped Party identity model with strict deduplication, guarded cross-tenant reachability, per-tenant roles, and an append-only consent timeline for all human-related data. 定義平台級 Party 身份模型：嚴格去重、守衛式跨租戶可達、租戶內角色扮演、同意授權 append-only 時間軸，承載所有與人相關的資料彈性。

## ADDED Requirements

### Requirement: Party provisioning with strict deduplication

The system SHALL create a new Party only when no existing Party matches the exact identifier tuple (identifier type + identifier number). Near matches SHALL create a new Party; automatic fuzzy merging is forbidden. 系統僅在無既有 Party 精確命中（證件類型 + 證號）時建立新 Party；近似者 SHALL 新建，禁止自動模糊合併。

#### Scenario: Provision new party

- **WHEN** an operator provisions a Party with an identifier tuple never seen before（操作員以未見過的識別元組建立 Party）
- **THEN** the system creates the Party（系統建立該 Party）

#### Scenario: Exact match binds without duplication

- **WHEN** provisioning presents an identifier tuple that exactly matches an existing Party（建立時出示與既有 Party 精確相符的識別元組）
- **THEN** the system binds to the existing Party and creates no duplicate（系統綁定既有 Party，不產生重複）

#### Scenario: Near match creates new party

- **WHEN** provisioning presents the same name and birthdate but a different identifier number（建立時姓名生日相同但證號不同）
- **THEN** the system creates a new Party instead of merging（系統新建 Party，不合併）

### Requirement: Guarded platform scope

Party identity data SHALL NOT be listable or traversable. Cross-tenant reachability is limited to exact identifier match for provisioning or binding with a stated purpose. Party 身份資料絕不可列出或遍歷；跨租戶僅能以精確識別比對命中以供建立或綁定。

#### Scenario: Traversal denied

- **WHEN** a caller scoped to tenant A attempts to list Parties of tenant B（租戶 A 範圍的呼叫者試圖列出租戶 B 的 Party）
- **THEN** the system denies the request（系統拒絕請求）

#### Scenario: Exact-match bind allowed

- **WHEN** provisioning in tenant A presents an identifier tuple matching a Party from tenant B（租戶 A 建立時出示命中租戶 B Party 的識別元組）
- **THEN** the system binds the new tenant role to that Party without exposing other data（系統將新租戶角色綁定該 Party，不洩漏其他資料）

### Requirement: Roles played per tenant, history preserved

A Party SHALL play zero or more roles per tenant. Deactivating a role or disabling a login SHALL preserve the Party and its history, including consent records. Party SHALL 在每租戶扮演零或多個角色；停用角色或帳號 SHALL 保留 Party 與其歷史（含同意記錄）。

#### Scenario: Deactivation preserves history

- **WHEN** an employee role is deactivated and its login disabled（員工角色停用且帳號停用）
- **THEN** the Party, its identifiers, and its consent history remain intact（Party、識別與同意歷史保持完整）

#### Scenario: One human, multiple roles

- **WHEN** a Party already employed in tenant A is registered as an examinee（租戶 A 在職員工被登記為受檢者）
- **THEN** the system adds the examinee role to the same Party（系統在同一 Party 加掛受檢者角色）

### Requirement: Consent records append-only

Consent and authorization records SHALL be recorded with timestamps and SHALL be immutable; revocation appends a new record and never edits an existing one. 同意與授權記錄 SHALL 具時間戳且不可變；撤銷以新增記錄為之，絕不修改既有記錄。

#### Scenario: Consent recorded

- **WHEN** a Party grants consent for a stated purpose（Party 就特定目的表示同意）
- **THEN** the system stores a timestamped immutable record（系統儲存具時間戳的不可變記錄）

#### Scenario: Revocation appends, original retained

- **WHEN** a Party revokes a previously granted consent（Party 撤銷既往同意）
- **THEN** the system appends a revocation record and retains the original（系統新增撤銷記錄並保留原記錄）
