/**
 * IamButton core logic (task 7.3 rework, framework-free): prop semantics + the one semantic
 * activation event name; presenters (Vue today; React/native path open) render this contract and
 * own nothing of the decision logic. Variant = the design-system tone; `outline` switches the
 * tone-mode (transparent fill, tone frame/text); `size` = sm/md(base)/lg; props declare
 * `| undefined` explicitly so Vue's reactive props object is assignable under
 * exactOptionalPropertyTypes.
 * 按鈕核心邏輯（零框架）：variant＝設計系統色調；outline＝色調外框模式；size＝sm/md/lg。
 */
import { toneModifier, type IamTone } from './tone';

export type IamButtonVariant = IamTone;

export type IamButtonSize = 'sm' | 'lg';

export const IAM_BUTTON_ACTIVATE_EVENT = 'iam-click';

export interface IamButtonProps {
  readonly variant?: IamButtonVariant | undefined;
  readonly outline?: boolean | undefined;
  readonly size?: IamButtonSize | undefined;
  readonly disabled?: boolean | undefined;
}

/** The full class list a presenter must apply（class 組合的唯一真相——presenter 不自行拼接）。 */
export function buttonClasses(props: IamButtonProps): string {
  const parts = ['ds-btn', toneModifier('ds-btn', props.variant)];
  if (props.outline === true) {
    parts.push('ds-btn-outline');
  }
  if (props.size) {
    parts.push(`ds-btn-${props.size}`);
  }
  return parts.filter((part) => part.length > 0).join(' ');
}

/** A disabled button never activates（disabled 即禁觸發——presenter 也必須遵守）。 */
export function activationSuppressed(props: IamButtonProps): boolean {
  return props.disabled === true;
}
