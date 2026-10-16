# ADR-002：Hosts 按組織單位組裝 API

- Status（狀態）：Accepted（接受）
- Date（日期）：2026-10-16

## Context（背景）

單一 `AppHost`＋模組自帶 Api 使所有組織單位（HQ、店家、消費者）共用同一 API 面，攻擊面、發版節奏、擴展需求被迫一致。

## Decision（決策）

模組不含 Api；外層 `Hosts.*` 按組織單位組裝，共用模組庫。附三護欄（ports-only 引用、單一 canonical 暴露、橫切只進 `HostBase`）＋G1 Contracts 歸 Application、G2 判定只在 Application、G3 驗證分層，皆由 ArchTests 斷言。

## Rationale（理由）

部署邊界對齊組織，攻擊面分離（沒裝的功能無需授權擋），前端 BFF/SPA 行政區劃一致；事件邊界保留未來微服務拆分路徑。

## Consequences（後果）

- 同一 use case 多 Host 暴露須共用 handler，審查負擔上升（review-work 閘門覆蓋）。
- 替代方案（單一 AppHost、第一天微服務）否決理由見 `iam-foundation` design 決策 1。
