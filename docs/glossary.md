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
