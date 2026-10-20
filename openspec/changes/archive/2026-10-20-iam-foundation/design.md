# Design

## Context

綠地專案（`src/` 為空、尚無主規格），動機見 `proposal.md`。專案 `context` 約束：.NET 10、DDD 模組化單體優先（Modular Monolith first，限界上下文間僅經 ports/events 溝通）、Reqnroll BDD、前端分層（adapters/theme 之外禁止引入 `vue`/`tailwind`）、PostgreSQL 共享資料庫多租戶（以 `tenant_id` 過濾隔離）。行為契約：`specs/identity/{tenant,user-auth,authorization,party}/spec.md`。

C4 Container 圖（c4-diagram skill 分類法，分層 Person -> System -> External）：可編輯原始檔見 `c4_iam_foundation.drawio`。

```
+------------------------------------------------------------------+
|  [Operator]                     [Platform User]                  |
|      |                                   |                       |
|      | manages [HTTPS]                   | signs in [HTTPS]      |
|      v                                   v                       |
|  +------------------------------------------------------------+  |
|  | Enterprise Cloud Platform [Software System]                |  |
|  |                                                            |  |
|  |  [IAM Web App] ---> [IAM API] ---> [IAM Database]         |  |
|  |   Vue 3+Vite         C#/NET 10      PostgreSQL             |  |
|  |        |                ||                                   |  |
|  |        |           events [AMQP, async]                     |  |
|  |        v                v                                    |  |
|  |  (AuthPort session)  [Domain Event Bus]                   |  |
|  +------------------------------------------------------------+  |
|      |                              |                            |
|      v                              v                            v
|  (future UI)              [Downstream Contexts]      [Email Service]
|                            (External, AMQP)           (External, SMTP)
+------------------------------------------------------------------+
```

## Goals / Non-Goals

**Goals（目標）：**

- 單一可部署單體，其 IAM 模組未來可不經 Domain 重寫直接抽離。
- 租戶隔離以構造保證（伺服器端解析 + 查詢過濾 + 測試），而非以約定保證。
- 前端可替換性以建置期 import lint 證明，而非僅以資料夾名稱宣稱。

**Non-Goals（非目標）：**

- 本變更不做微服務拆分，不含 Kubernetes 拓撲。
- 本地簽發器以外的 OIDC 聯邦、醫院專屬合規規則不做。
- 消費者工作負載的讀取擴展優化（快取、唯讀複本）不做。

## Decisions

### 1. Hosts model with L2 module layering（Hosts 模型＋L2 模組分層）

模組內分層為資料夾（L2）：`src/Modules/Iam/{Domain,Application,Infrastructure}`＋`ArchTests`，單專案每模組，分層由 NetArchTest 強制，不以多專案切分（專案爆炸否決）。模組不含 Api；外層 `Hosts.*` 按組織單位組裝（`Hosts.HQ`、`Hosts.Store`、`Hosts.Consumer`…），各自 API 面、各自部署，共用模組庫。

三護欄（ArchTests 斷言，違規建置失敗）：Hosts 僅能引用 Application ports＋DTO，絕不直接碰 Domain 實體與 Infrastructure；每 use case 單一 canonical 暴露（第二 Host 薄轉接，不寫邏輯）；橫切（驗證、租戶解析、ProblemDetails、遙測）只進共用 `HostBase`。

- G1 Contracts 歸 Application：Response contract 由 use case 擁有，Host 只做 HTTP 轉接（狀態碼/headers），禁自創形狀。
- G2 判定只在 Application：enforcer 住 Application，Host 只搬運身份（claims → 調用上下文）。
- G3 驗證分層：格式驗證在 Host（早失敗），業務規則驗證在 Application（單一真相，永不信任 Host）。
- G4 營運：`Hosts.Migrator` 為唯一遷移執行者（固定順序、冪等；線上 Host 禁自動 migrate；按 Host 分 migrate 明確否決——migration 是資料庫 concern）；`Hosts.Dev` 一鍵全起僅開發用；`HostBase` 統一 OpenTelemetry＋tenant/store 標籤。
- **組合根（composition root）**：各模組 Infrastructure SHALL 暴露單一組合入口擴充方法（如 `AddIam(this IServiceCollection)`）；Host 端點程式碼仍僅觸 Application ports＋DTO，允許調用組合入口（ArchTests 以白名單明文豁免組合入口類別）；`Hosts.Migrator` 因唯一遷移執行者身分取得明文豁免（需引用模組 EF DbContext 進行遷移接線）；SharedKernel 橫切 port（`IEventBus`、`IClock`）的 binding adapter SHALL 住 `HostBase`（跨模組共享，禁入單一模組的 Infrastructure——否則他模組將被迫依賴 Iam 模組取用 SharedKernel port）。

