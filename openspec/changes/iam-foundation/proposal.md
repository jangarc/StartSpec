# Proposal（提案）

## Why（為何）

企業平台在店家、消費者、醫院三個上下文開工前，需要單一信任錨點。若無租戶隔離加統一身份，每個下游限界上下文將各自重造驗證，並承擔跨租戶洩漏風險。先立 IAM 地基，全部垂直切片隨之解鎖。

## What Changes（變更內容）

- 引入租戶 + 組織模型，以 `tenant_id` 列級隔離為所有未來模組的預設。
- 引入使用者生命週期（建立、停用）與憑證驗證，簽發短效權杖加刷新流程。
- 引入角色/權限判定（角色指派、權限檢查 API、跨租戶拒絕），經整合 ports/events 取用。
- 搭建模組化單體分層（L2：模組內分層為資料夾＋`ArchTests`）與外層 `Hosts.*`（按組織單位組裝，僅引用 Application ports）；跨模組禁止直接存取資料庫。
- 搭建 `AuthPort` 之後的 token 前端工作階段處理；Vue adapters 暫無商業邏輯。
- 引入平台級 Party 身份模型（以精確識別元組全域去重，嚴禁模糊合併），地址/連絡人掛 Party，角色按租戶扮演；同意/授權記錄為角色上的 append-only 時間軸。
- **BREAKING**：無（綠地，`src/` 為空，無既有 API 消費者）。

## Capabilities（能力）

### New Capabilities（新能力）

- `identity/tenant`：租戶與組織建立、請求級租戶解析、隔離保證。
- `identity/user-auth`：使用者建立/生命週期、憑證驗證、工作階段/權杖簽發與刷新。
- `identity/authorization`：角色指派、權限判定、跨租戶拒絕、撤銷傳播上限。
- `identity/party`：自然人/組織方、嚴格去重、地址/連絡人、租戶內角色扮演、同意授權時間軸。

### Modified Capabilities（修改既有能力）

- 無（主規格為空）。

## Impact（影響）

- 新增 .NET 10 方案：`Modules/Iam/{Domain,Application,Infrastructure}`＋`ArchTests`、`SharedKernel`，外層 `Hosts.*`＋`HostBase`＋`Hosts.Migrator`；EF Core + Npgsql migrations；Reqnroll BDD 線束。
- 新增前端分層：`domain/`、`application/ports/`、`adapters/vue/`、`theme/tokens.css`；Tailwind 僅映射 tokens。
- 新增 Postgres schema（租戶、使用者、角色）；須加全域查詢過濾；Party 身份庫為平台級守衛例外。
- `user-auth` 建立改為綁定 Party（命中既有或新建）。
- 範圍之外：醫院合規細則、消費者讀取擴展優化、微服務抽離、本地簽發器以外的 OIDC 聯邦。
