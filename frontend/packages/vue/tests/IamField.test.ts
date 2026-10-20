import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import IamField from '../src/IamField.vue';

function mountField(props: { label?: string; error?: string }, slotHtml = '<input />') {
  return mount(IamField, { props, slots: { default: slotHtml } });
}

describe('<IamField>', () => {
  it('renders the label and the slotted control', () => {
    const wrapper = mountField({ label: '登入名稱' });
    expect(wrapper.find('label').text()).toBe('登入名稱');
    expect(wrapper.find('label input').exists()).toBe(true);
  });

  it('shows the error message only when the prop is present', () => {
    const failing = mountField({ label: '密碼', error: '不可為空白' });
    const alert = failing.find('[role="alert"]');
    expect(alert.exists()).toBe(true);
    expect(alert.text()).toBe('不可為空白');

    const passing = mountField({ label: '密碼' });
    expect(passing.find('[role="alert"]').exists()).toBe(false);
  });

  it('re-renders reactively when the label changes', async () => {
    const wrapper = mountField({ label: 'A' });
    await wrapper.setProps({ label: 'B' });
    expect(wrapper.find('label').text()).toBe('B');
  });
});
