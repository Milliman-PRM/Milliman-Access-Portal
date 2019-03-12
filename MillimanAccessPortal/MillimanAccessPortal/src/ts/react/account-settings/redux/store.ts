import * as toastr from 'react-redux-toastr';
import { applyMiddleware, createStore } from 'redux';
import createSagaMiddleware from 'redux-saga';

import { UserFull } from '../../models';
import { accountSettings } from './reducers';
import sagas from './sagas';

export interface AccountStatePassword {
  current: string;
  new: string;
  confirm: string;
}
export type PendingInputState =
  & Pick<UserFull, Exclude<keyof UserFull, 'isActivated' | 'isSuspended' | 'isLocal'>>
  & AccountStatePassword;
export interface PendingRequestState {
  fetchUser: boolean;
  update: boolean;
  validatePassword: boolean;
}
export interface AccountStatePending {
  inputs: PendingInputState;
  requests: PendingRequestState;
}
export interface AccountStateData {
  user: UserFull;
}

export interface AccountState {
  data: AccountStateData;
  pending: AccountStatePending;
  form: null;
  toastr: toastr.ToastrState;
}

const sagaMiddleware = createSagaMiddleware();
export const store = createStore(
  accountSettings,
  applyMiddleware(sagaMiddleware),
  );
sagaMiddleware.run(sagas);
