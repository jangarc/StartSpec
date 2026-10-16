# Proposal（提案）

## Why（為何）

架構決策現散於 `iam-foundation` 決策 1 與 `store-onboarding` 決策 6；`archive` 只同步 `specs/`，design 歸檔即散佚，主規格永遠看不到架構。需一個歸檔後仍存活、可引用的架構契約，加上人類可讀的決策脈絡。

## What Changes（變更內容）

- 新增能力 `platform/architecture`：三護欄＋G1–G4＋M1 寫成可斷言 requirements（scenario 即 ArchTests）。
- 新增 `docs/adr/001-003`：L2 分層、Hosts 模型、營運三件的人類脈絡（grill-me 拷問紀錄濃縮）。
- `design.md` 條件性跳過：無新增 HOW（分層已在兩份 design 定案），本提案即設計說明。
- **BREAKING**：無（約束性規格，不改行為）。

## Capabilities（能力）

### New Capabilities（新能力）

- `platform/architecture`：L2 模組分層、Hosts 組裝、三護欄、Contracts/判定/驗證歸屬、守衛先行、單一 Migrator。

### Modified Capabilities（修改既有能力）

- 無（主規格為空）。

## Impact（影響）

- 主規格新增 `platform/architecture`；`docs/adr/` 新增三份決策記錄。
- `iam-foundation` 與 `store-onboarding` 的 ArchTests 任務成為本契約的執行載體（對照見 tasks）。
- 範圍之外：任何行為變更、模組實作。