曾考慮替代方案：第一天即微服務。否決：分散式租戶執法與分散式驗證會擋在第一個可用登入之前；Hosts 模型已把部署邊界切到組織單位，事件邊界保留後續拆分路徑，無需現在付出代價。

### 2. Shared-db tenancy with `tenant_id` + global query filters（共享資料庫租戶，以 `tenant_id` + 全域查詢過濾隔離）

單一資料庫，所有 IAM 實體攜帶 `tenant_id`，EF Core 全域過濾套用由身分驗證工作階段在伺服器端解析的作用中租戶（絕不單獨信任客戶端自帶標頭）。單一 migration 管線。

曾考慮替代方案：每租戶一庫。暫緩：首切片不值得付出營運成本（佈建、migration 扇出）；待醫院合規要求實體隔離時再議。事件邊界使該遷移保持可行。

例外：平台級 Party 身份庫不受 `tenant_id` 隔離約束，見決策 8；其跨租戶可達性僅限精確識別比對（`identity/party` spec）。

### 3. Short-lived JWT access + rotating opaque refresh（短效 JWT 存取 + 輪換不透明刷新）

存取權杖為無狀態 JWT（分鐘級壽命，僅攜身份與 `tenant_id` 供路由與租戶解析；即使帶 role claims 也絕不作為授權依據——每次判定一律走伺服器端執行器（enforcer））。刷新家族按次登入簽發（按裝置獨立）：有效刷新換新配對並作廢舊權杖；配置寬限窗（`RefreshReuseWindowSeconds`，見決策 7）內重放回傳現行配對不撤銷，窗外重放撤銷家族；停用或改憑證撤銷全家族（覆蓋 `user-auth` 刷新場景）。

曾考慮替代方案：純伺服器端工作階段。否決：第一天即強制黏性狀態，並使未來 API 消費者（店家/POS 裝置）複雜化。

### 4. Credentials with Argon2id, generic failures（憑證以 Argon2id 雜湊，失敗訊息通用化）

Argon2id 雜湊；登入失敗一律回傳單一通用錯誤（不形成使用者枚舉 oracle）；已停用使用者在登入與刷新兩側皆 fail-closed。登入請求攜租戶識別（T1：`{tenant_id, login_name, password}`），未知租戶與錯密碼同訊息、同狀態碼；回應時間墊至同一量級（計時歸一化），未知租戶不得快失敗。

### 5. Frontend F2 app shells + seam contracts（前端 F2 應用殼＋接縫合約）

`frontend/apps/{hq,store,consumer}` 獨立應用殼（`consumer` 先空殼佔位），共用 `packages/{domain,application,components,theme,shell}`；依賴單向（apps→packages），eslint 斷言。`components` 為純展示型（props/events、無 store）；`theme/tokens.css` 為樣式唯一來源；port 簽名禁框架型別；平台能力經 application ports。各 app 宣告封裝目標（Web 先行，Web+Tauri+native 路徑敞開）。替換框架僅重寫殼與轉接，加 Tauri 殼不動 packages。

### 6. Reqnroll maps spec scenarios 1:1（Reqnroll 與 spec 場景一對一映射）

四份 delta specs 的每個 `#### Scenario` 對應一條 `.feature` 場景，以 Testcontainers Postgres 執行；單元測試覆蓋權限判定矩陣，整合測試覆蓋隔離與刷新輪換。

### 7. Policy-watch revocation propagation with `AuthorizationOptions`（政策監看撤銷傳播，以 `AuthorizationOptions` 調校）

授權由伺服器端執行器（enforcer，casbin 風格；其 `domain` 與租戶 1:1 映射，使隔離與權限判定共用同一模型）判定。`RoleRevoked` 事件經既有領域事件匯流排（Domain Event Bus）傳遞；各節點監看（watch）並於配置上限內刷新執行器——無需每請求查詢政策資料庫。所有調校收於部署級選項（啟動驗證、可熱更新），絕不寫死：

