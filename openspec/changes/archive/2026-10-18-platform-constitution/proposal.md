# Proposal（提案）

## Why（為何）

架構決策現散於 `iam-foundation` 決策 1 與 `store-onboarding` 決策 6；`archive` 只同步 `specs/`，design 歸檔即散佚，主規格永遠看不到架構。需一個歸檔後仍存活、可引用的架構契約，加上人類可讀的決策脈絡。grill-me 拷問（9 決策）後範圍再擴：補上「底層可替換性、商品化核心」的另一半契約——技術交接期可安全替換、同一核心組合多商品、一切規則落檔為人／AI 交接載體。

## What Changes（變更內容）

- 新增能力 `platform/architecture`：三護欄＋G1–G4＋M1 寫成可斷言 requirements（scenario 即 ArchTests）。
- `docs/adr/001-003`（已落盤，與兩份 design 決策同步；tasks 1.1 對照）：L2 分層、Hosts 模型、營運三件的人類脈絡。
- 新增 `docs/adr/010-011`：接縫目錄與替換程序、商品邊界與 kernel 共享基線（grill-me＋Oracle 檢視紀錄濃縮）。
- 新增 6 條 requirements：`Swappable seam catalog`、`Swap acceptance and rehearsal`、`Contract seedwork admission`、`Event delivery contract`、`Product repo boundary and kernel baseline`、`Rule precedence`（契約內容優先序 spec > ADR > AGENTS.md）。
- `design.md` 依 `instructions` 建立並含 C4 平台圖（grill 擴大範圍後產生新 HOW，原始「條件性跳過」案不復適用）。
- **BREAKING**：無（約束性規格，不改行為）。

## Capabilities（能力）

### New Capabilities（新能力）

- `platform/architecture`：L2 模組分層、Hosts 組裝、三護欄、Contracts/判定/驗證歸屬、守衛先行、單一 Migrator；接縫守護與替換驗收、contract seedwork 准入、事件傳遞契約、商品倉邊界與 kernel 共享基線、規則優先序。

### Modified Capabilities（修改既有能力）

- 無（主規格為空）。

## Impact（影響）

- 主規格新增 `platform/architecture`；`docs/adr/` 新增 001-003（已存在）與 010-011。
- `iam-foundation` 與 `store-onboarding` 的 ArchTests 任務成為本契約的執行載體（對照見 tasks 1.2/2.1）。
- `docs/glossary.md` 新增 11 個中英對照術語；歸檔時同步反寫 `AGENTS.md` 架構索引。
- 範圍之外：任何行為變更、模組實作、kernel registry 落點定案（design Open Questions）。
