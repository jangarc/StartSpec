# ADR-006：前端 F2 應用殼與封裝矩陣

- Status（狀態）：Accepted（接受）
- Date（日期）：2026-10-16

## Context（背景）

所有應用產品終將獨立封裝；基底為瀏覽器，終局為 Web（後台/網頁版）＋Web+Tauri+原生代碼（跨平台）。單 SPA 無法保證「HQ 碼不進 POS 包」（tree-shaking 保證沒用到，保證不了沒打包）。

## Decision（決策）

- F2：`apps/{hq,store,consumer}` 獨立應用殼，共用 `packages/{domain,application,components,theme,shell}`；依賴單向（apps→packages），ArchTests/eslint 斷言。
- 各 app 宣告封裝目標；Web 先行；Tauri 殼只加不動包（`platform/architecture` 有 scenario 為證）。
- 平台能力（儲存、裝置、殼）一律經 application ports；共用包直調瀏覽器/原生 API 即違規。
- `consumer` app 先開空殼佔位（路由佔位，不實作），證明拆分點。

## Rationale（理由）

稽核要的是「沒打包」而非「沒用到」；POS 離線/硬體連動遲早要安裝包，現在付組裝工（約 2–3 tasks），省未來搬家費。

## Consequences（後果）

- 兩份 change 的前端 tasks 已改為 F2 殼結構。
- 新增封裝目標須更新 app manifest 並經 review；禁偷渡原生呼叫進共用包。