| Option（選項名維持英文） | Default（預設值） | Notes（說明） |
|---|---|---|
| `PropagationBoundSeconds` | 5（5 秒） | 傳播場景斷言的上限（測試可覆寫） |
| `Transport` | InMemory | **分階段**：多節點中繼期換 **Valkey** pub/sub（BSD-3；Redis 授權之非 BSD 變更始於 7.4（RSALv2/SSPL）、Redis 8 起三重授權含 AGPLv3，違反本專案工具鏈選型原則）；broker 持久化期（V2 觸發器，ADR-009）改用 **RabbitMQ**（見決策 9 表＋`docs/adr/012`）——兩者皆同一 `IEventBus` 介面之下游演進，介面不變 |
| `WatcherInterval` | 1s（1 秒） | 匯流排傳輸無推送時的輪詢退路 |
| `JwtTtlMinutes` | 5（5 分鐘） | 僅為兜底；claims 不是授權輸入 |
| `EdgeTrustClaims` | false | 閘道不得僅憑 claims 放行 |

此處僅收部署級旋鈕。租戶級資料（角色定義、授權規則、租戶-domain 映射）留在政策庫——禁止 per-tenant options（租戶一多將撐爆配置面）。

### 8. Party modeling: global Party, per-tenant User, strict dedupe（Party 建模：Party 全域、User 按租戶、嚴格去重）

自然人/組織為全域唯一 Party（證件元組精確去重，禁模糊合併開關——去重語意一動即醫療事故，不給運行時旋鈕）；登入帳號與角色按租戶掛載；地址/連絡人/證件掛 Party；同意授權為角色時間軸 append-only。Party 庫為租戶隔離的守衛例外：僅精確識別比對可達，不可列出或遍歷（`identity/party` spec）。人工合併流程 defer，本期不做。

### 9. Bottom-layer toolchain selection table（底層工具選型總表）

全部工具為 OSI 授權且未付費化，並遵守 ADR-010 接縫義務（contract-kept 分級）。本節為摘要；完整查證證據（license 驗證來源、套件內容實測）見 `docs/adr/012-toolchain-selection.md`。必須回避之已付費化工具：MediatR、AutoMapper、MassTransit（v9 起商業）、Redis 8（RSAL/SSPL）、Redpanda（BSL）、EPPlus（Polyform Noncommercial）、QuestPDF（source-available＋營收門檻）、iText（AGPL＋商業雙授權）、Vault（BSL, IBM）、MinIO（AGPL＋停止維護）、Moq（SponsorLink）、FluentAssertions v8（改用 fork `AwesomeAssertions`（Apache-2.0））、SpecFlow（停維；改用 fork `Reqnroll`）。

