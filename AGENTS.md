# AGENTS.md

Greenfield OpenSpec scaffold — no app code, build, test, lint, or CI exists yet. `openspec/` is the source of truth. 綠地 OpenSpec 鷹架——尚無應用程式碼、建置、測試、lint 或 CI；`openspec/` 為唯一事實來源。

In case of conflict between the English and Traditional Chinese texts in this file, the English text prevails. 本文件若中英文義衝突，以英文為準。

## Goals

- Enterprise cloud full-stack app, delivered in vertical slices in order: `iam-foundation` → `store-onboarding` → consumer → hospital. 企業雲端全端應用，依序以垂直切片交付：`iam-foundation` → `store-onboarding` → 消費者端 → 醫院端。
- Backend C# / .NET (DDD + BDD), frontend Vite + Vue 3 (swappable), PostgreSQL. 後端 C#／.NET（DDD＋BDD），前端 Vite＋Vue 3（可替換），PostgreSQL。
- Out-of-slice needs are recorded only, never built. 非當期 slice 的需求只記錄、不實作。

## Layout

- `openspec/config.yaml` — schema `spec-driven` with project `context` constraints set. Read it before planning. schema 為 `spec-driven` 且已設定專案 `context` 約束；規劃前必須先讀取。
- `openspec/specs/` — main specs (currently empty, only `.gitkeep`). 主規格（目前為空，僅有 `.gitkeep`）。
- `openspec/changes/<name>/` — active change: `proposal.md`, `specs/<capability>/spec.md` (delta only), `design.md`, `tasks.md`. 進行中的變更：`proposal.md`、`specs/<capability>/spec.md`（僅增量）、`design.md`、`tasks.md`。
- `openspec/changes/archive/` — archived changes, do not edit. 已歸檔變更，不得編輯。
- `.opencode/commands/opsx-*.md` — slash workflows (`/opsx-propose`, `/opsx-apply`, `/opsx-archive`, `/opsx-explore`, `/opsx-sync`); mirror of `.opencode/skills/openspec-*/SKILL.md`. Skill/command text is authoritative for that workflow. 斜線工作流程；與 `.opencode/skills/openspec-*/SKILL.md` 互為鏡像；該工作流程以 skill/command 內文為準。
- `.opencode/node_modules/` — `@opencode-ai/plugin` deps only; ignore. 僅含 `@opencode-ai/plugin` 相依；忽略。

## OpenSpec rules

- `config.yaml` 的 `context` 是全程契約，`apply` 期也一律回讀——切實作前，先依依讀 config 的 context（且站樣式層等技術選擇以 context 為準），再讀 design/tasks。`openspec validate` 不解析內容語意，契約漂移只能靠「回讀」防止（7.x Tailwind 偏差的教訓）。

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

## Architecture (constitution — violations fail the build)

- Design principles are standing law for ALL code (backend, frontend, scripts, specs): follow SOLID, KISS, YAGNI, DRY, LoD (Law of Demeter), CQS (separate queries from commands), SOC, AHA (avoid hasty abstractions — prefer duplication over the WRONG abstraction). Principles CONFLICT in practice; mediate explicitly, priority order: safety/contract > correctness > clarity > DRY/KISS > micro-efficiency. Known tensions: DRY vs AHA (abstract only on the third real repetition); KISS/YAGNI vs premature SOLID structure (add indirection only when a different implementation is actually planned); CQS vs event publishing (publishing is a command-path side effect — a command use case may both persist and emit); LoD is enforced structurally (props/events, ports, scoped stores — no reaching through strangers). 全部程式碼一律遵守 SOLID、KISS、YAGNI、DRY、LoD、CQS、SOC、AHA，衝突時明確調停：安全/契約 > 正確性 > 清楚性 > DRY/KISS > 微效率；DRY 與 AHA 折衝（第三次真實重複才抽象）、過早 SOLID 結構違 YAGNI、事件發布屬命令路徑副作用（CQS 的例外即記錄）、LoD 以結構強制（props/events／ports／scoped store）。

