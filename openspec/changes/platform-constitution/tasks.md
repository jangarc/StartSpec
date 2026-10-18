# Tasks

## 1. 契約對照（Constitution mapping，規劃期可完成）

- [ ] 1.1 確認 `docs/adr/001-003` 存在且與兩份 design 決策一致（L2、Hosts＋三護欄＋G1–G4、營運三件＋M1），驗證：三檔齊備且無矛盾敘述
- [ ] 1.2 確認 `iam-foundation` tasks（1.1/1.2/2.4）與 `store-onboarding` tasks（1.1/1.3）的 ArchTests/守衛條目覆蓋本契約 19 requirements，驗證：覆蓋對照無缺項（缺項補回對應 tasks）
- [ ] 1.3 執行 review-work 閘門，驗證：審查報告附於變更且零開放 blocker
- [ ] 1.4 確認 `docs/adr/004` 與 `AGENTS.md` 發佈紀律一致，且契約演進 requirement 有契約測試策略承接（后續 change 的 E2E 須含契約測試），驗證：三處無矛盾敘述
- [ ] 1.5 確認 `docs/adr/009`（V1 信封＋鎖步）已被 iam/store 的事件任務引用（`IEventBus` 產生的事件帶標準信封），驗證：事件相關 tasks 與 ADR 無矛盾

## 2. 增補對照（grill 擴增範圍，接縫／商品化／交接）

- [ ] 2.1 對照四接縫與 iam/store 任務條目：persistence（iam 2.2 Testcontainers＋租戶隔離真實實作斷言）、event transport（iam 6.1 `IEventBus`）；auth（iam 4.2/4.3 契約場景）；Host 組裝與前端框架（iam 1.1/7.3/7.5 lint 守衛），驗證：映射表無缺項（缺項回補 iam/store tasks）
- [ ] 2.2 確認 `Contract seedwork admission` 與 `Event delivery contract` 的執行載體寫入 `iam-foundation` 任務（2.1 契約測試基底、4.2 fake、6.1 事件傳遞義務），驗證：任務文本含「契約場景由 spec 派生、fake 先行」且與「at-least-once＋冪等」無衝突
- [ ] 2.3 確認 ADR-010/011 與 ADR-004/008/009 無矛盾（多倉取代條款、process discipline 規範地位、broker V2 觸發器相容），並確認 `design.md` Open Questions 均附觸發條件，驗證：交叉檢查零矛盾、觸發條件明確

## Workflow follow-up

- 歸檔前：按 `design.md` Migration Plan 把 thin kernel／商品倉條目反寫 `AGENTS.md` 架構索引（spec > ADR > AGENTS.md 的落檔義務）。
- 以 `openspec archive platform-constitution` 歸檔，使 `platform/architecture` 進入主規格；歸檔順序不影響（本變更無行為依賴）。
