# ADR-007：SharedKernel 准入規則

- Status（狀態）：Accepted（接受）
- Date（日期）：2026-10-16

## Context（背景）

`SharedKernel` 無准入規則將在數月內淪為耦合磁鐵（DTO、工具、用例全往裡塞），屆時模組邊界名存實亡。

## Decision（決策）

- 准進三類：領域事件基底（含中繼資料）、三模組以上共用的核心值物件、橫切埠介面（`IEventBus`、`IClock`）。
- 晉升制：新值物件須被三處使用方可晉升，並以輕量記錄載明（批准人＋三處用途）；未達門檻者留所屬模組。
- 禁：業務實體、DTO、用例邏輯；審查退回。

## Rationale（理由）

K1 嚴格版：耦合垃圾場的清理成本十倍於准入審查；3-use 門檻以使用事實代替預測，避免過早抽象。

## Consequences（後果）

- iam tasks 1.1 建 SharedKernel 即受本規則約束。
- 晉升記錄獨立輕量，不佔 ADR 主編號（附於本 ADR 之附錄或 PR 描述）。
