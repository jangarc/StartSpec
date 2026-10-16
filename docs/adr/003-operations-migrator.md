# ADR-003：營運三件與單一 Migrator

- Status（狀態）：Accepted（接受）
- Date（日期）：2026-10-16

## Context（背景）

多 Hosts 後須決定：migration 由誰跑、本機如何起全站、觀測如何一致。

## Decision（決策）

- `Hosts.Migrator` 為唯一遷移執行者（固定順序、冪等）；線上 Host 禁自動 migrate；按 Host 分 migrate 明確否決（migration 是資料庫 concern，共享庫下會順序打架）。
- `Hosts.Dev` 一鍵全起，僅開發用。
- `HostBase` 統一 OpenTelemetry＋tenant/store 標籤。

## Rationale（理由）

三者皆為「現在不定、半年後哭」的營運債；規範文字零實作成本，早定便宜。

## Consequences（後果）

- db-per-host 到來時重議 M1（revisit trigger，與隔離決策並列）。
- `InvitationTtlHours` 等調校延續 Options 哲學（部署級旋鈕，不在此 ADR）。
