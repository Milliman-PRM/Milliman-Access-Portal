import * as toastr from 'react-redux-toastr';
import { applyMiddleware, createStore } from 'redux';
import { composeWithDevTools } from 'redux-devtools-extension';
import createSagaMiddleware from 'redux-saga';

import { UserFull } from '../../models';
import { accountSettings } from './reducers';
import sagas from './sagas';

export type PendingInputState =
  & Pick<UserFull, Exclude<keyof UserFull,
    | 'isActivated'
    | 'isSuspended'
    | 'isLocal'
    | 'id'
    | 'email'
    | 'userName'
    | 'isAccountDisabled'
    | 'dateOfAccountDisable'
  >>;
export interface PendingRequestState {
  fetchUser: boolean;
  update: boolean;
}
export interface PendingValidationState {
  user: boolean;
}

export interface ValidationState {
  valid: boolean;
  message?: string;
}

export interface AccountStateData {
  user: UserFull;
}
export interface AccountStatePending {
  inputs: PendingInputState;
  requests: PendingRequestState;
  validation: PendingValidationState;
}
export type AccountStateForm = {
  [key in keyof PendingInputState]: ValidationState;
};

export interface AccountState {
  data: AccountStateData;
  pending: AccountStatePending;
  form: AccountStateForm;
  toastr: toastr.ToastrState;
}

const sagaMiddleware = createSagaMiddleware();
export const store = createStore(
  accountSettings,
  composeWithDevTools(
    applyMiddleware(sagaMiddleware),
  ));
sagaMiddleware.run(sagas);
