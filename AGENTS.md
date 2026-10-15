# AGENTS.md

Greenfield OpenSpec scaffold — no app code, build, test, lint, or CI exists yet. `openspec/` is the source of truth.

## Layout

- `openspec/config.yaml` — schema `spec-driven`, no `context` constraints set. Read it before planning.
- `openspec/specs/` — main specs (currently empty, only `.gitkeep`).
- `openspec/changes/<name>/` — active change: `proposal.md`, `specs/<capability>/spec.md` (delta only), `design.md`, `tasks.md`.
- `openspec/changes/archive/` — archived changes, do not edit.
- `.opencode/commands/opsx-*.md` — slash workflows (`/opsx-propose`, `/opsx-apply`, `/opsx-archive`, `/opsx-explore`, `/opsx-sync`); mirror of `.opencode/skills/openspec-*/SKILL.md`. Skill/command text is authoritative for that workflow.
- `.opencode/node_modules/` — `@opencode-ai/plugin` deps only; ignore.

## OpenSpec rules

- Never assume paths: resolve root via `openspec context --json` (`root.path`), artifact paths via `openspec status --change "<name>" --json` (`planningHome`, `changeRoot`, `artifactPaths`).
- Planning (`propose`) writes artifacts only — do not edit project code or start `apply` in the same turn.
- `apply` implements `tasks.md` checkboxes in order; flip `- [ ]` to `- [x]` at the exact `sourcePath`/`line` from `openspec instructions apply --change "<name>" --json`, then re-run instructions to confirm progress.
- `design.md` is conditional (create only if `instructions` says so); `specs` delta is required unless `status` reports it `skipped` via `skip_specs`. `status: done` on `tasks` does not imply dependencies exist — walk `requires` edges transitively.
- Do not copy `context`/`rules`/`operationGuidance` from `instructions` output into artifacts or code.
- Verify with `openspec validate --change "<name>"` (or `--specs`) before `openspec archive <name>`; `archive` syncs deltas into `openspec/specs/`.

## Environment

- Shell is Windows PowerShell (`pwsh`): use `Get-ChildItem -Force`, `Get-Content`; `ls -la`, `head`, `cat` do not exist.
- Git `master` has zero commits; scaffold (`.opencode/`, `openspec/`) is untracked. Commit only when asked.

## OMO (oh-my-openagent)

- Global plugin `oh-my-openagent` is installed; no project-local `opencode.json` needed.
- Use `ultrawork` / `ulw` keyword to trigger multi-agent orchestration; `/ulw-plan` then `/ulw-execute` for plan-first work.
- `openspec/` remains source of truth; OMO agents must follow OpenSpec rules above.
- No app code yet, so no hierarchical `src/**/AGENTS.md` needed. Add scoped `AGENTS.md` when new top-level code dirs appear.
