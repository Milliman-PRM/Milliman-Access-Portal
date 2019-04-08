import { Action } from 'redux';

/**
 * An object of actions and their state transformations
 */
export type Handlers<TState, TAction extends Action<string>> = {
  [type in TAction['type']]?: (state: TState, action: TAction) => TState;
};

/**
 * Return a function that creates reducers for a subtree of the redux store
 */
export const createReducerCreator = <TAction extends Action>() =>
  <TState>
  (initialState: TState, handlers: Handlers<TState, TAction>) =>
    (state: TState = initialState, action: TAction) => action.type in handlers
      ? handlers[action.type](state, action)
      : state;
