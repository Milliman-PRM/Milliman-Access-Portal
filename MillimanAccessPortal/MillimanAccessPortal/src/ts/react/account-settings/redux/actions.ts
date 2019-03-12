import { UserFull } from '../../models';

export type TSError = any;  // any by necessity due to the nature of try/catch in TypeScript

/**
 * Set the value of a form input
 */
export interface SetPendingTextInputValue {
  type: 'SET_PENDING_TEXT_INPUT_VALUE';
  inputName: 'firstName' | 'lastName' | 'phone' | 'employer' | 'current' | 'new' | 'confirm';
  value: string;
}

/**
 * Reset all form inputs to their original values
 */
export interface ResetForm {
  type: 'RESET_FORM';
}

/**
 * GET:
 *   current user information
 */
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

/**
 * An action that changes the state of the page.
 */
export type PageAccountAction =
  | SetPendingTextInputValue
  | ResetForm
  ;

/**
 * An action that schedules another action.
 */
export type ScheduleAccountAction =
  | never
  ;

/**
 * An action that makes an Ajax request.
 */
export type RequestAccountAction =
  | FetchUser
  ;

/**
 * An action that marks the succesful response of an Ajax request.
 */
export type ResponseAccountAction =
  | FetchUserSucceeded
  ;

/**
 * An action that marks the errored response of an Ajax request.
 */
export type ErrorAccountAction =
  | FetchUserFailed
  ;

/**
 * An action available to the account settings page.
 */
export type AccountAction =
  | PageAccountAction
  | RequestAccountAction
  | ResponseAccountAction
  | ErrorAccountAction
  ;
