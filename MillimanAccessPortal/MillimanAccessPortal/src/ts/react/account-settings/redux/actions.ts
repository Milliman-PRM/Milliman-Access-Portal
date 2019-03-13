import { ValidationError } from 'yup';

import { UserFull } from '../../models';
import { PasswordInputState, UserInputState } from './api';

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
 * Validate the user section
 */
export interface ValidateInputUser {
  type: 'VALIDATE_INPUT_USER';
  value: UserInputState;
}
export interface ValidateInputUserSucceeded {
  type: 'VALIDATE_INPUT_USER_SUCCEEDED';
  result: any;
}
export interface ValidateInputUserFailed {
  type: 'VALIDATE_INPUT_USER_FAILED';
  result: ValidationError;
}

/**
 * Validate the password section
 */
export interface ValidateInputPassword {
  type: 'VALIDATE_INPUT_PASSWORD';
  value: PasswordInputState;
}
export interface ValidateInputPasswordSucceeded {
  type: 'VALIDATE_INPUT_PASSWORD_SUCCEEDED';
  result: any;
}
export interface ValidateInputPasswordFailed {
  type: 'VALIDATE_INPUT_PASSWORD_FAILED';
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
 * An action that marks a request for validation of an input field.
 */
export type ValidationAccountAction =
  | ValidateInputUser
  | ValidateInputPassword
  ;

/**
 * An action that marks the validation result for an input field.
 */
export type ValidationResultAccountAction =
  | ValidateInputUserSucceeded
  | ValidateInputUserFailed
  | ValidateInputPasswordSucceeded
  | ValidateInputPasswordFailed
  ;

/**
 * An action available to the account settings page.
 */
export type AccountAction =
  | PageAccountAction
  | RequestAccountAction
  | ResponseAccountAction
  | ErrorAccountAction
  | ValidationAccountAction
  | ValidationResultAccountAction
  ;