- Modules are L2 single-project (`src/Modules/<Name>/{Domain,Application,Infrastructure}` + `ArchTests`); no Api inside modules. Layering is enforced by NetArchTest, not by project splitting. 模組為 L2 單專案（`{Domain,Application,Infrastructure}`＋`ArchTests`），模組內無 Api；分層由 NetArchTest 強制，不以多專案切分。
- API surfaces are composed per org unit by outer `Hosts.*` (`Hosts.HQ`, `Hosts.Store`, `Hosts.Consumer`, …) sharing module libraries; `HostBase` holds shared startup, `Hosts.Migrator` is the sole migrator (online Hosts never auto-migrate), `Hosts.Dev` is dev-only. API 面按組織單位由外層 `Hosts.*` 組裝，共用模組庫；`HostBase` 收共用啟動，`Hosts.Migrator` 為唯一遷移執行者（線上 Host 禁自動 migrate），`Hosts.Dev` 僅開發用。
- Three guardrails (asserted by ArchTests): Hosts reference Application ports + DTOs only, never Domain entities or Infrastructure; one canonical exposure per use case; cross-cutting only in `HostBase`. 三護欄（ArchTests 斷言）：Hosts 僅引用 Application ports＋DTO，絕不碰 Domain 實體與 Infrastructure；每 use case 單一 canonical 暴露；橫切只進 `HostBase`。
- Contracts belong to Application; authorization decisions live in Application (Hosts only carry identity); format validation in Hosts, business-rule validation in Application. Contracts 歸 Application；判定只在 Application（Hosts 只搬身份）；格式驗證在 Hosts，業務規則驗證在 Application。
- Frontend shared packages (`domain`, `application`, `components`) never import framework modules; `components` holds component CORE only (pure-TS logic + tokens-only styles); Vue presenters live in `packages/vue` (@startspec/vue) as the ONLY framework seat — thin render glue over those contracts, so a future React/native presenter reuses the core without rewriting it; the design system is framework-free (`theme` tokens are pure CSS variables) and CSS-framework integrations compose over it (`theme/src/integrations/<framework>.css`, composed via `theme.css`) — swapping Tailwind for e.g. UnoCSS replaces only the integration file; styling consumes design tokens only; shared components talk via props/events with no store access. 前端共用包絕不引框架模組；`components` 僅存元件核心（純 TS 邏輯＋唯 tokens 樣式）；Vue presenter 層 `packages/vue` 為唯一框架座位——僅薄渲染膠水；設計系統免框架（tokens 為純 CSS 變數），CSS 框架整合以組合疊上（`integrations/<framework>.css` 經 `theme.css` 組合）——Tailwind 換 UnoCSS 只換整合檔；樣式只吃 tokens；元件僅經 props/events 通訊、不碰 store。
- Frontend ships per-org-unit app shells (`apps/hq`, `apps/store`, `apps/consumer`) over shared `packages/*`; each app declares packaging targets (Web first, Web+Tauri+native path open); platform capabilities go through application ports only. 前端按組織單位出應用殼，共用 packages；各應用宣告封裝目標（Web 先行，Tauri 路徑敞開）；平台能力只經 application ports。
- Guard tests precede the queries they guard (H1 precedent). 守衛測試先於被守衛的查詢（H1 前例）。
- `SharedKernel` admits only event bases, 3-use kernel value objects (promotion recorded), and cross-cutting ports; no entities, DTOs, or use-case logic. `SharedKernel` 僅收事件基底、3-use 核心值物件（晉升有記錄）、橫切埠；實體、DTO、用例邏輯禁入。
- Domain events carry the standard envelope and evolve additively; deploys are lockstep across Hosts until broker persistence or split (V2 trigger). 領域事件帶標準信封、加法演進；broker 持久化或拆分前部署跨 Hosts 鎖步（V2 觸發器）。
- Thin kernel = `SharedKernel` + cross-cutting ports + event envelope base + module admission rules; one product = one repo, and new products fork from the thin kernel on a shared baseline (additive contract surface, scheduled baseline bumps, bounded divergence with a CI drift alarm). 精簡核心＝`SharedKernel`＋橫切 ports＋事件信封基底＋模組准入規則；一商品一倉，新商品自 thin kernel fork，共享基線加法演進＋排程 baseline bump＋有界分歧窗掛 CI drift alarm。
- Four seams (persistence, event transport, Host+frontend composition, auth) keep port contract tests + fake + ArchTests; default obligation is contract-kept, swap-proven duties paid only when a second implementation is real; swap acceptance = contract tests + four gates green + `Hosts.Dev` rehearsal + rollback note; persistence swap = major. 四接縫（persistence、event transport、Host＋前端組裝、auth）維持 port contract tests＋fake＋ArchTests；預設 contract-kept，僅第二實作成真才付 swap-proven；驗收＝契約測試＋四閘門全綠＋`Hosts.Dev` 演練＋rollback note；persistence 替換＝major。
- Full text lives in the designs (`iam-foundation` decision 1, `store-onboarding` decision 6); this section is the index, not a copy — update designs first, then mirror here. With `platform-constitution` archived, the durable contract is `openspec/specs/platform/spec.md` — consult it before these pointers. 全文在 designs（`iam-foundation` 決策 1、`store-onboarding` 決策 6）；本節為索引非複本——先改 designs，再同步此處。`platform-constitution` 已歸檔，以 `openspec/specs/platform/spec.md` 為持久契約，優先於此處索引查閱。
- Monorepo with a single platform version (`version.json` is the sole source); backend `src/` and frontend `frontend/` ship together. Contracts evolve additively only; breaking changes are marked major with migration notes. CI has four gates (build + ArchTests + Reqnroll + lint) — all green before merge. 單倉＋單一平台版本（`version.json` 唯一真相），前後端同倉同發；契約僅加法演進，破壞性變更標 major＋遷移說明；CI 四閘門全綠才可合併。