| Seam / 用途 | 首選（授權） | Fallback（授權） | 接縫等級 |
|---|---|---|---|
| `ICommandBus` / `IEventBus` | Paramore Brighter / Darker（MIT） | CAP（MIT，outbox/saga 補位） | contract-kept |
| 事件傳輸（.transport） | RabbitMQ（＋`RabbitMQ.Client` v7，Apache-2.0/MIT 雙授權） | Valkey＋輪詢退路 | contract-kept |
| DTO/Entity 映射 | Riok.Mapperly（Apache-2.0，source-gen） | Mapster（MIT）、手寫 partial map | contract-kept |
| 授權判定（enforcer） | Casbin.NET（Apache-2.0；NuGet 套件 ID 為 `Casbin.NET`，Apache Foundation 孵化） | OPA（Apache-2.0） | 模組用（G2：enforcer 住 Application；grade 見 D1'/ADR-010 決策說明） |
| DTO 驗證 | FluentValidation（Apache-2.0） | DataAnnotations（內建）、Application 手寫規則驗證 | contract-kept |
| Logging | Serilog（Apache-2.0）＋ OpenTelemetry（Apache-2.0） | NLog（BSD-3）、`Microsoft.Extensions.Logging`（MIT） | contract-kept |
| Persistence | EF Core＋Npgsql（MIT，PostgreSQL 原生） | Dapper（Apache-2.0） | contract-kept｜替換＝major（ADR-010） |
| 授權雜湊 | Konscious.Security.Cryptography.Argon2（MIT，已查證） | Isopoh.Cryptography.Argon2（CC BY 4.0，屬性授權——選用前重讀條款） | auth 接縫（contract-kept） |
| JWT（短效存取權杖） | Microsoft.IdentityModel.JsonWebTokens（MIT） | System.IdentityModel.Tokens.Jwt（MIT） | auth 接縫（contract-kept） |
| 快取 | Valkey＋`StackExchange.Redis`（MIT） | FusionCache / `HybridCache`（MIT） | contract-kept |
| 背景作業 | Quartz.NET（Apache-2.0） | Hangfire Core（LGPL-3.0，商用免費已驗證） | contract-kept |
| 物件儲存（S3 協定） | SeaweedFS（Apache-2.0） | Ceph（LGPL-2.1） | contract-kept |
| 機密管理 | OpenBao（MPL-2.0） | 雲 KMS（需自包 port） | contract-kept |
| HTTP client | Refit（MIT）＋`IHttpClientFactory`（MIT） | TypedClient 手寫 | contract-kept |
| 韌性策略 | Polly（BSD-3）＋`Microsoft.Extensions.Resilience`（MIT） | 手寫 policy | contract-kept |
| 檔案處理（Office） | DocumentFormat.OpenXml（MIT）；ClosedXML（MIT） | NPOI（Apache-2.0）、MiniExcel（Apache-2.0） | 模組用｜非架構接縫 |
| 檔案處理（PDF） | PdfPig（Apache-2.0，讀）；PDFsharp（MIT，產生） | ExcelDataReader（MIT，唯讀 Excel） | 模組用｜非架構接縫 |
| 時間處理 | NodaTime（Apache-2.0） | — | 模組用 |
| OpenID Provider | OpenIddict（Apache-2.0）或 Keycloak（Apache-2.0） | — | Post-V1（決策見 `Open Questions`） |
| 原始碼產生器 | Reqnroll source-gen；Mapperly | — | 無 runtime binding，換套件即換 |

勝過：把選型留在對話（session 丟失即決策失憶，同 D4' 論證）；只寫工具名不記授權與證據（未來替換時重複全部調查）。調查含第二輪 license 查證與 nupkg 實測（案例：DataFusionSharp `runtimes/<rid>/native/` 已內含四平台組件，restore 即用、無額外部署步驟——「FFI＝部署複雜」是誤判，實質風險在單一維護者與 extension API 橋接缺口，詳見 `docs/adr/012-toolchain-selection.md`）。
落 design：本節（spec 不收）；完整證據與排除清單落 `docs/adr/012-toolchain-selection.md`。


## Risks / Trade-offs

- [Risk] 手寫查詢繞過全域過濾 -> 緩解：基礎設施測試套件斷言每條 IAM 查詢皆含 `tenant_id`；archive 前經 review-work 閘門。
- [Risk] 刷新權杖失竊 -> 緩解：輪換 + 重放即撤銷家族；短存取壽命收斂爆炸半徑。
- [Risk] 偽造範圍冒充 `tenant_id` -> 緩解：作用中租戶僅由已驗證工作階段在伺服器端解析；跨租戶存取預設拒絕（見 `authorization` spec）。
- [Risk] 共享庫鄰居干擾 / 未來合規拆分 -> 緩解：首切片接受；模組 ports 與事件使抽離保持可能；以 ADR 記錄重議觸發條件。
- [Trade-off] 已撤銷 JWT 至過期前仍可用 -> 緩解：JWT 無授權效力（決策 3），陳舊權杖單獨授予不了任何東西；執行器側傳播（決策 7）收斂真實空窗；短過期限制重放價值。
- [Accepted] 資料面 DB 計時殘餘差異 -> 接受：計時歸一化僅做驗證端（登入）；資料面以統一 404＋單一路徑＋游標分頁封堵實務枚舉，殘餘計時差接受不修。

## Migration Plan

綠地，無資料遷移。部署順序：PostgreSQL -> `dotnet ef database update` -> IAM API -> SPA。以冪等 seeder 建立初始租戶 + 操作員。回滾：重跑前一版 migration + 重新部署前一版 API；本變更無破壞性 DDL。

## Open Questions

- 店家/消費者 SSO 的 OIDC 聯邦供應商：延至第二個業務上下文落地；IAM 切片本地簽發器已足夠。
- 刷新權杖家族壽命與絕對工作階段上限：`apply` 期可調，不動 specs。
- 稽核日誌保留期：需產品意見，醫院上下文之前決定即可；本期不阻塞。
