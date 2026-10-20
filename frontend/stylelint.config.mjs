export default {
  rules: {},
  overrides: [
    {
      // tokens.css／semantic.css 是 tokens 定義層（raw 值定義是它們的本職）；其餘 css 只准消費 var(--iam-*)
      files: ['**/packages/theme/src/tokens.css', '**/packages/theme/src/semantic.css'],
      rules: {
        'no-empty-source': true,
        'property-no-unknown': true,
      },
    },
    {
      // 樣式唯 tokens（swap principle）：kit 消費層與整合層＋app 樣式，機腕宣告只准 `var(--iam-*)`。
      files: [
        '**/packages/theme/src/components/*.css',
        '**/packages/theme/src/integrations/*.css',
        '**/packages/components/src/**/*.css',
        '**/apps/*/src/**/*.css',
      ],
      rules: {
        'declaration-property-value-allowed-list': {
          '/^(background-color|color|border-color|font-size|font-weight|padding|margin|gap|border-radius|--(btn|card)-tone[a-z-]*)$/': [
            '/^var\\(--iam-/',
            '/^var\\(--btn-tone-?/', // kit 中繼變數：值來自 --iam-tone-*（守衛測試鎖定變數值來源）
            '/^var\\(--card-tone-?/',
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
