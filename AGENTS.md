# AGENTS.md

Greenfield OpenSpec scaffold — no app code, build, test, lint, or CI exists yet. `openspec/` is the source of truth. 綠地 OpenSpec 鷹架——尚無應用程式碼、建置、測試、lint 或 CI；`openspec/` 為唯一事實來源。

In case of conflict between the English and Traditional Chinese texts in this file, the English text prevails. 本文件若中英文義衝突，以英文為準。

## Layout

- `openspec/config.yaml` — schema `spec-driven` with project `context` constraints set. Read it before planning. schema 為 `spec-driven` 且已設定專案 `context` 約束；規劃前必須先讀取。
- `openspec/specs/` — main specs (currently empty, only `.gitkeep`). 主規格（目前為空，僅有 `.gitkeep`）。
- `openspec/changes/<name>/` — active change: `proposal.md`, `specs/<capability>/spec.md` (delta only), `design.md`, `tasks.md`. 進行中的變更：`proposal.md`、`specs/<capability>/spec.md`（僅增量）、`design.md`、`tasks.md`。
- `openspec/changes/archive/` — archived changes, do not edit. 已歸檔變更，不得編輯。
- `.opencode/commands/opsx-*.md` — slash workflows (`/opsx-propose`, `/opsx-apply`, `/opsx-archive`, `/opsx-explore`, `/opsx-sync`); mirror of `.opencode/skills/openspec-*/SKILL.md`. Skill/command text is authoritative for that workflow. 斜線工作流程；與 `.opencode/skills/openspec-*/SKILL.md` 互為鏡像；該工作流程以 skill/command 內文為準。
- `.opencode/node_modules/` — `@opencode-ai/plugin` deps only; ignore. 僅含 `@opencode-ai/plugin` 相依；忽略。

## OpenSpec rules

- Never assume paths: resolve root via `openspec context --json` (`root.path`), artifact paths via `openspec status --change "<name>" --json` (`planningHome`, `changeRoot`, `artifactPaths`). 絕不假設路徑：以 `openspec context --json` 解析根目錄（`root.path`），以 `openspec status --change "<name>" --json` 解析產物路徑（`planningHome`、`changeRoot`、`artifactPaths`）。
- Planning (`propose`) writes artifacts only — do not edit project code or start `apply` in the same turn. 規劃（`propose`）僅撰寫產物——同一回合不得編輯專案程式碼或啟動 `apply`。
- `apply` implements `tasks.md` checkboxes in order; flip `- [ ]` to `- [x]` at the exact `sourcePath`/`line` from `openspec instructions apply --change "<name>" --json`, then re-run instructions to confirm progress. `apply` 依序實作 `tasks.md` 核取方塊；依 `openspec instructions apply --change "<name>" --json` 回傳的精確 `sourcePath`/`line` 將 `- [ ]` 改為 `- [x]`，再重跑 instructions 確認進度。
- `design.md` is conditional (create only if `instructions` says so); `specs` delta is required unless `status` reports it `skipped` via `skip_specs`. `status: done` on `tasks` does not imply dependencies exist — walk `requires` edges transitively. `design.md` 為條件性產物（僅當 `instructions` 要求才建立）；`specs` 增量為必要，除非 `status` 經 `skip_specs` 回報 `skipped`；`tasks` 的 `status: done` 不代表其相依存在——必須沿 `requires` 邊遞移檢查。
- Do not copy `context`/`rules`/`operationGuidance` from `instructions` output into artifacts or code. 不得將 `instructions` 輸出的 `context`/`rules`/`operationGuidance` 複製進產物或程式碼。
- Verify with `openspec validate --change "<name>"` (or `--specs`) before `openspec archive <name>`; `archive` syncs deltas into `openspec/specs/`. 執行 `openspec archive <name>` 前必須以 `openspec validate --change "<name>"`（或 `--specs`）驗證；`archive` 會將增量同步進 `openspec/specs/`。

## Markdown bilingual convention (applies to ALL markdown produced in this project)

