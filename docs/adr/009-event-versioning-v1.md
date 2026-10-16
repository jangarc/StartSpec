# ADR-009：事件 V1 版本化（信封＋加法＋鎖步）

- Status（狀態）：Accepted（接受）
- Date（日期）：2026-10-16

## Context（背景）

領域事件需演進規則，但現狀為單體＋in-memory bus，同行程同版本，零錯位；唯一錯位窗口是未來的多實例滾動部署、broker 持久化、服務拆分。

## Decision（決策）

- V1：標準信封（`event_id`、`occurred_at`、`tenant_id`、`correlation_id`、`causation_id`、`version`、`payload`）＋加法演進＋跨 Hosts 鎖步部署。
- V2（版本註冊、upcaster、雙發、相容矩陣）明確否決於現階段：零錯位下全額付稅，且 upcaster 無法在生產驗證（假保險劣於無保險）。
- V2 觸發器：broker 持久化或服務拆分第一天啟動。

## Rationale（理由）

信封欄位一上線即凍結，現在定；轉換機制現在寫不出可驗證的版本，不寫。

## Consequences（後果）

- 全 V2 副作用清單（測試矩陣爆炸、設計鎖死、雙發次生災害等）記於本決策，後人勿重蹈。
- 滾動部署在 broker 期之前禁止。
