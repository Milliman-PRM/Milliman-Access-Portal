export type TSError = any;  // any by necessity due to the nature of try/catch in TypeScript

export interface SetPendingTextInputValue {
  type: 'SET_PENDING_TEXT_INPUT_VALUE';
  inputName: 'firstName' | 'lastName' | 'phone' | 'employer' | 'current' | 'new' | 'confirm';
  value: string;
}
export interface ResetForm {
  type: 'RESET_FORM';
}

export type PageAction = SetPendingTextInputValue | ResetForm;
export type AccountAction = PageAction;
