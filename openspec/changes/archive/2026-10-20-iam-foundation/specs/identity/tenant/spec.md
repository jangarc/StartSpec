# Spec Delta

## Purpose

Defines tenant and organization provisioning plus request-scoped isolation guarantees that all downstream business contexts rely on for data separation. 定義租戶與組織的建立，以及請求級隔離保證，供所有下游業務上下文作為資料分離的依據。

## ADDED Requirements

### Requirement: Tenant provisioning

The system SHALL provision a new tenant with a unique identifier and an initial organization record. Duplicate tenant identifiers SHALL be rejected. 系統 SHALL 以唯一識別建立新租戶及其初始組織；重複識別 SHALL 拒絕。

#### Scenario: Provision new tenant

- **WHEN** an operator provisions a tenant with a unique identifier（操作員以未使用的識別建立租戶）
- **THEN** the system creates the tenant with its initial organization and returns the tenant identifier（系統建立租戶與初始組織並回傳識別）

#### Scenario: Duplicate tenant rejected

- **WHEN** an operator provisions a tenant with an identifier that already exists（操作員以已存在的識別建立租戶）
- **THEN** the system rejects the request with a conflict error and creates nothing（系統以衝突錯誤拒絕，且不建立任何資料）

### Requirement: Tenant resolution from request scope

The system SHALL resolve the active tenant from the authenticated request scope for every operation. Requests without a resolvable tenant SHALL be rejected before any business logic runs. 系統 SHALL 自每次操作的身分驗證請求範圍解析作用中租戶；無法解析租戶的請求 SHALL 在商業邏輯執行前拒絕。

#### Scenario: Resolve active tenant

- **WHEN** an authenticated request carries a valid tenant scope（身分驗證請求帶有有效租戶範圍）
- **THEN** the system resolves that tenant as the active tenant for the operation（系統將該租戶解析為本次操作的作用中租戶）

#### Scenario: Missing tenant scope rejected

- **WHEN** a request carries no resolvable tenant scope（請求未帶可解析的租戶範圍）
- **THEN** the system rejects the request without executing business logic（系統拒絕請求且不執行商業邏輯）

### Requirement: Cross-tenant isolation

The system SHALL return only data belonging to the active tenant. Any read or write targeting another tenant's data SHALL be denied, and list operations SHALL exclude other tenants' records. The platform-scoped Party identity store (see `identity/party`) is exempt: reachable only by exact identifier match, never listable or traversable. 系統 SHALL 僅回傳屬於作用中租戶的資料；存取他租戶資料的讀寫 SHALL 拒絕，清單查詢 SHALL 排除他租戶紀錄。平台級 Party 身份庫（見 `identity/party`）為例外：僅精確比對可達，絕不可列出或遍歷。

#### Scenario: Cross-tenant read denied

- **WHEN** a caller scoped to tenant A requests a record owned by tenant B（租戶 A 範圍的呼叫者請求租戶 B 的紀錄）
- **THEN** the system denies access as if the record does not exist（系統視同紀錄不存在而拒絕存取）

#### Scenario: List excludes other tenants

- **WHEN** a caller scoped to tenant A lists organization records（租戶 A 範圍的呼叫者列出組織紀錄）
- **THEN** the system returns only records owned by tenant A（系統僅回傳屬於租戶 A 的紀錄）

### Requirement: Uniform denial responses and cursor pagination

Denials for cross-tenant access SHALL use responses identical to not-found through the same code path. List operations SHALL be cursor-based with a has-next indicator and SHALL NOT expose exact totals. 跨租戶拒絕 SHALL 經同一程式碼路徑回傳與不存在相同的回應；清單 SHALL 為游標式（有下一頁），不得暴露精確總數。

#### Scenario: Cross-tenant read looks like not-found

- **WHEN** a caller scoped to tenant A requests a record owned by tenant B（租戶 A 範圍的呼叫者請求租戶 B 的紀錄）
- **THEN** the system responds exactly as if the record does not exist（系統回應與紀錄不存在完全相同）

#### Scenario: Lists expose no totals

- **WHEN** a caller pages through organization records（呼叫者翻閱組織紀錄）
- **THEN** the system returns a has-next indicator without exact totals（系統回傳有下一頁指示，不含精確總數）
