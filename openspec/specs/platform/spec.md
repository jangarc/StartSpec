# platform Specification

## Purpose

Establishes the platform layering constitution as an archivable, referenceable contract: L2 modules, per-org-unit Hosts, guardrails, and ownership rules, each verifiable by architecture tests. 將平台分層憲法立為可歸檔、可引用的契約：L2 模組、按組織單位 Hosts、護欄與歸屬規則，每條皆可由架構測試驗證。

## Requirements

### Requirement: L2 single-project modules

Each module SHALL be a single project with `Domain`, `Application`, `Infrastructure` folders plus `ArchTests`; no Api folder inside modules. Layering violations SHALL fail the build via NetArchTest. 每模組 SHALL 為單專案（含 `Domain`、`Application`、`Infrastructure` 資料夾＋`ArchTests`），模組內無 Api；分層違規 SHALL 由 NetArchTest 使建置失敗。

#### Scenario: Layering enforced

- **WHEN** the architecture test suite runs（架構測試套件執行）
- **THEN** any forbidden layer reference fails the build（任何禁用的分層引用皆使建置失敗）

### Requirement: Hosts compose per org unit

API surfaces SHALL be composed per org unit by outer `Hosts.*` sharing module libraries; Hosts SHALL reference Application ports and DTOs only, never Domain entities or Infrastructure, except the sanctioned composition entries exposed by module Infrastructure and the Migrator's migration wiring. API 面 SHALL 由外層 `Hosts.*` 按組織單位組裝並共用模組庫；Hosts SHALL 僅引用 Application ports 與 DTO，絕不碰 Domain 實體或 Infrastructure——僅明文豁免：模組 Infrastructure 暴露之組合入口與 `Hosts.Migrator` 之遷移接線。

#### Scenario: Host references guarded

- **WHEN** the architecture test suite runs（架構測試套件執行）
- **THEN** any Host reference to Domain or Infrastructure outside the sanctioned composition entries and the Migrator migration wiring fails the build（Host 對 Domain 或 Infrastructure 的任何引用——組合入口與 Migrator 遷移接線之明文豁免除外——皆使建置失敗）

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

### Requirement: Swappable seam catalog

Four seams SHALL remain publicly swappable: persistence (EF Core/Npgsql), event transport (`IEventBus`), Host and frontend composition, and authentication technology. Each SHALL keep port contract tests, a fake, and an ArchTests assertion; default is contract-kept; swap-proven duties SHALL be paid only when a second implementation exists. SHALL 保持四接縫可替換：persistence、event transport、Host 與前端組裝、認證技術；每接縫 SHALL 維持 contract tests、fake 與 ArchTests 斷言；預設 contract-kept，僅第二實作成真才付 swap-proven 義務。

#### Scenario: Seam guards kept

- **WHEN** a seam's port, fake, or contract suite drifts（接縫的 port、fake 或契約套件漂移）
- **THEN** CI fails the build（CI 使建置失敗）

#### Scenario: No speculative swap-proof

- **WHEN** no second implementation is planned for a seam（接縫尚無第二實作計畫）
- **THEN** only contract-kept duties apply（僅適用 contract-kept 義務）

### Requirement: Swap acceptance and rehearsal

A seam swap SHALL be accepted only when port contract tests pass against the new implementation and the four CI gates are green on the new wiring; the swap SHALL be rehearsed in `Hosts.Dev` with contract and E2E suites green plus a recorded rollback note. A persistence swap SHALL be a major version with schema and data migration notes. 替換 SHALL 僅在新實作 contract tests 全綠且四閘門於新接線全綠時驗收；SHALL 在 `Hosts.Dev` 演練（contract 與 E2E 綠）並附 rollback note；persistence 替換 SHALL 為 major 版本並附 schema 與資料遷移說明。

#### Scenario: Swap rehearsed before rollout

- **WHEN** a seam swap enters rollout（接縫替換進入 rollout）
- **THEN** a `Hosts.Dev` rehearsal exists with contract and E2E suites green plus a rollback note（存在 `Hosts.Dev` 演練，其 contract 與 E2E 套件為綠且附 rollback note）

