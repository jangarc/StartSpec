# ADR-008：人機協作協議（P1 自驗＋證據）

- Status（狀態）：Accepted（接受）
- Date（日期）：2026-10-16

## Context（背景）

AI 執行、人類決策的協作若無成文協議，將出現：任務完成標準不一、AI 越權（擅自 commit、刪測試求綠）、長 session 幻覺、人的注意力耗在機器可自證之事上。

## Decision（決策）

- 角色分工：人擁有 WHAT/WHY、產品決策、三閘門、抽查權；AI 擁有 HOW、執行、自驗＋證據。
- 驗收權（P1）：`tasks.md`「驗證」欄即驗收標準，無證據＝未完成，AI 自律翻核取；人不逐項驗收。
- 三閘門：propose 審產物 → apply 中抽查 → archive 前 review-work。
- 抽查條款：人可隨點任一已完成 task 要求重驗證據；連續兩次拿不出，整批退回重做。
- AI 權限邊界：禁未明示 commit、禁刪測試求綠、禁臆測未讀代碼、禁留 broken state 收工。
- 證據標準：檔改→diagnostics 乾淨；命令→exit 0；測試→通過；委派→結果已驗收。
- 失敗協議：三連敗即停手→還原→oracle→問人，不 shotgun debug。
- Session 交接：進度只認 tasks 核取＋evidence，不認對話記憶。
- 語言：任務書繁中為主、調度提示英文、code 與識別字英文。

## Rationale（理由）

人的注意力是最貴資源，不花在機器可自證之事；抽查權＋整批退回使 AI 不敢水，人才敢放手。

## Consequences（後果）

- 所有 `apply` 遵循本協議；違反（無證據翻核取）視為缺陷，退回重做。
- P2（人逐項驗收）明確否決。
