/**
 * IamCard core logic (framework-free): the card is a presentational container — its "logic" is the
 * class composition over the shared tone scale + outline mode (regions are slots, presenter-side).
 * 卡片核心邏輯（零框架）：容器無狀態行為；邏輯＝色調尺度＋outline 的 class 組成；區域＝slot。
 */
import { toneModifier, type IamTone } from './tone';

export interface IamCardProps {
  readonly title?: string | undefined;
  readonly tone?: IamTone | undefined;
  readonly outline?: boolean | undefined;
}

/** The full class list a presenter must apply（class 組合的唯一真相）。 */
export function cardClasses(props: IamCardProps): string {
  const parts = ['ds-card', toneModifier('ds-card', props.tone)];
  if (props.outline === true) {
    parts.push('ds-card-outline');
  }
  return parts.filter((part) => part.length > 0).join(' ');
}
