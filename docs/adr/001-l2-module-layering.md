# ADR-001：L2 單專案模組分層

- Status（狀態）：Accepted（接受）
- Date（日期）：2026-10-16

## Context（背景）

模組內 `Domain`/`Application`/`Infrastructure`/`Api` 四層需強制隔離。候選：L1（層＝專案，編譯器強制）與 L2（層＝資料夾＋NetArchTest，測試強制）。

## Decision（決策）

採用 L2：每模組單專案（`{Domain,Application,Infrastructure}`＋`ArchTests`），模組內無 Api。

## Rationale（理由）

L1 在 6 模組時膨脹至約 27 個專案，建置變慢且 AI agent 配錯參照的機率上升；L2 線性增長，與 H1 守衛先行同哲學（測試即執法），違規一樣建置失敗。

## Consequences（後果）

- 測試外的手動繞過理論上可能，但 CI 紅燈擋下。
- 既有 tasks 1.1（iam、store）已改為 L2＋NetArchTest。
