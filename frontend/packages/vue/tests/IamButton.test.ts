import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import IamButton from '../src/IamButton.vue';

describe('<IamButton>', () => {
  it('renders the slot and emits iam-click on click', async () => {
    const wrapper = mount(IamButton, { slots: { default: '儲存' } });
    expect(wrapper.text()).toBe('儲存');
    expect(wrapper.find('button').classes()).toEqual(['ds-btn']);
    await wrapper.find('button').trigger('click');
    expect(wrapper.emitted('iam-click')).toBeTruthy();
  });

  it('suppresses the event when disabled', async () => {
    const wrapper = mount(IamButton, { props: { disabled: true }, slots: { default: '存' } });
    await wrapper.find('button').trigger('click');
    expect(wrapper.emitted('iam-click')).toBeUndefined();
  });

  it('composes tone / outline / size classes via the core helper', () => {
    const danger = mount(IamButton, { props: { variant: 'danger' } });
    expect(danger.find('button').classes()).toEqual(expect.arrayContaining(['ds-btn', 'ds-btn-danger']));

    const outlineSecondary = mount(IamButton, { props: { variant: 'secondary', outline: true } });
    expect(outlineSecondary.find('button').classes()).toEqual(expect.arrayContaining(['ds-btn', 'ds-btn-secondary', 'ds-btn-outline']));

    const large = mount(IamButton, { props: { size: 'lg' } });
    expect(large.find('button').classes()).toEqual(expect.arrayContaining(['ds-btn', 'ds-btn-lg']));

    const small = mount(IamButton, { props: { size: 'sm' } });
    expect(small.find('button').classes()).toEqual(expect.arrayContaining(['ds-btn', 'ds-btn-sm']));
  });
});
