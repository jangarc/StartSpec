# Glossary（術語表，中英對照）

新領域術語必須先入此表再使用，以防術語漂移。New domain terms must enter here before use.

| English | 繁中 | 說明 |
|---|---|---|
| tenant | 租戶 | 隔離邊界單位；品牌＝租戶 |
| organization | 組織 | 租戶內的初始組織紀錄 |
| Party | 方 | 自然人或組織，不可再分；證件去重全域唯一 |
| role | 角色 | Party 在租戶／店內的職能掛載，可多重、可調動 |
| permission | 權限 | 經角色授予的可判定行為 |
| claim | 聲明 | JWT 內鍵值；不具授權效力 |
| refresh family | 刷新家族 | 按次登入簽發的刷新權杖組；重放窗外即全家族撤銷 |
| propagation bound | 傳播上限 | 撤銷推送至所有節點的時間上限（可配置，`PropagationBoundSeconds`） |
| guard test | 守衛測試 | 先於被守衛查詢存在的斷言測試（H1 前例） |
| canonical exposure | canonical 暴露 | 每 use case 唯一的標準暴露，其他 Host 薄轉接 |
| envelope | 信封 | 領域事件標準中繼資料（七欄） |
| tombstone | 墓碑 | 下架／撤回事件，標記公開資料失效 |
| swappable seam | 可替換接縫 | 公開支援替換的底層邊界（persistence／event transport／Host 組裝＋前端框架／auth） |
| contract-kept | 契約維持 | 接縫預設義務：port contract tests＋fake＋ArchTests 不漂移 |
| swap-proven | 替換已證 | 第二實作成真時的替換全驗義務 |
| contract seedwork | 契約種子 | port contract tests 基底類與 SharedKernel port fake 的共住架構位 |
| thin kernel | 薄核心 | SharedKernel＋橫切 ports＋事件信封基底＋模組准入規則；新商品 fork 的核心 |
| product repo | 商品倉 | 一商品一倉；自帶 tenant 空間、`version.json` 與部署 |
| kernel baseline | kernel 基線 | 跨商品倉的 kernel 共享基線；契約面僅加法、修復走排程 bump |
| drift alarm | 分歧警報 | 商品倉 kernel 落後／分歧逾界時 CI 發出的警報 |
| swap rehearsal | 替換演練 | 於 `Hosts.Dev` 先行演練替換並記錄 rollback note |
| rule precedence | 規則優先序 | 架構契約內容 spec > ADR > AGENTS.md；process discipline 住 ADR |
