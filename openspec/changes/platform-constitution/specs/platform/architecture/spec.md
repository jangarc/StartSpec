# Spec Delta

## Purpose

Establishes the platform layering constitution as an archivable, referenceable contract: L2 modules, per-org-unit Hosts, guardrails, and ownership rules, each verifiable by architecture tests. 將平台分層憲法立為可歸檔、可引用的契約：L2 模組、按組織單位 Hosts、護欄與歸屬規則，每條皆可由架構測試驗證。

## ADDED Requirements

### Requirement: L2 single-project modules

Each module SHALL be a single project with `Domain`, `Application`, `Infrastructure` folders plus `ArchTests`; no Api folder inside modules. Layering violations SHALL fail the build via NetArchTest. 每模組 SHALL 為單專案（含 `Domain`、`Application`、`Infrastructure` 資料夾＋`ArchTests`），模組內無 Api；分層違規 SHALL 由 NetArchTest 使建置失敗。

#### Scenario: Layering enforced

- **WHEN** the architecture test suite runs（架構測試套件執行）
- **THEN** any forbidden layer reference fails the build（任何禁用的分層引用皆使建置失敗）

### Requirement: Hosts compose per org unit

API surfaces SHALL be composed per org unit by outer `Hosts.*` sharing module libraries; Hosts SHALL reference Application ports and DTOs only, never Domain entities or Infrastructure. API 面 SHALL 由外層 `Hosts.*` 按組織單位組裝並共用模組庫；Hosts SHALL 僅引用 Application ports 與 DTO，絕不碰 Domain 實體或 Infrastructure。

#### Scenario: Host references guarded

- **WHEN** the architecture test suite runs（架構測試套件執行）
- **THEN** any Host reference to Domain or Infrastructure fails the build（Host 對 Domain 或 Infrastructure 的任何引用皆使建置失敗）

### Requirement: Single canonical exposure

Each use case SHALL have a single canonical exposure; additional Hosts SHALL reuse the same Application handler with thin adaptation and no logic. 每 use case SHALL 僅一個 canonical 暴露；其他 Host SHALL 複用同一 Application handler 做薄轉接，不得寫邏輯。

#### Scenario: No logic in hosts

- **WHEN** reviewers inspect a second Host exposure（審查者檢視第二個 Host 暴露）
- **THEN** it delegates to the canonical handler with mapping only（其僅委派 canonical handler 做映射）

### Requirement: Ownership of contracts, decisions, validation

Response contracts SHALL belong to Application; authorization decisions SHALL live in Application with Hosts carrying identity only; format validation SHALL live in Hosts and business-rule validation in Application. Response contract SHALL 歸 Application；授權判定 SHALL 住 Application（Hosts 只搬身份）；格式驗證在 Hosts，業務規則驗證在 Application。

#### Scenario: Ownership reviewable

- **WHEN** reviewers inspect contracts, decisions, and validation placement（審查者檢視契約、判定與驗證位置）
- **THEN** each sits in its owned layer per this requirement（各居其所屬分層）

### Requirement: Guard tests precede queries

Guard tests for isolation and layering SHALL exist before the queries they guard. 隔離與分層的守衛測試 SHALL 先於被守衛的查詢存在。

#### Scenario: Guard-first order

- **WHEN** reviewers inspect task ordering（審查者檢視任務順序）
- **THEN** guard tasks precede the implementation tasks they guard（守衛任務先於被守衛的實作任務）

### Requirement: Single migrator

`Hosts.Migrator` SHALL be the sole migration runner with fixed order and idempotency; online Hosts SHALL never auto-migrate; per-Host migration is forbidden. `Hosts.Migrator` SHALL 為唯一遷移執行者（固定順序、冪等）；線上 Host SHALL 絕不自動 migrate；禁止按 Host 分遷移。

#### Scenario: No auto-migration in hosts

- **WHEN** reviewers inspect Host startup configuration（審查者檢視 Host 啟動配置）
- **THEN** no online Host runs migrations（線上 Host 皆不執行遷移）

### Requirement: Additive-only contract evolution

Application ports, domain events, and REST/OpenAPI contracts SHALL evolve additively only (new fields, new events); existing fields and semantics SHALL NOT be altered or removed without a major version. 契約（Application ports、領域事件、REST/OpenAPI）SHALL 僅加法演進（加欄位、加事件）；既有欄位與語意非經 major 版本 SHALL 不得修改或刪除。