Language scope by artifact (to avoid self-contradiction, read this first): specs deltas are the cross-team contract and stay fully bilingual; `design.md` and `tasks.md` are Traditional-Chinese-primary with English keywords. 產物語言分工（優先閱讀，避免自我矛盾）：specs 增量為跨團隊契約，維持完整雙語；`design.md` 與 `tasks.md` 以繁中為主、關鍵詞維持英文。

- Structure keywords stay English: markdown headings used by tooling (`## ADDED Requirements`, `### Requirement:`, `#### Scenario:`, `- **WHEN**` / `- **THEN**`, `## Purpose`), checkbox markers (`- [ ] 4.3`), file names, capability paths, API/field identifiers, commands, option names, numeric units bound at first use (e.g. 5 秒（`PropagationBoundSeconds = 5`)). 結構關鍵字維持英文：工具解析用的 markdown 標題、核取方塊標記、檔名、capability 路徑、API/欄位識別字、指令、選項名、數字單位（首次出現時綁定）。
- Specs deltas: body is bilingual, English sentence first, then Traditional Chinese in the same paragraph/line. Keep normative `SHALL` in both languages; never soften to should/may. specs 增量內文為雙語：英文句在前、繁中在後，置於同一段落/行；兩種語言皆保留規範性 `SHALL`，絕不弱化為 should/may。
- `design.md` / `tasks.md`: Traditional Chinese primary; keep `SHALL` and all identifiers/commands in English, bound to their Chinese explanation at first use. `design.md` / `tasks.md` 以繁中為主；`SHALL` 與所有識別字/指令維持英文，首次出現時與繁中說明綁定。
- Scenario steps: keep `- **WHEN**` / `- **THEN**` on single lines with the Traditional Chinese translation in full-width parentheses on the same line — do not split translations onto separate lines. 場景步驟：`- **WHEN**` / `- **THEN**` 維持單行，繁中翻譯以全形括號附於同一行——不得拆行。
- Requirement titles and scenario names stay English (searchable, mappable to code and `.feature` files). Requirement 標題與場景名稱維持英文（可搜尋、可對應程式碼與 `.feature` 檔）。
- Each requirement description (text before its first scenario) stays within 500 characters total, both languages combined. 每條 requirement 描述（其首個場景之前的文字）中英合計維持在 500 字元內。
- Rationale: `openspec validate` parses structure, not language — this format keeps validation green while the team reads Traditional Chinese. New domain terms must enter the glossary with an English-Chinese mapping to prevent term drift; the glossary is mandatory once design/tasks go Chinese-primary. 原理：`openspec validate` 解析結構而非語言——此格式讓驗證通過，同時團隊可用繁中閱讀；design/tasks 以繁中為主後，glossary 為必備，新領域術語必須建立中英對照以防術語漂移。

## Environment

- Shell is Windows PowerShell (`pwsh`): use `Get-ChildItem -Force`, `Get-Content`; `ls -la`, `head`, `cat` do not exist. Shell 為 Windows PowerShell（`pwsh`）：使用 `Get-ChildItem -Force`、`Get-Content`；`ls -la`、`head`、`cat` 不存在。
- Git `master` has zero commits; scaffold (`.opencode/`, `openspec/`) is untracked. Commit only when asked. Git `master` 尚無提交；鷹架（`.opencode/`、`openspec/`）未追蹤；僅在被要求時才提交。

## OMO (oh-my-openagent)

- Global plugin `oh-my-openagent` is installed; no project-local `opencode.json` needed. 全域插件 `oh-my-openagent` 已安裝；無需專案級 `opencode.json`。
- Use `ultrawork` / `ulw` keyword to trigger multi-agent orchestration; `/ulw-plan` then `/ulw-execute` for plan-first work. 以 `ultrawork` / `ulw` 關鍵字觸發多代理編排；計畫優先的工作先 `/ulw-plan` 再 `/ulw-execute`。
- `openspec/` remains source of truth; OMO agents must follow OpenSpec rules above. `openspec/` 仍為事實來源；OMO agents 必須遵守上述 OpenSpec 規則。
- No app code yet, so no hierarchical `src/**/AGENTS.md` needed. Add scoped `AGENTS.md` when new top-level code dirs appear. 尚無應用程式碼，故無需階層式 `src/**/AGENTS.md`；新增頂層程式碼目錄時再補 scoped `AGENTS.md`。
