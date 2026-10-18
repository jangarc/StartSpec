# ADR-012：底層工具鏈選型總表（Toolchain selection）

- Status（狀態）：Accepted（接受）
- Date（日期）：2026-10-18

## Context（背景）

平台宣稱四接縫可替換（ADR-010），但具名工具與授權證據原僅存於對話調查（兩輪 license 查證）；session 曾無預警丟失，對話記憶不具裁決力（platform-constitution D4'）。本 ADR 為持久載體：記錄首選、fallback、授權（SPDX）與查證證據，供未來替換與跨商品引用。規範義務不變：替換驗收依 ADR-010（contract-kept 分級；persistence 替換＝major）。

**禁用（已付費化／授權違規，一律回避）**：MediatR、AutoMapper（商業）；MassTransit v9（商業）；Redis 8（RSALv2/SSPL，→ Valkey BSD-3）；Redpanda（BSL，→ RabbitMQ Apache-2.0）；EPPlus（Polyform Noncommercial，→ ClosedXML MIT / NPOI Apache-2.0）；QuestPDF（source-available＋營收門檻，→ PDFsharp MIT）；iText（AGPL＋商業，→ PDFsharp / PdfPig）；Vault（BSL/IBM，→ OpenBao MPL-2.0）；MinIO（AGPL-3.0＋官方宣告停止維護，→ SeaweedFS Apache-2.0）；Moq（SponsorLink 事件，→ NSubstitute BSD-3）；FluentAssertions v8（商業，→ fork AwesomeAssertions Apache-2.0）；SpecFlow（停維，→ fork Reqnroll）。

## Decision（決策：首選＋fallback＋接縫等級）

| Seam / 用途 | 首選（SPDX） | Fallback（SPDX） | 等級 |
|---|---|---|---|
| `ICommandBus` / `IEventBus` | Paramore Brighter / Darker（MIT；10.7.0 活躍，.NET 8/9/10） | CAP（MIT） | contract-kept |
| 事件傳輸 | RabbitMQ；`RabbitMQ.Client` v7（Apache-2.0/MIT 雙授權） | Valkey 輪詢退路 | contract-kept |
| DTO/Entity 映射 | Riok.Mapperly（Apache-2.0；source-gen、無反射） | Mapster（MIT）；手寫 partial map | contract-kept |
| 授權判定 enforcer | casbin-dotnet（Apache-2.0；Apache Foundation 孵化中） | OPA（Apache-2.0） | contract-kept（enforcer 住 Application，Hosts 只搬身份） |
| DTO 驗證 | FluentValidation（Apache-2.0） | DataAnnotations；Application 手寫 | contract-kept |
| Logging/遙測 | Serilog（Apache-2.0）＋ OpenTelemetry（Apache-2.0） | NLog（BSD-3）；`Microsoft.Extensions.Logging`（MIT） | contract-kept |
| Persistence | EF Core＋Npgsql（MIT） | Dapper（Apache-2.0） | contract-kept；替換＝major |
| Argon2id | Konscious.Security.Cryptography.Argon2（Apache-2.0） | Isopoh.Cryptography.Argon2（MIT） | 模組測試守護 |
| 快取 | Valkey（BSD-3）＋StackExchange.Redis（MIT，對 Valkey 相容） | FusionCache / HybridCache（MIT） | contract-kept |
| 背景作業 | Quartz.NET（Apache-2.0） | Hangfire Core（LGPL-3.0，商用免費） | contract-kept |
| 物件儲存（S3） | SeaweedFS（Apache-2.0） | Ceph（LGPL-2.1） | contract-kept |
| 機密管理 | OpenBao（MPL-2.0） | 雲 KMS（自包 port） | contract-kept |
| HTTP client | Refit（MIT）＋IHttpClientFactory（MIT） | TypedClient 手寫 | contract-kept |
| 韌性 | Polly（BSD-3）＋Microsoft.Extensions.Resilience（MIT） | 手寫 policy | contract-kept |
| Office | DocumentFormat.OpenXml（MIT）；ClosedXML（MIT／Excel） | NPOI（Apache-2.0）；MiniExcel（Apache-2.0）；ExcelDataReader（MIT 唯讀） | 模組用、非架構接縫 |
| PDF | PdfPig（Apache-2.0 讀取）；PDFsharp 6（MIT 讀寫／產生） | — | 模組用、非架構接縫 |
| 其他 | NodaTime（Apache-2.0）；FluentFTP（Apache-2.0）；OpenIddict / Keycloak（Apache-2.0，Post-V1） | Humanizer（MIT）；Bogus（MIT 測試資料） | 依用途 |
| 測試棧 | Reqnroll（MIT）＋Testcontainers（MIT）＋NetArchTest（MIT，休眠中） | ArchUnitNET（Apache-2.0，活躍） | — |

## Rationale（理由）

- 工具選型以「授權永不成脅」為第一約束（使用者明文：禁用任何已改為收費的工具），其次為社群活躍度與功能完整度。
- 指定替換查證（2026-10）：Brighter 活躍（release 常態）；casbin-dotnet 已捐入 Apache Foundation 孵化（bus factor 改善）；Mapperly 活躍（source-gen 使替換成本純粹落在套件換名）。
- **nupkg 實測教訓（DataFusionSharp 0.6.3）**：「FFI＝部署複雜」是誤判——native 組件已內含於套件（`runtimes/<rid>/native/`，win-x64 ~133 MB），restore 即用。真正的風險是：2026-01 建立、單一維護者、❌UDF／❌Catalog／❌Table providers 三缺口（→ 物件儲存接不上、SQL 退回字串拼接、業務函數無法下推而全量回撈）。因此其授權合規（Apache-2.0），仍列 fallback 而非首選。
- 具名工具維持 contract-kept 義務：換工具不動 port 契約；persistence 例外（major＋遷移說明）。

## Consequences（後果）

- 被 `iam-foundation/design.md` 決策 9 引用為完整證據源；`archival-export` change（未開）提案時可直接引用本表與 platform-constitution D10'（Parquet＋DuckDB 首選＋DataFusionSharp fallback）。
- 負面：工具退場／收費化事件（如 FluentAssertions v8）需人工監視——四閘門中 lint 站不加檢；以本 ADR 為審查時的對照基準。
- Follow-up：fallback 升首選時（如 NetArchTest 不再維護），不需改 spec（接縫等級不變），僅修本表與 iam design 決策 9。
