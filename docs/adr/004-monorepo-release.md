# ADR-004：單倉＋單一平台版本與發佈紀律

- Status（狀態）：Accepted（接受）
- Date（日期）：2026-10-16

## Context（背景）

組合性將長出大量子專案（後端模組/Hosts、前端專案），版本矩陣與契約漂移風險隨之而來。候選：單倉單版本 vs 多倉獨立版本。

## Decision（決策）

- 單倉先行（至少至醫院端上線）：後端 `src/`、前端 `frontend/` 同倉，單一平台版本號（`version.json` 唯一真相）；組合性由專案參照＋ArchTests 保證。
- 契約紀律：ports、領域事件、REST/OpenAPI 僅加法演進；破壞性變更標 major＋遷移說明；契約測試守門（見 `platform/architecture` spec）。
- 流程紀律：固定發佈節奏、conventional commits→changelog、CI 四閘門（build＋ArchTests＋Reqnroll＋lint）全綠才可合併；每 release 附 OpenAPI 快照為前端契約真相。

## Rationale（理由）

地基期付多倉管理稅無回報；單倉下跨模組修改原子提交，無版本矩陣地獄。spec 只收機器可斷言的契約條款，流程紀律另立於此。

## Consequences（後果）

- 團隊拆分或醫院端後重議單倉存續（revisit trigger）。
- 多倉若將來採用，契約發佈、版本矩陣、跨倉 CI 須另立設計。