#### Scenario: Persistence swap flagged major

- **WHEN** the persistence seam is replaced（persistence 接縫被替換）
- **THEN** a major version with schema and data migration notes is declared（宣告 major 版本並附 schema 與資料遷移說明）

### Requirement: Contract seedwork admission

Contract-test base classes and fakes SHALL live in the contract seedwork home. It SHALL admit only test-purpose plumbing — base classes for authoring port contract tests, fakes for ports declared by `SharedKernel`, and shared test fixtures; fakes for module-owned ports SHALL stay in module tests; business logic and production references SHALL NOT enter. Contract seedwork SHALL 只收：port contract tests 基底類、`SharedKernel` 已宣告 port 的 fake、共用測試器具；模組自有 port 的 fake 留在模組測試；業務邏輯與 production 引用禁入。

#### Scenario: Seedwork creep rejected

- **WHEN** a proposed addition is business logic or a module-owned fake（擬加入物為業務邏輯或模組自有 port 的 fake）
- **THEN** review rejects it to its owning module or tests（審查退回所屬模組或其模組測試）

### Requirement: Event delivery contract

The `IEventBus` port contract SHALL specify at-least-once delivery and consumer idempotency as obligations; the in-memory fake SHALL simulate redelivery; port contract scenarios SHALL be derived from spec requirements, not from the first implementation. `IEventBus` 契約 SHALL 規定 at-least-once 傳遞與消費者冪等義務；in-memory fake SHALL 模擬重投遞；契約場景 SHALL 由 spec requirement 派生，不得由第一個實作反推。

#### Scenario: Redelivery processed idempotently

- **WHEN** the same event is delivered twice（同一事件被投遞兩次）
- **THEN** each consumer processes it idempotently（各消費者均冪等處理）

### Requirement: Product repo boundary and kernel baseline

One product SHALL be one repository with own tenant space, `version.json`, and deployment; this repo is product #1 and new products fork from the thin kernel. The thin kernel SHALL evolve on a shared baseline: additive contract surface, fixes via scheduled baseline bumps, and bounded divergence with a CI drift alarm. 每商品 SHALL 一倉（自有 tenant 空間、`version.json`、部署）；本倉為商品 #1，新商品由其 fork。Thin kernel SHALL 走共享基線：契約面僅加法、修復經排程 baseline bump、分歧窗有界＋CI drift alarm。

#### Scenario: Divergence alarmed

- **WHEN** a product repo's kernel lags or diverges beyond the bounded window（商品倉的 kernel 落後或分歧超逾有界窗口）
- **THEN** CI raises a drift alarm（CI 發出 drift alarm）

#### Scenario: Party globality scoped per product

- **WHEN** Party deduplication asserts global uniqueness（Party 去重主張全域唯一）
- **THEN** the boundary is one product repository（其疆界為單一商品倉）

### Requirement: Rule precedence

For architecture contract content the spec SHALL outrank ADRs, and ADRs SHALL outrank AGENTS.md; ADRs SHALL remain normative for process discipline the spec cannot assert; AGENTS.md is an index only. All rules SHALL live in files (specs, ADRs, AGENTS.md, glossary); a rule stated only in conversation SHALL not be authoritative. 架構契約內容 SHALL 按 spec > ADR > AGENTS.md；spec 無法斷言之 process discipline 由 ADR 維持規範性；AGENTS.md 僅為索引。規則 SHALL 一律落檔（specs、ADR、AGENTS.md、glossary）；僅存於對話的規則不具裁決力。

#### Scenario: Conflict resolved by rank

- **WHEN** the spec and an ADR conflict on contract content（spec 與 ADR 就契約內容衝突）
- **THEN** the spec governs（以 spec 為準）

#### Scenario: Rule lives in files

- **WHEN** a rule exists only in a conversation（規則僅存在於對話）
- **THEN** it is not authoritative（其不具裁決力）
