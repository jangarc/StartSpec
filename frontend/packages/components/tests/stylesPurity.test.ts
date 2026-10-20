/**
 * Token purity guard (task 7.3/7.5 double-guard): every color/typography/spacing declaration in a
 * shared component style MUST consume an `--iam-*` design token (raw hex / px literals forbidden).
 * 檢查共用元件樣式宣告只吃 `--iam-*` tokens（禁原始 hex／px 字面值）。
 */
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import path from 'node:path';
import { describe, expect, it } from 'vitest';

const stylesDir = path.join(path.dirname(fileURLToPath(import.meta.url)), '..', 'src', 'styles');
/** token-bound property declarations（色彩/字級/留白類） */
const TOKEN_BOUND_PROPERTIES = /(?:background-color|border-color|color|font-size|font-weight|padding|margin|gap|border-radius)\s*:\s*([^;{]+);/g;
const ALLOWED_RAW_VALUES = /(?:^|\s)(?:inherit|transparent|none|0)\b/;

function offendersOf(css: string): string[] {
  const offenders: string[] = [];
  for (const match of css.matchAll(TOKEN_BOUND_PROPERTIES)) {
    const declaration = match[0];
    const prop = declaration.slice(0, declaration.indexOf(':'));
    const value = match[1] ?? '';
    if (!value.includes('var(--iam-') && !ALLOWED_RAW_VALUES.test(value)) {
      offenders.push(`${prop.trim()}: ${value.trim()}`);
    }
  }
  return offenders;
}

describe('component styles consume design tokens only', () => {
  it('iam-button.css has no raw color/spacing literal', () => {
    const css = readFileSync(path.join(stylesDir, 'iam-button.css'), 'utf8');
    expect(offendersOf(css)).toEqual([]);
  });

  it('iam-field.css has no raw color/spacing literal', () => {
    const css = readFileSync(path.join(stylesDir, 'iam-field.css'), 'utf8');
    expect(offendersOf(css)).toEqual([]);
  });
});
