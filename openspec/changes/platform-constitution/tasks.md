# Tasks

## 1. 契約對照（Constitution mapping，規劃期可完成）

- [ ] 1.1 確認 `docs/adr/001-003` 存在且與兩份 design 決策一致（L2、Hosts＋三護欄＋G1–G4、營運三件＋M1），驗證：三檔齊備且無矛盾敘述
- [ ] 1.2 確認 `iam-foundation` tasks（1.1/1.2/2.4）與 `store-onboarding` tasks（1.1/1.3）的 ArchTests/守衛條目覆蓋本契約 13 requirements，驗證：覆蓋對照無缺項（缺項補回對應 tasks）
- [ ] 1.3 執行 review-work 閘門，驗證：審查報告附於變更且零開放 blocker
- [ ] 1.4 確認 `docs/adr/004` 與 `AGENTS.md` 發佈紀律一致，且契約演進 requirement 有契約測試策略承接（后續 change 的 E2E 須含契約測試），驗證：三處無矛盾敘述
- [ ] 1.5 確認 `docs/adr/009`（V1 信封＋鎖步）已被 iam/store 的事件任務引用（`IEventBus` 產生的事件帶標準信封），驗證：事件相關 tasks 與 ADR 無矛盾

## Workflow follow-up

- 以 `openspec archive platform-constitution` 歸檔，使 `platform/architecture` 進入主規格；歸檔順序不影響（本變更無行為依賴）。
