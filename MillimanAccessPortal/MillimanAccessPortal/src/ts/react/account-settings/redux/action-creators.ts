import { Action } from 'redux';

import * as AccountActions from './actions';
import { TSError } from './actions';

export type ActionWithoutType<T> = Pick<T, Exclude<keyof T, 'type'>>;

function createActionCreator<T extends Action>(type: T['type']): (actionProps: ActionWithoutType<T>) => T {
  return (actionProps: ActionWithoutType<T>) => Object.assign(
    {} as T,  // TypeScript can't infer T from its parts because it is generic
    { type },
    actionProps,
  );
}

export const setPendingTextInputValue =
  createActionCreator<AccountActions.SetPendingTextInputValue>('SET_PENDING_TEXT_INPUT_VALUE');