#### Scenario: Additive change passes

- **WHEN** a change adds a field or event without altering existing ones（變更僅加欄位或事件，未動既有）
- **THEN** contract tests pass with no consumer updates（契約測試通過，消費者無需修改）

#### Scenario: Breaking change flagged

- **WHEN** a change alters or removes an existing contract element（變更修改或刪除既有契約元素）
- **THEN** it is marked major with migration notes and consumer updates（其標記為 major 並附遷移說明與消費者更新）

### Requirement: No framework leakage into shared packages

Shared frontend packages (`domain`, `application`, `components`) SHALL NOT import framework modules; port signatures SHALL NOT contain framework types. Unit tests for `domain`/`application` SHALL run in pure Node without DOM. 共用前端 packages SHALL 不引入框架模組；port 簽名 SHALL 不含框架型別；`domain`/`application` 單元測試 SHALL 跑在純 Node 無 DOM。

#### Scenario: Leakage fails build

- **WHEN** a shared package imports a framework module or a port exposes a framework type（共用包引入框架模組或 port 暴露框架型別）
- **THEN** lint fails the build（lint 使建置失敗）

#### Scenario: Pure Node tests

- **WHEN** `domain`/`application` unit tests run（執行單元測試）
- **THEN** they pass without DOM or jsdom（無 DOM 即可通過）

### Requirement: Tokens-only styling

Component styling SHALL consume design tokens only; raw values outside tokens and non-token `@apply` are forbidden. 元件樣式 SHALL 僅取用 design tokens；tokens 外裸值與非 token `@apply` 禁止。

#### Scenario: Raw value rejected

- **WHEN** a component uses a raw color or spacing value（元件使用裸色碼或間距值）
- **THEN** stylelint fails the build（stylelint 使建置失敗）

### Requirement: Packaging-agnostic shared packages

Shared packages SHALL NOT call browser-only or native APIs directly; all platform capabilities (storage, device, shell) SHALL go through application ports. 共用包 SHALL 不得直調瀏覽器或原生 API；平台能力一律經 application ports。

#### Scenario: Direct platform call rejected

- **WHEN** a shared package calls a browser or native API directly（共用包直調瀏覽器或原生 API）
- **THEN** lint fails the build（lint 使建置失敗）

### Requirement: Declared packaging targets per app

Each app SHALL declare its packaging targets (Web, Web+Tauri+native); Web ships first and adding a Tauri shell SHALL NOT modify shared packages. 各應用 SHALL 宣告封裝目標；Web 先行；加 Tauri 殼 SHALL 不動共用包。

#### Scenario: Tauri shell touches apps only

- **WHEN** a Tauri shell is added to an app（為應用加上 Tauri 殼）
- **THEN** no shared package source changes（共用包源碼零修改）

### Requirement: SharedKernel admission rule

`SharedKernel` SHALL admit only domain-event bases, kernel value objects used by three or more modules (3-use promotion with ADR record), and cross-cutting port interfaces; business entities, DTOs, and use-case logic are forbidden. `SharedKernel` SHALL 僅收領域事件基底、三模組以上共用的核心值物件（3-use 晉升＋ADR 記錄）、橫切埠介面；業務實體、DTO、用例邏輯禁止進入。

#### Scenario: Creep rejected

- **WHEN** a proposed addition is a business entity or single-module helper（擬加入物為業務實體或單模組幫手）
- **THEN** review rejects it into the owning module（審查將其退回所屬模組）

### Requirement: Event envelope and V1 versioning

Every domain event SHALL carry the standard envelope (`event_id`, `occurred_at`, `tenant_id`, `correlation_id`, `causation_id`, `version`, `payload`) and evolve additively; deployments SHALL be lockstep across Hosts until broker persistence or service split triggers full versioning. 每個領域事件 SHALL 帶標準信封並加法演進；broker 持久化或服務拆分之前，部署 SHALL 跨 Hosts 鎖步。

#### Scenario: Envelope complete

- **WHEN** a domain event is published（發佈領域事件）
- **THEN** it carries the full standard envelope（其帶完整標準信封）
