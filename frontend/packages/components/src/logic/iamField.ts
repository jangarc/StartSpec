/**
 * IamField core logic (task 7.3 rework, framework-free): derives a rendering-agnostic view model
 * from the props contract (label + error); presenters render it — no logic duplication per frame.
 * 欄位核心邏輯（零框架）：由 props 推導與渲染無關的 view model；presenter 只負責呈現。
 */
export interface IamFieldProps {
  readonly label?: string | undefined;
  readonly error?: string | undefined;
}

export interface FieldViewModel {
  readonly labelText: string;
  readonly errorText: string;
  readonly hasError: boolean;
}

/** Error renders ONLY when the prop is present（error 屬性在場才顯示、文字原樣帶出）。 */
export function deriveFieldViewModel(props: IamFieldProps): FieldViewModel {
  const hasError = typeof props.error === 'string';
  return {
    labelText: props.label ?? '',
    errorText: hasError ? (props.error as string) : '',
    hasError,
  };
}
