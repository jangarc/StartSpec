# Tasks

## 1. 方案鷹架（Solution scaffolding）

- [ ] 1.1 建立 .NET 10 方案：`Modules/Iam/{Domain,Application,Infrastructure}`＋`ArchTests`（L2 單專案）、`Modules/SharedKernel`，外層 `Hosts.HQ`、`Hosts.Store`、`Hosts.Consumer`、`HostBase`、`Hosts.Migrator`、`Hosts.Dev`，分層由 NetArchTest 強制，驗證：`dotnet build` 零警告＋架構測試通過
- [ ] 1.2 加入 EF Core + Npgsql + Reqnroll + Testcontainers + NetArchTest 套件並還原，驗證：`dotnet restore` 成功且套件清單已鎖定

## 2. 租戶能力（`specs/identity/tenant`）

- [ ] 2.1 以 TDD 實作租戶 Domain（`Tenant`、`Organization`、`TenantProvisioned` 事件）與單元測試，驗證：租戶單元測試通過
- [ ] 2.2 實作租戶建立/解析 application ports、EF 實體、`tenant_id` 全域過濾與初始 migration，驗證：migration 可乾淨套用至 Testcontainers Postgres
- [ ] 2.3 新增過濾守衛測試（守衛先行），斷言每條 IAM 查詢皆含 `tenant_id`（Party 身份庫除外）且跨租戶拒絕與不存在回應一致，驗證：守衛框架就位，後續查詢出生即被斷言；移除任一過濾即紅燈、恢復即綠燈
- [ ] 2.4 開放租戶 API 端點，Reqnroll feature 覆蓋全部 8 個租戶場景，驗證：`dotnet test --filter Tenant` 綠燈（含守衛全程通過）

## 3. Party 能力（`specs/identity/party`）

- [ ] 3.1 以 TDD 實作 Party Domain（`Party`、`PartyRole`、精確識別元組、同意記錄）與單元測試（新建/精確命中綁定/近似新建/禁模糊合併），驗證：Party 單元測試通過
- [ ] 3.2 實作平台級 Party 儲存（無 `tenant_id`、守衛存取：禁列出與遍歷、僅精確比對）與 migration，驗證：遍歷查詢測試拒絕、精確比對命中成功
- [ ] 3.3 實作同意授權 append-only 時間軸，Reqnroll party feature 覆蓋全部 9 個 Party 場景，驗證：`dotnet test --filter Party` 綠燈
- [ ] 3.4 將使用者建立接線 Party（命中綁定/否則新建），驗證：重複證件綁定測試通過且不產生重複 Party

## 4. 使用者驗證能力（`specs/identity/user-auth`）

- [ ] 4.1 實作使用者 Domain（`User`、啟用/停用生命週期、`UserProvisioned`/`UserDisabled` 事件）與單元測試，驗證：使用者單元測試通過
- [ ] 4.2 實作 Argon2id 憑證服務與登入應用（`{tenant_id, login_name, password}`，四種失敗同一訊息/狀態碼），錯誤憑證一律通用失敗，驗證：登入單元測試通過（含無使用者枚舉、未知租戶無區別、計時歸一化測試）
- [ ] 4.3 實作短效 JWT 存取 + 按裝置刷新家族 + 寬限窗重放（窗內回現行配對、窗外撤銷家族、停用撤銷全家族），驗證：刷新整合測試通過（成功、窗內重放、窗外撤銷、逾期拒絕、雙裝置互不干擾）
- [ ] 4.4 開放驗證 API 端點，Reqnroll feature 覆蓋全部 12 個 user-auth 場景，驗證：`dotnet test --filter UserAuth` 綠燈
- [ ] 4.5 新增冪等 seeder（初始租戶 + 操作員），驗證：連跑兩次，第二次零新增

## 5. 授權能力（`specs/identity/authorization`）

- [ ] 5.1 以 TDD 實作角色/授權 Domain 與租戶級判定（含允許/拒絕/跨租戶矩陣），驗證：判定單元測試通過
- [ ] 5.2 實作指派應用、判定 port 與 API，Reqnroll feature 覆蓋全部 6 個授權場景，驗證：`dotnet test --filter Authorization` 綠燈
- [ ] 5.3 在 `AuthorizationOptions` 之下實作 policy-watch 撤銷傳播（測試覆寫上限、in-memory 傳輸），傳播測試撤銷角色並斷言所有節點拒絕，驗證：次秒級測試上限下傳播測試通過

## 6. 領域事件接縫（Domain events seam）

- [ ] 6.1 實作 `IEventBus`（傳輸可換為 broker，預設 in-memory 投遞），發佈 `TenantProvisioned`/`UserProvisioned`/`UserDisabled`/`RoleRevoked`，驗證：訂閱測試收到全部四個事件

## 7. 前端分層（Frontend layers）

- [ ] 7.1 搭建 Vite + Vue 3 + Tailwind，含 `domain/`、`application/ports/`、`adapters/vue/`、`theme/tokens.css`（Tailwind 僅映射 tokens），驗證：`npm run build` 成功
- [ ] 7.2 實作純 TS domain（工作階段、租戶範圍、權限判定）與 vitest 單元測試，零 `vue`/`tailwind` 引入，驗證：`npm run test` 通過
- [ ] 7.3 實作 `AuthPort`/`HttpPort` 轉接器與 Vue 登入/管理畫面，驅動租戶、user-auth、授權三條流程，驗證：每個畫面對本地 API 走完流程
- [ ] 7.4 新增 import lint 規則，`domain`/`application` 出現 `vue`/`tailwind` 引入即建置失敗，驗證：植入違規即紅燈、移除即綠燈

## 8. 端到端驗證（End-to-end verification，僅整合檢查）

- [ ] 8.1 對乾淨 Postgres 執行完整 Reqnroll 套件（全部 35 個 spec 場景），驗證：`dotnet test` 全綠
- [ ] 8.2 對本變更執行 review-work 閘門，逐條處理或記錄發現，驗證：審查報告附於變更且零開放 blocker

## Workflow follow-up

- review 通過後以 `openspec archive iam-foundation` 歸檔，再驗證主規格已納入四個 identity capabilities。
