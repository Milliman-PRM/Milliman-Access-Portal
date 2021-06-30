import { ValidationError } from 'yup';

import { UserFull } from '../../models';
import { UserInputState } from './api';

export type TSError = any;  // any by necessity due to the nature of try/catch in TypeScript

type UserInputName =
  | 'firstName'
  | 'lastName'
  | 'phone'
  | 'employer'
  | 'timezone'
  ;
type InputName =
  | UserInputName
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
 * GET a bodiless response that serves as a session heartbeat.
 */
export interface FetchSessionCheck {
  type: 'FETCH_SESSION_CHECK';
  request: {};
}
export interface FetchSessionCheckSucceeded {
  type: 'FETCH_SESSION_CHECK_SUCCEEDED';
  response: {};
}
export interface FetchSessionCheckFailed {
  type: 'FETCH_SESSION_CHECK_FAILED';
  error: TSError;
}

/**
 * Fetch session check after a delay.
 */
export interface ScheduleSessionCheck {
  type: 'SCHEDULE_SESSION_CHECK';
  delay: number;
}

/**
 * POST:
 *   update user information
 */
export interface UpdateAccount {
  type: 'UPDATE_ACCOUNT';
  request: {
    user?: UserInputState,
  };
}
export interface UpdateAccountSucceeded {
  type: 'UPDATE_ACCOUNT_SUCCEEDED';
  response: UserFull;
}
export interface UpdateAccountFailed {
  type: 'UPDATE_ACCOUNT_FAILED';
  error: TSError;
}
export interface RequestPasswordReset {
  type: 'REQUEST_PASSWORD_RESET';
  request: {};
}
export interface RequestPasswordResetSucceeded {
  type: 'REQUEST_PASSWORD_RESET_SUCCEEDED';
  response: {
    successMessage: string;
  };
}
export interface RequestPasswordResetFailed {
  type: 'REQUEST_PASSWORD_RESET_FAILED';
  error: TSError;
}

/**
 * Validate the user section
 */
export interface ValidateInputUser {
  type: 'VALIDATE_INPUT_USER';
  value: UserInputState;
  inputName?: UserInputName;
}
export interface ValidateInputUserSucceeded {
  type: 'VALIDATE_INPUT_USER_SUCCEEDED';
  result: any;
  inputName?: UserInputName;
}
export interface ValidateInputUserFailed {
  type: 'VALIDATE_INPUT_USER_FAILED';
  result: ValidationError;
  inputName?: UserInputName;
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
  | ScheduleSessionCheck
  ;

/**
 * An action that makes an Ajax request.
 */
export type RequestAccountAction =
  | FetchUser
  | UpdateAccount
  | RequestPasswordReset
  | FetchSessionCheck
  ;

/**
 * An action that marks the succesful response of an Ajax request.
 */
export type ResponseAccountAction =
  | FetchUserSucceeded
  | UpdateAccountSucceeded
  | RequestPasswordResetSucceeded
  | FetchSessionCheckSucceeded
  ;

/**
 * An action that marks the errored response of an Ajax request.
 */
export type ErrorAccountAction =
  | FetchUserFailed
  | UpdateAccountFailed
  | RequestPasswordResetFailed
  | FetchSessionCheckFailed
  ;

/**
 * An action that marks a request for validation of an input field.
 */
export type ValidationAccountAction =
  | ValidateInputUser
  ;

/**
 * An action that marks the validation result for an input field.
 */
export type ValidationResultAccountAction =
  | ValidateInputUserSucceeded
  | ValidateInputUserFailed
  ;

/**
 * An action available to the account settings page.
 */
export type AccountAction =
  | PageAccountAction
  | ScheduleAccountAction
  | RequestAccountAction
  | ResponseAccountAction
  | ErrorAccountAction
  | ValidationAccountAction
  | ValidationResultAccountAction
  ;
