import { THEME_NAMES, type ThemeName, type ThemePort } from '@startspec/application';

/**
 * Browser adapter of ThemePort (design decision 2 form): theme lives on <html data-theme>,
 * persists via localStorage, and falls back to the OS color-scheme preference for first visits.
 * ThemePort 的瀏覽器轉接器：data-theme＋localStorage＋OS 色系偏好偵測。
 */
const STORAGE_KEY = 'startspec-theme';
const THEMES = new Set<string>(THEME_NAMES);

function normalizeCandidate(value: string | null): ThemeName | null {
  return value !== null && THEMES.has(value) ? (value as ThemeName) : null;
}

export function createBrowserThemePort(): ThemePort {
  return {
    current(): ThemeName {
      const stored = normalizeCandidate(window.localStorage.getItem(STORAGE_KEY));
      if (stored) {
        return stored;
      }
      return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    },
    apply(name: ThemeName): void {
      document.documentElement.dataset.theme = name;
      window.localStorage.setItem(STORAGE_KEY, name);
    },
  };
}
