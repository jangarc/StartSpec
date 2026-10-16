# ADR-005：前端接縫合約（框架可遷移）

- Status（狀態）：Accepted（接受）
- Date（日期）：2026-10-16

## Context（背景）

前端須保留替換 JS/CSS 框架的能力；消極禁令（lint 說不准）不足以讓 `apply` 有法可依，需積極介面合約。

## Decision（決策）

- 新增 `packages/components`（純展示型）：無狀態、無 store/router/API，僅經 props/events 通訊，僅依賴 `theme` tokens，禁碰 `domain`/`application`；有狀態容器留各 app。
- JS 接縫：port 簽名禁框架型別（`Ref` 等），只用純值＋回呼。
- CSS 接縫：樣式唯一來源為 tokens，禁裸值與非 token `@apply`（stylelint 白名單斷言）。
- 元件接縫：props/events 中性模式；`domain`/`application` 測試跑純 Node（需 DOM 即耦合味）。

## Rationale（理由）

每條皆可驗證（lint/test 即 scenario），故同步寫入 `platform/architecture` spec；換框架時 components 只需「照 props/events 重寫外皮」。

## Consequences（後果）

- 兩份 change 的前端 tasks 已納入 `components`＋接縫測試。
- 嚴禁為方便在 shared components 內直接調 store，違規建置失敗。