## Collaboration protocol (P1 self-verify + evidence; full text in `docs/adr/008-*`)

- Tasks verify themselves: the 驗證 clause in `tasks.md` is the acceptance bar — no evidence means not done. Humans do not sign off per task. 任務自驗：`tasks.md` 驗證欄即驗收標準，無證據＝未完成；人不逐項驗收。
- Three human gates only: review planning artifacts, spot-check during apply, `review-work` before archive. Spot-check any completed task for re-verification; two consecutive failures return the whole batch. 人只守三閘門（審產物、抽查、archive 前 review）；可隨點重驗，連兩次失敗整批退回。
- AI hard bounds: no unasked commits, no deleting tests to pass, no guessing unread code, never leave broken state; after 3 consecutive failures stop, revert, consult oracle, ask. AI 硬邊界：禁未明示提交、禁刪測試求綠、禁臆測未讀代碼、禁留破損收工；三連敗停手還原問人。
- Progress is recognized only by `tasks.md` checkboxes + evidence, never by conversation memory. 進度只認核取＋證據，不認對話記憶。

## Environment

- Shell is Windows PowerShell (`pwsh`): use `Get-ChildItem -Force`, `Get-Content`; `ls -la`, `head`, `cat` do not exist. Shell 為 Windows PowerShell（`pwsh`）：使用 `Get-ChildItem -Force`、`Get-Content`；`ls -la`、`head`、`cat` 不存在。
- Run EF migrations via `scripts/Add-IamMigration.ps1 -Name <MigrationName>` (`-DryRun` prints the resolved command); it pins `--project src\Modules\Iam`, `--startup-project src\Hosts\Hosts.Migrator`, `--output-dir Infrastructure\Migrations` regardless of the caller's directory. 執行 EF 遷移一律用 `scripts/Add-IamMigration.ps1 -Name <遷移名稱>`（`-DryRun` 印出解析後指令）；路徑已固化，與呼叫端所在目錄無關。
- Test-suite invocations are intentionally NOT scripted yet; run them by hand and remember: Unit/Contract live in `src\tests\`, but `ArchTests` lives in `src\ArchTests` (not `src\tests\ArchTests` — MSB1009 pitfall), Reqnroll runs bootstrap Testcontainers Postgres, and every dotnet command needs the UTF-8 console prefix `[Console]::OutputEncoding = [Text.Encoding]::UTF8`. Consolidate these into a test wrapper script when the CI four gates (build + ArchTests + Reqnroll + lint) land. 四套測試指令刻意暫不腳本化：單元／契約在 `src\tests\`，`ArchTests` 在 `src\ArchTests`（不是 `src\tests\ArchTests`——MSB1009 地雷），Reqnroll 會啟動 Testcontainers Postgres，且每條 dotnet 指令需 UTF-8 console 前綴；待 CI 四閘門落地時再固化為測試腳本。
- Git `master` has zero commits; scaffold (`.opencode/`, `openspec/`) is untracked. Commit only when asked. Git `master` 尚無提交；鷹架（`.opencode/`、`openspec/`）未追蹤；僅在被要求時才提交。

## OMO (oh-my-openagent)

- Global plugin `oh-my-openagent` is installed; no project-local `opencode.json` needed. 全域插件 `oh-my-openagent` 已安裝；無需專案級 `opencode.json`。
- Use `ultrawork` / `ulw` keyword to trigger multi-agent orchestration; `/ulw-plan` then `/ulw-execute` for plan-first work. 以 `ultrawork` / `ulw` 關鍵字觸發多代理編排；計畫優先的工作先 `/ulw-plan` 再 `/ulw-execute`。
- `openspec/` remains source of truth; OMO agents must follow OpenSpec rules above. `openspec/` 仍為事實來源；OMO agents 必須遵守上述 OpenSpec 規則。
- No app code yet, so no hierarchical `src/**/AGENTS.md` needed. Add scoped `AGENTS.md` when new top-level code dirs appear. 尚無應用程式碼，故無需階層式 `src/**/AGENTS.md`；新增頂層程式碼目錄時再補 scoped `AGENTS.md`。
