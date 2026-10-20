/**
 * Display-only `<iam-field>` (task 7.3): zero framework imports; props via attributes (`label`,
 * `error`), a single slot for the control input, no events, no store/router — tokens only styles.
 * 展示型表單欄位：props 走 attributes、slot 承接控制項、無事件、不碰 store/router；樣式唯 tokens。
 */
import fieldStyles from './styles/iam-field.css?inline';

export const IAM_FIELD_TAG = 'iam-field';

export class IamField extends HTMLElement {
  static readonly observedAttributes = ['label', 'error'];

  #label!: HTMLLabelElement;
  #error!: HTMLDivElement;
  #slot!: HTMLSlotElement;

  constructor() {
    super();
    const root = this.attachShadow({ mode: 'open' });
    const style = document.createElement('style');
    style.textContent = fieldStyles;
    root.appendChild(style);

    const label = document.createElement('label');
    this.#slot = document.createElement('slot');
    label.appendChild(this.#slot);
    this.#label = label;
    root.appendChild(label);

    const error = document.createElement('div');
    error.className = 'error';
    error.setAttribute('role', 'alert');
    error.hidden = true;
    this.#error = error;
    root.appendChild(error);
  }

  connectedCallback(): void {
    this.#sync();
  }

  attributeChangedCallback(): void {
    this.#sync();
  }

  #sync(): void {
    this.#label.textContent = this.getAttribute('label') ?? '';
    this.#label.appendChild(this.#slot);
    const message = this.getAttribute('error');
    this.#error.textContent = message ?? '';
    this.#error.hidden = message === null;
  }
}

if (!customElements.get(IAM_FIELD_TAG)) {
  customElements.define(IAM_FIELD_TAG, IamField);
}
