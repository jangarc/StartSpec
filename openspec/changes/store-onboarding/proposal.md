# Proposal（提案）

## Why（為何）

IAM 地基只解決「人與權限」，尚未解決「店」——品牌無法開店、指派店員，平台就沒有可賣的對象。店家 onboarding 是 B2B2C 鏈的第一環，也是 `TenantProvisioned` 事件的第一個真實訂閱者；沒有它，地基的事件匯流排永遠空轉。

## What Changes（變更內容）

- 引入品牌＝租戶、店＝租戶內實體模型（N），`store_id` 為租戶內第二隔離維度；單店先行，總部合併報表與庫存調撥為非目標。
- 引入邀請＋審核開店鏈：平台方發邀請（含 token 與效期）→ 店家接受並提交證件/營登 → 平台審核（通過/駁回）→ 建店實體＋店長帳號（綁定 Party）。
- 引入多店角色與調店：一人可持多店角色，有效期（`valid_from/until`）懶惰判定（E1）；調店＝加減角色；排班/打卡 defer。
- 假設：`iam-foundation` 的 `identity/*`（租戶、Party、授權、撤銷傳播）先歸檔；Party 角色有效期擴充隨本變更對接（屆時以 MODIFIED 補 `identity/party`）。
- **BREAKING**：無（新 Bounded Context，不碰既有行為）。

## Capabilities（能力）

### New Capabilities（新能力）

- `store/provisioning`：邀請簽發/接受/逾期、證件審核、開店建實體與店長帳號。
- `store/staffing`：多店角色指派、有效期懶惰判定、調店加減角色、總部跨店角色。

### Modified Capabilities（修改既有能力）

- 無（主規格為空；對 `iam-foundation` delta 的疊加記為假設，不在此列）。

## Impact（影響）

- 新增 `Modules/Store/{Domain,Application,Infrastructure,Api}`；EF migration（`stores`、`store_roles`、邀請與審核記錄）；訂閱 `TenantProvisioned`。
- `tenant` 守衛與 `authorization` 判定疊加 `store_id` 維度（本變更內實作，不改 `iam-foundation` 產物）。
- 範圍之外：總部合併報表、庫存調撥、排班/打卡、消費者端、醫院合規。
