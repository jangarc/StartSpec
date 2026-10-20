/**
 * Component CORE — cross-component tone contract, framework-free (the Bootstrap-5-like scale):
 * tones are a DESIGN-SYSTEM vocabulary (theme semantic layer), never a component's private enum;
 * every component that colors itself takes a `tone` from here, and the kit css supplies the
 * `ds-{component}-{tone}` classes. PRIMARY is the base default (no modifier class emitted).
 * 跨元件色調契約（零框架）：primary 即元件基準預設（不出 modifier class）；擴充＝tokens＋本型別＋kit css。
 */
export type IamTone =
  | 'primary'
  | 'secondary'
  | 'success'
  | 'warning'
  | 'danger'
  | 'info'
  | 'light'
  | 'dark';

/** The unique modifier class a component gets for its tone: primary = base default (no class). */
export function toneModifier(prefix: string, tone: IamTone | undefined): string {
  return tone && tone !== 'primary' ? `${prefix}-${tone}` : '';
}
