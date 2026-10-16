# Tasks

## 1. 方案鷹架與事件接線（Solution scaffolding）

- [ ] 1.1 建立 `Modules/Store/{Domain,Application,Infrastructure}`＋`ArchTests`（L2，無 Api）並掛入 `Hosts.Store`＋`Hosts.HQ`（三護欄＋G1–G4），驗證：`dotnet build` 零警告＋架構測試通過
- [ ] 1.2 訂閱 `TenantProvisioned`（品牌租戶就緒方可開店），驗證：發佈測試事件後 Store 收到
- [ ] 1.3 新增 `store_id` 守衛測試（守衛先行，H1）：斷言店級查詢皆含 `tenant_id`＋`store_id`，驗證：守衛框架就位，後續查詢出生即被斷言

## 2. 開店鏈（`specs/store/provisioning`）

- [ ] 2.1 以 TDD 實作邀請 Domain（一次性 token、效期 `InvitationTtlHours`、接受/逾期狀態機）與單元測試，驗證：邀請單元測試通過
- [ ] 2.2 實作接受/證件提交/審核應用與 migration（邀請、審核記錄），核准建店＋店長帳號（綁定 Party），驗證：migration 乾淨套用
- [ ] 2.3 開放開店 API 端點，Reqnroll feature 覆蓋全部 5 個 provisioning 場景，驗證：`dotnet test --filter Provisioning` 綠燈（含守衛全程通過）

## 3. 多店人事（`specs/store/staffing`）

- [ ] 3.1 以 TDD 實作多店角色 Domain（多店兼任、有效期懶惰判定、調店加減）與單元測試，驗證：人事單元測試通過
- [ ] 3.2 實作總部跨店角色（租戶級授予）與店級隔離判定，Reqnroll staffing feature 覆蓋全部 5 個場景，驗證：`dotnet test --filter Staffing` 綠燈

## 4. 前端畫面（Frontend views，`apps/store` 殼）

- [ ] 4.1 以共用 `components`（props/events、無 store）組裝 `apps/store` 邀請管理、審核、店員管理三組 Vue 畫面（經既有 ports），驗證：開店鏈端到端走完（邀請→接受→核准→登入）
- [ ] 4.2 新畫面與元件通過接縫 lint（共用包無框架引入、樣式唯 tokens、元件無 store 存取、無直調瀏覽器/原生 API），驗證：全組 lint 綠燈

## 5. 端到端驗證（End-to-end verification，僅整合檢查）

- [ ] 5.1 對乾淨 Postgres 執行完整 Reqnroll 套件（全部 10 個 spec 場景），驗證：`dotnet test` 全綠
- [ ] 5.2 對本變更執行 review-work 閘門，驗證：審查報告附於變更且零開放 blocker

## Workflow follow-up

- `iam-foundation` 歸檔後對接 Party 角色有效期（屆時 MODIFIED `identity/party`）；再以 `openspec archive store-onboarding` 歸檔。
