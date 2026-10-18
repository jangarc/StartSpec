# Design（設計）

> 本 doc 以 grill-me 拷問（9 決策）經 Oracle 檢視修正後的 D1'–D9' 為本；規範條文落在 `specs/platform/architecture/spec.md` delta（新增 6 requirements），決策脈絡落在 `docs/adr/010-011`。平台層級結構圖：`c4-platform-architecture.drawio`（本目錄），術語以 `docs/glossary.md` 為準。

## Context（背景）

- 既有憲法（13 requirements）焦點在分層與歸屬；本變更補上「可替換性與商品化」的另一半：接縫（swappable seam）如何守、替換如何證、核心（kernel）如何在商品間不分裂、人／AI 交接以誰為準。
- 前提約束：本專案尚無程式碼，所有 SHALL 均為 planning 型條款，執行載體是現有 tasks 的覆蓋對照與 ArchTests/守衛測試落入 `iam-foundation`、`store-onboarding`。
- 相關設計前置：`iam-foundation` 決策 2（global filter）、決策 4（Argon2id 計時歸一化）、決策 7（`IEventBus` in-memory→broker 的 V2 觸發器）、`store-onboarding` 決策 6、`docs/adr/004`（單倉＋`version.json` 唯一真相，並明言「多倉若採用須另立設計」）。

## Goals / Non-Goals

**Goals:**
- 技術交接期可安全替換底層工具：四接縫的守護義務、替換的驗收證據與演練 SHALL。
- 同一核心組合出多商品：thin kernel 定義、跨商品共享基線、商品倉邊界 SHALL。
- 「未來所有需求生產」安全可測試：contract seedwork 之家與其准入，事件傳遞語意一次釘死。
- 一切規則落檔成為交接載體：優先序 spec > ADR > AGENTS.md SHALL。

**Non-Goals:**
- 不實作任何接縫的第二實作（contract-kept 分級下，swap-proven 義務僅在替換成真時支付）。
- 不改動 13 條既有 architecture requirements 的條文（僅擴充 tasks 覆蓋對照）。
- 不定案 kernel registry 實體落點與發佈機制（見 Open Questions）。
- 不承諾語言/runtime 替換；host 替換範圍限 Host 組裝層（見 D5'）。

## Decisions（決策：以「勝過何替代案」表述；完整脈絡見 ADR-010/011）

**D1' 四接縫、分級守護**：persistence（EF Core/Npgsql）、event transport（`IEventBus`）、Host 組裝＋前端框架、auth（Argon2id/JWT）為公開支援的可替換接縫；每接縫維持 port contract tests＋fake＋ArchTests 斷言。預設義務為 contract-kept（接縫契約不漂移）；替換成真才付 swap-proven 全驗義務。
勝過：四層全 swap-proven（在無第二實作時預付全稅，違反 ADR-009「無觸發器不付 V2 稅」）；完全不守（接縫漂移無聲）。落 spec：`Swappable seam catalog`。

**D2' 替換驗收與演練**：替換驗收＝port contract tests 全綠＋四閘門（build＋ArchTests＋Reqnroll＋lint）於新接線全綠＋`Hosts.Dev` 演練（contract 與 E2E 套件綠，附 rollback note）；persistence 接縫替換視為 major 版本（附 schema／資料遷移說明）。
勝過：僅四閘門綠（Oracle 檢視證實隔離機制、傳遞語意、資料可攜性是 contract tests 看不到的盲區）；parallel-run（雙軌維護成本最高且 greenfield 無資料切換壓力）。落 spec：`Swap acceptance and rehearsal`。

**D3' Thin kernel**：核心＝`SharedKernel`＋橫切 ports＋事件信封基底＋模組准入規則；IAM/Store 是商品 #1 的業務模組，非核心。
勝過：IAM+Store 模組群跨域共用（領域不相通，強行共用即裁切災難）；monolith 複製（違反「同一核心」原意，核心退化）。落 spec：`Product repo boundary and kernel baseline`（共用條款）＋ AGENTS.md 索引（歸檔時同步）。

**D4' 檔案交接載體**：規則一律落到 specs＋ADR＋AGENTS.md＋glossary；對話記憶不具裁決力。
勝過：依賴 session/對話傳承（本 session 事件已證明 session 可無預警丢失）。落 spec：`Rule precedence`（檔案載體條款）。

