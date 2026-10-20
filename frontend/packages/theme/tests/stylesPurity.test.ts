/**
 * Token purity guard (design-system swap principle): kit classes on token-bound properties must
 * consume semantic aliases or the per-element tone variables (whose values are themselves token
 * references); raw literals are forbidden — exactly the whitelist stylelint enforces, mirrored at
 * declaration level. Also guards tone/region/structure rules presenters rely on.
 * 檢查 kit css 的 token 綁定宣告只吃 tokens；與 stylelint 白名單同構的 funnel 現在 testing 鏡射。
 */
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import path from 'node:path';
import { describe, expect, it } from 'vitest';

const baseDir = path.join(path.dirname(fileURLToPath(import.meta.url)), '..', 'src');

const TONES = ['primary', 'secondary', 'success', 'warning', 'danger', 'info', 'light', 'dark'] as const;

/** Token-bound properties under check（與 stylelint 白名單同集合） */
const PROPERTY_WHITELIST = /^(background-color|border-color|color|font-size|font-weight|padding|padding-bottom|padding-top|margin|gap|border-radius|--(btn|card)-tone[a-z-]*)$/;
/** Declarations written as `prop: value;`（先剝註解、再逐一掃） */
const DECLARATION = /([a-zA-Z][a-zA-Z0-9-]*)\s*:\s*([^;{}]+);/g;
const ALLOWED_RAW_VALUES = /(?:^|\s)(?:inherit|transparent|none|0)\b/;
/** Legal var() targets: any semantic-layer variable + per-element tone relays（與 stylelint 白名單同構） */
const TOKEN_VARS = /var\(--iam-|var\(--(?:btn|card)-tone/;

function offendersOf(css: string): string[] {
  const stripped = css.replace(/\/\*[\s\S]*?\*\//g, '');
  const offenders: string[] = [];
  for (const match of stripped.matchAll(DECLARATION)) {
    const prop = match[1] ?? '';
    if (!PROPERTY_WHITELIST.test(prop)) {
      continue;
    }
    const value = match[2] ?? '';
    if (!TOKEN_VARS.test(value) && !ALLOWED_RAW_VALUES.test(value)) {
      offenders.push(`${prop}: ${value.trim()}`);
    }
  }
  return offenders;
}

const btnCss = () => readFileSync(path.join(baseDir, 'components', 'ds-btn.css'), 'utf8');
const cardCss = () => readFileSync(path.join(baseDir, 'components', 'ds-card.css'), 'utf8');
const semanticCss = () => readFileSync(path.join(baseDir, 'semantic.css'), 'utf8');

describe('kit css consumes tokens only (stylelint whitelist mirrored)', () => {
  const files = ['ds-btn.css', 'ds-card.css', 'ds-field.css', 'ds-alert.css'];
  for (const file of files) {
    it(`${file} has no raw literal on token-bound properties`, () => {
      const css = readFileSync(path.join(baseDir, 'components', file), 'utf8');
      expect(offendersOf(css)).toEqual([]);
    });
  }

  it('semantic.css defines every alias as a primitive reference (no raw values in the semantic layer)', () => {
    const semantic = semanticCss();
    const definitions = [...semantic.matchAll(/--iam-[a-z-]+\s*:\s*([^;]+);/g)]
      .filter((match) => !match[1].includes('var('))
      .map((match) => match[0]);
    expect(definitions).toEqual([]);
  });
});

describe('kit tone coverage (every IamTone has a base class)', () => {
  it('ds-btn.css defines a base rule for every non-default tone (primary is the base default)', () => {
    const css = btnCss();
    const missing = TONES.filter((tone) => tone !== 'primary' && !css.includes(`.ds-btn-${tone} `) && !css.includes(`.ds-btn-${tone}{`) && !css.includes(`.ds-btn-${tone} {`));
    expect(missing).toEqual([]);
  });

  it('ds-card.css defines a base rule for every non-default tone (primary is the base default)', () => {
    const css = cardCss();
    const missing = TONES.filter((tone) => tone !== 'primary' && !css.includes(`.ds-card-${tone} `) && !css.includes(`.ds-card-${tone}{`) && !css.includes(`.ds-card-${tone} {`));
    expect(missing).toEqual([]);
  });

  it('semantic.css defines base/strong/contrast for every tone', () => {
    const semantic = semanticCss();
    const missing = TONES.flatMap((tone) =>
      ['', '-strong', '-contrast'].filter((suffix) => !semantic.includes(`--iam-tone-${tone}${suffix}:`)),
    );
    expect(missing).toEqual([]);
  });
});

describe('kit structure the presenters rely on', () => {
  it('btn outline mode + sizes exist with their state pattern', () => {
    const css = btnCss();
    expect(css.includes('.ds-btn-outline {')).toBe(true);
    expect(css.includes('.ds-btn-outline:hover')).toBe(true);
    expect(css.includes('.ds-btn-sm {')).toBe(true);
    expect(css.includes('.ds-btn-lg {')).toBe(true);
  });

  it('card header/footer regions + outline mode exist', () => {
    const css = cardCss();
    expect(css.includes('.ds-card-header {')).toBe(true);
    expect(css.includes('.ds-card-footer {')).toBe(true);
    expect(css.includes('.ds-card-outline {')).toBe(true);
    expect(css.includes('.ds-card-outline .ds-card-title')).toBe(true);
  });

  it('cards/buttons consume only their per-element tone variables', () => {
    const btn = btnCss();
    expect(btn.includes('var(--btn-tone)')).toBe(true);
    expect(btn.includes('var(--btn-tone-strong)')).toBe(true);
    expect(btn.includes('var(--btn-tone-contrast)')).toBe(true);
    const card = cardCss();
    expect(card.includes('var(--card-tone)')).toBe(true);
  });
});
