import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import IamCard from '../src/IamCard.vue';

describe('<IamCard>', () => {
  it('renders the kit card class and slotted body', () => {
    const wrapper = mount(IamCard, { slots: { default: '<p>工作內容</p>' } });
    expect(wrapper.find('section').classes()).toEqual(['ds-card']);
    expect(wrapper.text()).toContain('工作內容');
  });

  it('renders the header region only when title or header slot is provided', () => {
    const titled = mount(IamCard, { props: { title: '工作階段' } });
    expect(titled.find('.ds-card-header').exists()).toBe(true);
    expect(titled.find('.ds-card-header').text()).toBe('工作階段');

    const bare = mount(IamCard);
    expect(bare.find('.ds-card-header').exists()).toBe(false);
  });

  it('lets a custom header slot override the title text', () => {
    const wrapper = mount(IamCard, { props: { title: '預設標題' }, slots: { header: '<span>客製頁首</span>' } });
    expect(wrapper.find('.ds-card-header').text()).toBe('客製頁首');
  });

  it('renders the footer region only when the footer slot is provided', () => {
    const withFooter = mount(IamCard, { slots: { footer: '<button>操作</button>' } });
    expect(withFooter.find('.ds-card-footer').exists()).toBe(true);

    const withoutFooter = mount(IamCard);
    expect(withoutFooter.find('.ds-card-footer').exists()).toBe(false);
  });

  it('composes tone and outline classes via the core helper', () => {
    const outlined = mount(IamCard, { props: { tone: 'info', outline: true, title: 't' } });
    expect(outlined.find('section').classes()).toEqual(expect.arrayContaining(['ds-card', 'ds-card-info', 'ds-card-outline']));
  });

  it('falls through host classes onto the card root', () => {
    const wrapper = mount(IamCard, { attrs: { class: 'mx-6 my-4' } });
    expect(wrapper.find('section').classes()).toEqual(expect.arrayContaining(['ds-card', 'mx-6', 'my-4']));
  });
});
