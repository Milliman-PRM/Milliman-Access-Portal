import { ValidationError } from 'yup';

import { UserFull } from '../../models';

export type TSError = any;  // any by necessity due to the nature of try/catch in TypeScript

type InputName =
  | 'firstName'
  | 'lastName'
  | 'phone'
  | 'employer'
  | 'current'
  | 'new'
  | 'confirm'
  ;

/**
 * Set the value of a form input
 */
export interface SetPendingTextInputValue {
  type: 'SET_PENDING_TEXT_INPUT_VALUE';
  inputName: InputName;
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
 * Validate a form input
 */
export interface ValidateInput {
  type: 'VALIDATE_INPUT';
  inputName: InputName;
  value: string;
}
export interface ValidateInputSucceeded {
  type: 'VALIDATE_INPUT_SUCCEEDED';
  inputName: InputName;
  result: any;
}
export interface ValidateInputFailed {
  type: 'VALIDATE_INPUT_FAILED';
  inputName: InputName;
  result: ValidationError;
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
