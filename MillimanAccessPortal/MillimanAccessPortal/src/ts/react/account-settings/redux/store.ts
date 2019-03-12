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
  & Pick<UserFull, Exclude<keyof UserFull,
    | 'isActivated'
    | 'isSuspended'
    | 'isLocal'
    | 'id'
    | 'email'
    | 'userName'
    >>
  & AccountStatePassword;
export interface PendingRequestState {
  fetchUser: boolean;
  update: boolean;
  validatePassword: boolean;
}
export type PendingValidationState = {
  [key in keyof PendingInputState]: boolean;
};

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
  applyMiddleware(sagaMiddleware),
  );
sagaMiddleware.run(sagas);
