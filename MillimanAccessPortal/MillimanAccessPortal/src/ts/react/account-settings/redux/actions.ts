import { UserFull } from '../../models';

export type TSError = any;  // any by necessity due to the nature of try/catch in TypeScript

export interface SetPendingTextInputValue {
  type: 'SET_PENDING_TEXT_INPUT_VALUE';
  inputName: 'firstName' | 'lastName' | 'phone' | 'employer' | 'current' | 'new' | 'confirm';
  value: string;
}
export interface ResetForm {
  type: 'RESET_FORM';
}

export interface FetchUser {
  type: 'FETCH_USER';
  request: {};
}
export interface FetchUserSucceeded {
  type: 'FETCH_USER_SUCCEEDED';
  response: UserFull;
}
export interface FetchUserFailed {
  type: 'FETCH_USER_FAILED';
  error: TSError;
}

export type PageAction = SetPendingTextInputValue | ResetForm;
export type RequestAction = FetchUser;
export type ResponseAction = FetchUserSucceeded;
export type ErrorAction = FetchUserFailed;
export type ScheduleAction = never;
export type AccountAction = PageAction | RequestAction | ResponseAction | ErrorAction;

export function isScheduleAction(action: AccountAction): action is ScheduleAction {
  return false && action;
}
export function isErrorAction(action: AccountAction): action is ErrorAction {
  return (action as ErrorAction).error !== undefined;
}
