import { Action } from 'redux';
import { ValidationError } from 'yup';

import {
    ErrorAction, RequestAction, ResponseAction, TSError, ValidationResultAction,
} from './actions';

export type ActionWithoutType<T> = Pick<T, Exclude<keyof T, 'type'>>;

/**
 * Create an action creator for a generic action.
 * @param type Action type
 */
export function createActionCreator<T extends Action>(type: T['type']): (actionProps: ActionWithoutType<T>) => T {
  return (actionProps: ActionWithoutType<T>) => Object.assign(
    {} as T,  // TypeScript can't infer T from its parts because it is generic
    { type },
    actionProps,
  );
}

/**
 * Create an action creator for an action that contains a request property.
 * @param type Action type
 */
export function createRequestActionCreator<T extends RequestAction>(type: T['type']):
  (request: T['request']) => T {
  return (request: T['request']) => Object.assign(
    {} as T,  // TypeScript can't infer T from its parts because it is generic
    { type },
    { request },
  );
}

/**
 * Create an action creator for an action that contains a response property.
 * This function is exported for use by sagas.
 * @param type Action type
 */
export function createResponseActionCreator<T extends ResponseAction>(type: T['type']):
  (response: T['response']) => T {
  return (response: T['response']) => Object.assign(
    {} as T,  // TypeScript can't infer T from its parts because it is generic
    { type },
    { response },
  );
}

/**
 * Create an action creator for an action that contains an error property.
 * This function is exported for use by sagas.
 * @param type Action type
 */
export function createErrorActionCreator<T extends ErrorAction>(type: T['type']):
  (error: TSError) => T {
  return (error: TSError) => Object.assign(
    {} as T,  // TypeScript can't infer T from its parts because it is generic
    { type },
    { error },
  );
}

/**
 * Create an action creator for an action that contains a validation result.
 * This function is exported for use by sagas.
 * @param type Action type
 */
export function createValidationResultActionCreator
  <TInputName extends string, T extends ValidationResultAction>(type: T['type']):
  (inputName: TInputName, result: any) => T {
  return (inputName: TInputName, result: any) => Object.assign(
    {} as T,  // TypeScript can't infer T from its parts because it is generic
    { type },
    { inputName },
    { result },
  );
}

/**
 * Create an action creator for an action that contains a validation error.
 * This function is exported for use by sagas.
 * @param type Action type
 */
export function createValidationErrorActionCreator
  <TInputName extends string, T extends ValidationResultAction>(type: T['type']):
  (inputName: TInputName, result: ValidationError) => T {
  return (inputName: TInputName, result: ValidationError) => Object.assign(
    {} as T,  // TypeScript can't infer T from its parts because it is generic
    { type },
    { inputName },
    { result },
  );
}