**D5' Host 組裝層可換、韌體不動**：端點框架與中介層可替換；`HostBase` 為主要替換面（所有橫切集中處，替換成本集中於此）；Application/Domain 絕不動；不承諾語言/runtime 替換。
勝過：跨語言承諾（契約面倍增、無動機證據；若提出必先有替代 runtime 的真實需求）；完全不定義（彈性僅是事實而非可驗收義務）。落 design（本節）；spec 不收。

**D6' Contract seedwork 之家與准入**：port contract tests 基底類＋僅 SharedKernel 已宣告 port 的 fake 收入 seedwork 之家；模組自有 port 的 fake 留在模組測試；業務邏輯與 production 引用禁入；契約場景 SHALL 由 spec 派生、fake 先行為參考實作，禁止由第一個實作反推。
勝過：全收進 `SharedKernel`（准入規則開後門，違 ADR-007 防耦合磁鐵）；各模組自帶基底類（複製並漂移，每模組各自一份契約）。落 spec：`Contract seedwork admission`＋`Event delivery contract`（派生規則半）。

**D7' 一商品一倉**：本倉＝商品 #1；新商品自 thin kernel fork 起建，各有自己的 tenant 空間、`version.json` 與部署；Party 去重「全域唯一」以單一商品倉為界。
勝過：同倉多商品（tenant 模型與 G4 鎖步部署混淆、版本真相糾纏；ADR-004 已預言多倉需另立設計，D7 即是）。落 spec：`Product repo boundary and kernel baseline`。

**D8' Kernel 共享基線＋加法升級**：加法僅限 kernel 契約面（contract surface）；修復以排程 baseline bump 傳遞；分歧窗有界並掛 CI drift alarm；發佈機制（NuGet 套件 vs git remote）待第二商品倉成立時定案。
勝過：零分歧即時拉取（商品倉失去發佈節奏；bug fix 本質非加法，零分歧＋加法自相矛盾）；各倉自由演進（核心 fork 後面目全非，「同一核心」失守）。落 spec：`Product repo boundary and kernel baseline`。

**D9' 優先序（boundary-aware）**：架構契約內容的優先序為 spec > ADR > AGENTS.md；ADR 對不可斷言的 process discipline（發佈節奏、協作硬邊界）仍具規範性——修正是為不孤立 `docs/adr/004`（流程紀律另立於此）與 `008`（協作協議）的規範地位。
勝過：全面「ADR 僅 rationale」（與現有 corpus 衝突）；ADR>spec（AI 接手時過時 ADR 抵銷新 spec）。落 spec：`Rule precedence`。

## Risks / Trade-offs

- [契約測試反推第一實作（chicken-and-egg），驗收變循環] → D6' 派生規則：場景由 spec 派生＋fake 先行為參考實作。
- [in-memory `IEventBus` 語意與 broker 不符；消費者非冪等，broker 日契約測試全綠但行為全裂] → `Event delivery contract` 現在釘 at-least-once＋消費者冪等＋fake 模擬重投遞。
- [persistence 替換的 schema／資料可攜性，contract tests 看不到] → persistence 替換強制走 major 事件＋遷移說明＋受測 rollback；隔離不變量由真實實作＋Testcontainers 斷言（iam tasks 已規劃）。
- [kernel fork 漂移無聲] → 有界分歧窗＋CI drift alarm（`Divergence alarmed` scenario）。
- [文件型 SHALL 無自動 enforcement（優先序、落檔）] → review-work 閘門＋tasks 覆蓋對照；不偽稱全自動。
- [四接縫維護稅] → contract-kept 分級限縮義務；不躺平亦不預付。

## Migration Plan（產物路徑）

本變更無程式碼行為，純 planning：`openspec validate` 綠→依 tasks 覆蓋對照→歸檔時同步進主規格，並按 AGENTS.md「先改 designs 再同步索引」把 thin kernel／商品倉條目反寫進 `AGENTS.md` 架構索引。歸檔順序不影響（本變更無行為依賴）；排在 `iam-foundation` apply 前僅為它提供守衛測試與接縫工具的就位基礎。

## Open Questions

1. **Kernel registry 落點**（母版倉 vs 發佈套件）：觸發條件＝第二商品倉成立；同時定案 D8' 發佈機制並發佈 ADR-004 修訂案。
2. **契約套件的 CI 歸屬**（併入 build 閘或新增第五閘）：觸發條件＝`iam-foundation` apply 起模組骨架後、ArchTests 套件成案時定案。
