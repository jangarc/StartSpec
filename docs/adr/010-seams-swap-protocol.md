# ADR-010：接縫目錄與替換程序（Seams and swap protocol）

- Status（狀態）：Accepted（接受）
- Date（日期）：2026-10-17

## Context（背景）

平台宣稱底層工具可替換（persistence、event transport、Host 組裝與前端框架、auth），卻無「替換安全」的驗收定義。Oracle 檢視證實三個盲區：contract tests 看不到 tenant 隔離機制（EF global filter 屬真實實作）、in-memory `IEventBus` 語意與 broker 不同步（exactly-once vs at-least-once）、persistence 替換的 schema／資料可攜性無人負責。若不先釘程序，替換當天即是測試全綠但行為全裂。

## Decision（決策）

- 四接縫公開維護，義務分級：預設 **contract-kept**（每接縫維持 port contract tests＋fake＋ArchTests 斷言）；**swap-proven** 全驗義務（含檢驗語意、資料遷移）僅當第二實作成真時支付。
- 替換驗收＝port contract tests 對新實作全綠＋四閘門（build＋ArchTests＋Reqnroll＋lint）於新接線全綠＋`Hosts.Dev` 演練紀錄（contract 與 E2E 綠，附 rollback note）。
- `IEventBus` 契約承諾 **at-least-once** 傳遞＋消費者冪等義務；in-memory fake SHALL 模擬重投遞。
- persistence 接縫替換一律定為 **major** 版本事件（schema＋資料遷移說明＋受測 rollback）。

## Rationale（理由）

平行兩套實作（parallel-run）雙軌維護成本最高且 greenfield 無資料切換壓力；而「四閘門綠即安全」把「隔離機制重驗」與「資料遷移」排除在證據外。「未演練的替換程序」正是 ADR-009 否定的「假保險劣於無保險」；iam 的 Migration Plan 已立 rollback note 前例。select at-least-once 係因真實 broker 全屬此語意，且消費者冪等是廉價紀律；契約場景 SHALL 由 spec 派生、fake 先行為參考實作，禁由第一實作反推（防驗收循環）。

## Consequences（後果）

- 被 spec `Swappable seam catalog`、`Swap acceptance and rehearsal`、`Event delivery contract` 承接；iam/store 任務對照其守衛條目（tasks 覆蓋）。
- 負面：每接縫皆需維護契約套件與 fake；文件紀律成本高。
- Follow-up：契約套件的 CI 閘歸屬在 `iam-foundation` 骨架就位後定案（見 platform-constitution design Open Questions 2）。
- 替代案（已拒）：全層 swap-proven（預付無第二實作之稅）；語言/runtime 承諾（無真實動機）。
