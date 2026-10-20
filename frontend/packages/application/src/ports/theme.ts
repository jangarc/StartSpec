/**
 * ThemePort (platform capability, application layer): theme switching goes through this port only;
 * apps inject a browser/native adapter — the shared layers never touch DOM APIs (7.5 lint).
 * 主題是平台能力：只經此 port；各 app 注入轉接器（Web 用 data-theme＋localStorage）。
 */
export type ThemeName = 'light' | 'dark' | 'synthwave';

export const THEME_NAMES: readonly ThemeName[] = ['light', 'dark', 'synthwave'];

export interface ThemePort {
  current(): ThemeName;
  apply(name: ThemeName): void;
}
