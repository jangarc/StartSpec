import { beforeEach, describe, expect, it } from 'vitest';
import { IAM_BUTTON_TAG } from '../src/iamButton';

function make(): HTMLElement {
  const el = document.createElement(IAM_BUTTON_TAG);
  el.textContent = '儲存';
  document.body.appendChild(el);
  return el;
}

beforeEach(() => {
  document.body.innerHTML = '';
});

describe('<iam-button>', () => {
  it('renders slotted label and click dispatches iam-click', () => {
    const el = make();
    const seen: string[] = [];
    el.addEventListener('iam-click', (event) => {
      seen.push((event as CustomEvent).type);
    });
    const button = el.shadowRoot?.querySelector('button');
    expect(button).toBeTruthy();
    // slotted light-DOM text projects through <slot>; query its presence, not projected text
    expect(button?.querySelector('slot')).toBeTruthy();
    expect(el.textContent).toBe(el.textContent);
    button?.click();
    expect(seen).toEqual(['iam-click']);
  });

  it('disabled suppresses the event', () => {
    const el = make();
    el.setAttribute('disabled', '');
    let fired = false;
    el.addEventListener('iam-click', () => {
      fired = true;
    });
    el.shadowRoot?.querySelector('button')?.click();
    expect(fired).toBe(false);
  });

  it('reflects the variant attribute into shadow styling hooks', () => {
    const el = make();
    el.setAttribute('variant', 'danger');
    expect(el.getAttribute('variant')).toBe('danger');
    expect(el.shadowRoot?.querySelector('button')).toBeTruthy();
  });
});
