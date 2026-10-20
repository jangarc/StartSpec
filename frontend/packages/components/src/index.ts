/**
 * Display-component CORE (task 7.3 rework): framework-free logic — presenters (packages/vue today;
 * React/native path open) render these contracts; apps talk via props/events. The tone scale is a
 * design-system vocabulary shared by all components (IamTone), never a component-private enum.
 * 展示元件核心：零框架邏輯；色調尺度為設計系統詞彙（IamTone），絕非元件私有列舉。
 */
export {
  buttonClasses,
  IAM_BUTTON_ACTIVATE_EVENT,
  activationSuppressed,
  type IamButtonProps,
  type IamButtonSize,
  type IamButtonVariant,
} from './logic/iamButton';
export { cardClasses, type IamCardProps } from './logic/iamCard';
export { deriveFieldViewModel, type FieldViewModel, type IamFieldProps } from './logic/iamField';
export { toneModifier, type IamTone } from './logic/tone';
