import { describe, expect, it } from 'vitest';
import { activationSuppressed, buttonClasses, cardClasses, deriveFieldViewModel, IAM_BUTTON_ACTIVATE_EVENT, toneModifier } from '../src';

describe('tone contract', () => {
  it('maps the design-system tone to the kit modifier class', () => {
    expect(toneModifier('ds-btn', 'danger')).toBe('ds-btn-danger');
    expect(toneModifier('ds-btn', 'secondary')).toBe('ds-btn-secondary');
    expect(toneModifier('ds-btn', 'primary')).toBe('');
    expect(toneModifier('ds-btn', undefined)).toBe('');
  });
});

describe('button class composition', () => {
  it('composes tone + outline + size from one helper', () => {
    expect(buttonClasses({})).toBe('ds-btn');
    expect(buttonClasses({ variant: 'danger' })).toBe('ds-btn ds-btn-danger');
    expect(buttonClasses({ variant: 'secondary', outline: true })).toBe('ds-btn ds-btn-secondary ds-btn-outline');
    expect(buttonClasses({ size: 'sm' })).toBe('ds-btn ds-btn-sm');
    expect(buttonClasses({ variant: 'success', outline: true, size: 'lg' })).toBe('ds-btn ds-btn-success ds-btn-outline ds-btn-lg');
  });

  it('still suppresses activation only when disabled', () => {
    expect(activationSuppressed({ disabled: true })).toBe(true);
    expect(activationSuppressed({})).toBe(false);
  });

  it('uses one stable activation event name', () => {
    expect(IAM_BUTTON_ACTIVATE_EVENT).toBe('iam-click');
  });
});

describe('card class composition', () => {
  it('composes tone + outline from one helper', () => {
    expect(cardClasses({})).toBe('ds-card');
    expect(cardClasses({ tone: 'warning' })).toBe('ds-card ds-card-warning');
    expect(cardClasses({ tone: 'info', outline: true })).toBe('ds-card ds-card-info ds-card-outline');
  });
});

describe('IamField core logic', () => {
  it('activates by default', () => {
    expect(activationSuppressed({})).toBe(false);
    expect(activationSuppressed({ variant: 'danger' })).toBe(false);
  });

  it('suppresses activation only when disabled', () => {
    expect(activationSuppressed({ disabled: true })).toBe(true);
    expect(activationSuppressed({ disabled: false })).toBe(false);
  });

  it('uses one stable activation event name', () => {
    expect(IAM_BUTTON_ACTIVATE_EVENT).toBe('iam-click');
  });
});

describe('IamField core logic', () => {
  it('derives a neutral view model without label or error', () => {
    expect(deriveFieldViewModel({})).toEqual({ labelText: '', errorText: '', hasError: false });
  });

  it('carries the label text', () => {
    expect(deriveFieldViewModel({ label: '登入名稱' }).labelText).toBe('登入名稱');
  });

  it('shows the error only when the prop is present', () => {
    expect(deriveFieldViewModel({ label: 'a', error: '' }).hasError).toBe(true);
    expect(deriveFieldViewModel({ label: 'a', error: '不可為空白' })).toEqual({
      labelText: 'a',
      errorText: '不可為空白',
      hasError: true,
    });
  });
});
