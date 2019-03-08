import { Action } from 'redux';

import * as AccountActions from './actions';
import { ErrorAction, RequestAction, ResponseAction, TSError } from './actions';

export type ActionWithoutType<T> = Pick<T, Exclude<keyof T, 'type'>>;

function createActionCreator<T extends Action>(type: T['type']): (actionProps: ActionWithoutType<T>) => T {
  return (actionProps: ActionWithoutType<T>) => Object.assign(
    {} as T,  // TypeScript can't infer T from its parts because it is generic
    { type },
    actionProps,
  );
}
function createRequestActionCreator<T extends RequestAction>(type: T['type']):
  (request: T['request']) => T {
  return (request: T['request']) => Object.assign(
    {} as T,  // TypeScript can't infer T from its parts because it is generic
    { type },
    { request },
  );
}
export function createResponseActionCreator<T extends ResponseAction>(type: T['type']):
  (response: T['response']) => T {
  return (response: T['response']) => Object.assign(
    {} as T,  // TypeScript can't infer T from its parts because it is generic
    { type },
    { response },
  );
}
export function createErrorActionCreator<T extends ErrorAction>(type: T['type']):
  (error: TSError) => T {
  return (error: TSError) => Object.assign(
    {} as T,  // TypeScript can't infer T from its parts because it is generic
    { type },
    { error },
  );
}

export const setPendingTextInputValue =
  createActionCreator<AccountActions.SetPendingTextInputValue>('SET_PENDING_TEXT_INPUT_VALUE');
export const resetForm =
  createActionCreator<AccountActions.ResetForm>('RESET_FORM');

export const fetchUser =
  createRequestActionCreator<AccountActions.FetchUser>('FETCH_USER');
