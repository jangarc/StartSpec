/**
 * Display-only `<iam-button>` (task 7.3): zero framework imports; props via attributes (`variant`,
 * `disabled`), one semantic event (`iam-click`, bubbles + composed); consumes design tokens ONLY.
 * 展示型按鈕：props 走 attributes、單一語意事件 `iam-click`；樣式唯 tokens。
 */
import buttonStyles from './styles/iam-button.css?inline';

export const IAM_BUTTON_TAG = 'iam-button';

export class IamButton extends HTMLElement {
  static readonly observedAttributes = ['variant', 'disabled'];

  constructor() {
    super();
    const root = this.attachShadow({ mode: 'open' });
    const style = document.createElement('style');
    style.textContent = buttonStyles;
    root.appendChild(style);
    const button = document.createElement('button');
    button.type = 'button';
    const label = document.createElement('slot');
    button.appendChild(label);
    button.addEventListener('click', () => {
      if (this.hasAttribute('disabled')) {
        return;
      }
      this.dispatchEvent(new CustomEvent('iam-click', { bubbles: true, composed: true }));
    });
    root.appendChild(button);
  }
}

if (!customElements.get(IAM_BUTTON_TAG)) {
  customElements.define(IAM_BUTTON_TAG, IamButton);
}
