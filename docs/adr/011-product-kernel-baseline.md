# ADR-011：商品邊界與 kernel 共享基線（Product repo boundary and kernel baseline）

- Status（狀態）：Accepted（接受）
- Date（日期）：2026-10-17

## Context（背景）

目標是「同一核心組合出不同領域的商品」。grill 定案核心＝**thin kernel**（`SharedKernel`＋橫切 ports＋事件信封基底＋模組准入規則），IAM/Store 屬商品 #1 的業務模組。但「一商品一倉」後每倉各持一份 kernel，「同一核心」立即面臨基線（baseline）分裂；且 ADR-004 已明言「多倉若採用，契約發佈、版本矩陣、跨倉 CI 須另立設計」——本 ADR 即是那份設計，須明文銜接。

## Decision（決策）

- **一商品一倉**：本倉＝商品 #1；新商品自 thin kernel fork 起建，各有自己的 tenant 空間、`version.json` 與部署。Party 去重「全域唯一」以單一商品倉為界。
- **kernel 共享基線＋加法升級**：加法僅限 kernel 契約面（contract surface）；修復以排程 baseline bump 傳遞；分歧窗有界並掛 CI drift alarm。
- **Contract seedwork 之家**：port contract tests 基底類＋僅 `SharedKernel` 已宣告 port 的 fake 共住；模組自有 port 的 fake 留在模組測試；契約場景由 spec 派生、fake 先行為參考實作。
- **優先序**：架構契約內容 spec > ADR > AGENTS.md；ADR 對不可斷言的 process discipline（如本條發佈節奏）仍具規範性；規則一律落檔（specs、ADR、AGENTS.md、glossary）為交接載體。

## Rationale（理由）

「零分歧＋加法」自相矛盾：bug fix 本質非加法，且零分歧等於商品倉被迫失去發佈節奏；「各倉自由演進」則核心 fork 後面目全非。排程 bump＋有界分歧窗＋drift alarm 取其中道：允許商品倉節奏差異，但漂移不可無聲。依賴對話傳承已被 session 丢失事件證偽，故交接以檔案為唯一載體。

## Consequences（後果）

- 被 spec `Product repo boundary and kernel baseline`、`Contract seedwork admission`、`Rule precedence` 承接。
- Open question：kernel registry 落點（母版倉 vs 發佈套件）與發佈機制（NuGet vs git remote），觸發條件＝第二商品倉成立，屆時發佈 ADR-004 修訂案。
- 負面：多一個架構位（seedwork）需准入紀律；文件優先序需人類閘門抽查捍衛。
- 替代案（已拒）：IAM+Store 模組群跨域共用（裁切災難）；monolith 複製（核心退化）。
