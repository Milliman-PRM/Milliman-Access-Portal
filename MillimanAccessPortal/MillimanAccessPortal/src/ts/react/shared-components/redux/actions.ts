import { Action } from 'redux';
import { ValidationError } from 'yup';

/**
 * Error type alias.
 * Aliased as any by necessity due to the nature of try/catch in TypeScript.
 */
export type TSError = any;

export interface ScheduleAction extends Action {
  delay: number;
}
export interface RequestAction<TRequest extends {} = any> extends Action {
  request: TRequest;
}
export interface ResponseAction<TResponse extends {} = any> extends Action {
  response: TResponse;
}
export interface ErrorAction extends Action {
  error: TSError;
}
export interface ValidationAction extends Action {
  value: any;
  inputName?: string;
}
export interface ValidationResultAction extends Action {
  result: any | ValidationError;
  inputName?: string;
}

/**
 * Schedule action type guard.
 * @param action An action to inspect.
 */
export function isScheduleAction(action: Action): action is ScheduleAction {
  return (action as ScheduleAction).delay !== undefined;
}
/**
 * Request action type guard.
 * @param action An action to inspect.
 */
export function isRequestAction(action: Action): action is RequestAction {
  return (action as RequestAction).request !== undefined;
}
/**
 * Response action type guard.
 * @param action An action to inspect.
 */
export function isResponseAction(action: Action): action is ResponseAction {
  return (action as ResponseAction).response !== undefined;
}
/**
 * Error action type guard.
 * @param action An action to inspect.
 */
export function isErrorAction(action: Action): action is ErrorAction {
  return (action as ErrorAction).error !== undefined;
}
