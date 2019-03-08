export type TSError = any;  // any by necessity due to the nature of try/catch in TypeScript

export interface SetPendingTextInputValue {
  type: 'SET_PENDING_TEXT_INPUT_VALUE';
  inputName: 'firstName' | 'lastName' | 'phone' | 'employer' | 'currentPassword' | 'newPassword' | 'confirmPassword',
  value: string;
}

export type AccountAction = SetPendingTextInputValue;
