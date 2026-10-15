# Design

## Context

第二個 Bounded Context（`iam-foundation` 規劃完成未歸檔；本變更假設其 `identity/*` 能力先落地，Party 角色有效期擴充隨本變更對接）。動機見 `proposal.md`；行為契約：`specs/store/{provisioning,staffing}/spec.md`。沿用專案 `context`（.NET 10、模組化單體、Reqnroll、前端分層、Postgres 共享庫）。

C4 Container 圖（c4-diagram skill 分類法）：可編輯原始檔見 `c4_store_onboarding.drawio`。

```
+------------------------------------------------------------------+
|  [Platform Operator]          [HQ Manager / Staff]               |
|      | manages                           | signs in / manages   |
|      v                                   v                       |
|  +------------------------------------------------------------+  |
|  | Enterprise Cloud Platform [Software System]                |  |
|  |                                                            |  |
|  |  [SPA] ---> [IAM API]      [Store API] ---> [Postgres]     |  |
|  |   Vue        C#/NET 10      C#/NET 10 (new)   tenant+store |  |
|  |     \            \              ||                         |  |
|  |      \            \      events [AMQP, async]               |  |
|  |       \            v            v                            |  |
|  |        +----> [Domain Event Bus]                           |  |
|  +------------------------------------------------------------+  |
|                        |               |                         |
|                        v               v                         v
|              (TenantProvisioned)  (StoreOpened)      [Email Service]
|                                                     (External, SMTP)
+------------------------------------------------------------------+
```

## Goals / Non-Goals

**Goals（目標）：**

- 品牌租戶內開店完整鏈（邀請→證件→審核→建店＋店長），店為租戶內實體（N）。
- 隔離升為 `tenant_id` x `store_id` 二維，守衛先行（H1 前例）。
- 一人多店角色＋有效期懶惰判定（N2＋E1），調店即加減角色。

**Non-Goals（非目標）：**

- 總部合併報表、庫存調撥、排班/打卡、subdomain 租戶路由。
- Party 角色有效期的主規格修改（屬 `iam-foundation` 對接，記為假設）。

## Decisions

### 1. Store as tenant-internal entity（店為租戶內實體，N）

`stores` 表攜 `tenant_id` + `store_id`；所有店級查詢疊加 `store_id` 過濾；守衛測試先於店查詢存在（H1）。替代方案（店＝租戶）否決：總部視角與跨店聚合將在授權上打洞，違背 G2。

### 2. Invitation plus review（邀請＋審核）

平台方簽發一次性邀請 token，效期收進 Options（`InvitationTtlHours`，預設 72 小時）；接受須備齊證件；逾期/重用一律通用失敗。核准建店＋店長帳號（綁定 Party 精確命中或新建）；駁回記理由。通知通道首期僅 Email（`Email Service`），簡訊為後續傳輸選項。

### 3. Staffing: multi-store roles, lazy validity（多店角色，有效期懶惰判定）

同一 Party 可持多店角色；角色帶 `valid_from/until`，判定時比對當下時間（E1），無掃蕩任務。調店＝加角色＋撤角色，各自留痕。總部管理角色（`hq-manager`）為租戶級，天然跨店；店級角色僅見所屬店。

### 4. Enforcer domain format（執行器 domain 格式）

casbin domain 取複合值 `t:{tenant_id}/s:{store_id}`；`hq-manager` 授權掛 `t:{tenant_id}` 租戶級 domain，判定時租戶級授予覆蓋店級。店級拒絕沿用統一 404＋游標分頁（G2）。

### 5. Event integration（事件整合）

Store 模組訂閱 `TenantProvisioned`（品牌租戶就緒方可開店）、發佈 `StoreOpened`；下游消費者端將來訂閱後者。傳輸沿用 `IEventBus`（預設 in-memory）。

### 6. Hosts layering applies（沿用 Hosts 分層模型）

Store 模組同為 L2（`Modules/Store/{Domain,Application,Infrastructure}`＋`ArchTests`，無 Api）；店端點經相關 Hosts（首期 `Hosts.Store`＋`Hosts.HQ`）暴露，遵守三護欄與 G1–G4（Contracts 歸 Application、判定只在 Application、驗證分層、營運三件）；migration 由 `Hosts.Migrator` 統一執行。

## Risks / Trade-offs

- [Risk] 邀請 token 失竊 -> 緩解：一次性＋短 TTL（Options 可調）＋通用失敗。
- [Risk] `hq-manager` 權限過廣 -> 緩解：租戶級授予獨立審查；review-work 閘門。
- [Risk] `store_id` 過濾繞過 -> 緩解：守衛先行測試（H1），每條店查詢出生即被斷言。
- [Accepted] `iam-foundation` 未歸檔的疊加風險 -> 接受：本變更僅新增 `store/*` 能力；Party 有效期對接記為假設，歸檔順序保證地基先行。

## Migration Plan

新增 `Modules/Store` 與 migration（`stores`、`store_roles`、邀請與審核記錄）；部署順序接 IAM 之後；回滾：移除 Store migration＋下線模組，不碰 IAM 表。

## Open Questions

- 通知通道綁定（簡訊）：延至消費者端再議，不擋本期。
- 店員初始角色模板： propose 期以單一 `cashier`＋`manager` 模板假設先行。
