export default {
  rules: {},
  overrides: [
    {
      // tokens.css 只准自訂屬性定義（raw 值定義是 tokens 本職，不套 whitelist）
      files: ['**/packages/theme/src/*.css'],
      rules: {
        'no-empty-source': true,
        'property-no-unknown': true,
      },
    },
    {
      // 樣式唯 tokens（task 7.5）：共用元件樣式與 app 樣式，色彩/字級/留白宣告只准 `var(--iam-*)`。
      files: ['**/packages/components/src/styles/*.css', '**/apps/**/*.css'],
      rules: {
        'declaration-property-value-allowed-list': {
          '/^(background-color|color|border-color|font-size|font-weight|padding|margin|gap|border-radius)$/': [
            '/^var\\(--iam-/',
            'inherit',
            'transparent',
            'none',
            '0',
          ],
        },
      },
    },
  ],
};
