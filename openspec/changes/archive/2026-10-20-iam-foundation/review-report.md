# Review Report（review-work gate，task 8.2）

# Review Report — iam-foundation gate review

- **審查對象**：本變更全部交付（src 後端、src/tests 四套件、frontend/、scripts/Add-IamMigration.ps1、AGENTS.md 更新）；git 零提交，工作樹即變更集。
- **方法**：lead 親自手動 QA（真實表面＋四套件回歸）→ 單一 gate reviewer（oracle）全程審查 → REJECT → 修復 → **fresh reviewer delta 重審 → APPROVE**。

## Verdict History

| Round | Verdict | Confidence | 開放 blocker |
|---|---|---|---|
| 首審 | REJECT | HIGH | 1 |
| 修復後重審（delta） | **APPROVE** | **HIGH** | **0** |

## 首審 Blocker（已關閉）

**IAM API 零請求驗證**：租戶範圍僅由可偽造的 `X-Tenant-Id` 標頭解析；簽發的 JWT 無任何 Host 驗證、前端從未送出。違反 design 決策 2（絕不單獨信任客戶端自帶標頭）、tenant spec「authenticated request scope」、中介器 docstring 對 4.x 的承諾。

**修復（claims-based 全鏈）**：
- `SharedKernel/Ports/ISessionInspector.cs`（ADR-007 橫切 port＋seedwork fake 放 ContractSeedwork/Fakes）
- `Iam.UnitTests/Session/SessionInspectorContractTests.cs`：4 鎖定事實（新簽回身分／竄改 null／逾期 null／殘缺 null）× fake＋wired 兩腿
- `Iam.Infrastructure/Persistence/JwtTokenInspector.cs`：HMAC-SHA256＋`ValidAlgorithms` 固定＋效期（30s skew）；claims-only 身分；任何失敗一律 null
- `TenantScopeMiddleware` 重寫：`/iam/*` 除 sign-in／refresh 一律 Bearer；範圍僅自驗證 claims；缺失與無效共用單一 401 本體 `tenant_scope_missing`；`TenantHeaderName` 常數與所有標頭綁定全數退役（全 repo grep 歸零）
- HQ endpoints 改注入 `TenantScope`；前端 `httpFetchPort` 送 `Authorization: Bearer`（claims 範圍）；Reqnroll 新輔助 `CallerAuth`（部署路徑建操作員＋真實 HTTP 登入），四份步驟檔改 Bearer 轂送（**.feature 契約文字不變**）

## QA 矩陣（最終樹、本 session 證據）

| # | 說明 | 結果 | 證據 |
|---|---|---|---|
| 1 | `npm run build` 三 app | exit=0 | build 輸出 |
| 2 | `npm run test`（domain＋components＋stylesPurity） | 20/20 | vitest 輸出 |
| 3 | 接縫 lint red→green（五種子） | 5 errors 紅→0 綠，另抓到 1 個真違規並修 | lint 輸出 |
| 4 | 真實 UI 三流程（vite :5174→HQ :8787→PG :55432） | sign-in 200→refresh 200→tenants 201→organizations 200（僅自家租戶組織）→users 201→roles 201→assignments 201→evaluations allowed=true→DELETE 204→傳播後 allowed=false | `iam74-8.2-fix-flows-final.png`＋network requests＋a11y 快照 |
| 5 | `dotnet test` 四套件（乾淨 Testcontainers PG） | Unit **85**／Arch **10**／Contract **59**／Reqnroll **38**（35 spec 場景＋3 wired）全 exit=0 | dotnet 輸出 |
| 6 | Migration 腳本＋idempotent seeder | DryRun 兩處一致；seed 重跑零新增 | 腳本輸出＋IamSeederTests |

`openspec validate iam-foundation` → valid（兩回合審查者各自重現）。

## 已處理的 review notes

- 停用改為**積極清掃全家族**（關閉 reuse-window replay 洞）＋新單元事實驗證。
- `PolicyEnforcer` 改 **per-tenant 快照撕除**（修正 factually wrong 的「RoleRevoked 不帶 tenant id」註解；envelope TenantId 已由聚合填入）。
- 組織清單 endpoint 補 `cursor` pass-through（hasNext 可操作）。
- tenant spec 簡體字 typo（為例外）；TenantFeatureSteps 過時 docstring 更新。

## 開放（非 blocker）Notes——記錄待 production 前

1. Argon2id 參數目前 8 MiB/t=3/p=2，低於 OWASP 建議基線——測試速度考量，production 前調整。
2. Dev 簽署金鑰放在 base appsettings.json（已標 dev-only），production 部署必須覆寫。
3. `version.json`（單一版本真相）平台級義務，非本變更任務——首次提交前補。
4. repo root 殘留：`openapi.yaml`（工具產物）與證據 PNG——首次提交不納入。
5. `ValidateIssuer`/`ValidateAudience` 目前 false——單簽章者切片可接受；出現第二簽章者/外部 audience 時需固定。

## 整體裁定：REVIEW PASSED（零開放 blocker，26/26 可收口；archive 另行指示）
