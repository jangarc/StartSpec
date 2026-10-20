import { beforeEach, describe, expect, it } from 'vitest';
import { IAM_FIELD_TAG } from '../src/iamField';

beforeEach(() => {
  document.body.innerHTML = '';
});

function make(attrs: Record<string, string> = {}): HTMLElement {
  const el = document.createElement(IAM_FIELD_TAG);
  for (const [key, value] of Object.entries(attrs)) {
    el.setAttribute(key, value);
  }
  const input = document.createElement('input');
  el.appendChild(input);
  document.body.appendChild(el);
  return el;
}

describe('<iam-field>', () => {
  it('renders the label attribute and slotted control', () => {
    const el = make({ label: '登入名稱' });
    expect(el.shadowRoot?.querySelector('label')?.textContent).toBe('登入名稱');
    expect(el.shadowRoot?.querySelector('slot')?.assignedElements().length).toBe(1);
  });

  it('shows the error message only when the attribute is set', () => {
    const withError = make({ label: '密碼', error: '不可為空白' });
    const error = withError.shadowRoot?.querySelector('[role="alert"]');
    expect(error?.textContent).toBe('不可為空白');
    expect(error?.hidden).toBe(false);

    const withoutError = make({ label: '密碼' });
    expect(withoutError.shadowRoot?.querySelector('[role="alert"]')?.hidden).toBe(true);
  });

  it('updates reactively when attributes change', () => {
    const el = make({ label: 'A' });
    el.setAttribute('label', 'B');
    expect(el.shadowRoot?.querySelector('label')?.textContent).toBe('B');
  });
});
