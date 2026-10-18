# ADR Preferences

preferred-style: custom

Custom style note: 沿用 repo 既有 docs/adr/001-009 風格——繁中為主、標籤雙語（# ADR-XXX：繁中標題（English title））、章節 Status/Date/Context/Decision/Rationale/Consequences（標題附繁中括註）、Status 行為「- Status（狀態）：Accepted（接受）」。

When `preferred-style` is `unset`, ask the user which ADR style they prefer before drafting an ADR, then update this file with one of:

- `madr-full`
- `madr-minimal`
- `nygard`
- `y-statement`
- `custom`

If the user chooses `custom`, record a short note describing the preferred structure.