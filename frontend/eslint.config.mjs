import tseslint from 'typescript-eslint';
import vue from 'eslint-plugin-vue';
import vueParser from 'vue-eslint-parser';

const FRAMEWORK_IMPORTS = {
  patterns: [
    { group: ['vue', 'vue/*', '@vue/*', 'vue-router', 'vue-router/*', 'pinia', 'pinia/*'], message: 'Shared packages are framework-free (task 7.5).' },
  ],
};

const BROWSER_API_IN_PURE_LAYERS = {
  globals: ['document', 'window', 'navigator', 'location', 'localStorage', 'sessionStorage', 'fetch', 'XMLHttpRequest', 'setTimeout', 'setInterval', 'requestAnimationFrame', 'customElements'],
};

const BROWSER_API_IN_COMPONENTS = {
  globals: ['fetch', 'XMLHttpRequest', 'localStorage', 'sessionStorage'],
};

export default tseslint.config(
  { ignores: ['**/node_modules/**', '**/dist/**', '**/coverage/**'] },
  ...tseslint.configs.recommended,
  ...vue.configs['flat/essential'],
  {
    files: ['**/*.vue'],
    languageOptions: {
      parser: vueParser,
      parserOptions: { parser: tseslint.parser },
    },
  },
  {
    // 純邏輯層（domain/application/shell）：禁框架、禁瀏覽器/原生 API 直調（port 契約為純 TS）。
    files: ['packages/domain/src/**/*.{ts,tsx}', 'packages/application/src/**/*.{ts,tsx}', 'packages/shell/src/**/*.{ts,tsx}'],
    rules: {
      'no-restricted-imports': ['error', FRAMEWORK_IMPORTS],
      'no-restricted-globals': ['error', BROWSER_API_IN_PURE_LAYERS],
    },
  },
  {
    // 展示層（components）：禁框架；DOMAPI 允許（自訂元素本職），資料面 API 仍禁直調。
    files: ['packages/components/src/**/*.{ts,tsx}'],
    rules: {
      'no-restricted-imports': ['error', FRAMEWORK_IMPORTS],
      'no-restricted-globals': ['error', BROWSER_API_IN_COMPONENTS],
    },
  },
);
