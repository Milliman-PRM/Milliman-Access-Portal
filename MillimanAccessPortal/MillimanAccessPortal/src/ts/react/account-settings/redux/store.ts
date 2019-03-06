import * as toastr from 'react-redux-toastr';
import { applyMiddleware, createStore } from 'redux';
import createSagaMiddleware from 'redux-saga';
import { accountSettings } from './reducers';

// import { accountSettings } from './reducers';
// import sagas from './sagas';

interface AccountStateInformation {
  firstName: string;
  lastName: string;
  phone: string;
  employer: string;
}
interface AccountStatePassword {
  current: string;
  new: string;
  confirm: string;
}
export interface PendingRequestState {
  update: boolean;
  validatePassword: boolean;
}
interface AccountStatePending {
  data: AccountStateData;
  requests: PendingRequestState;
}
interface AccountStateData {
  information: AccountStateInformation;
  password: AccountStatePassword;
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
//  applyMiddleware(sagaMiddleware),
  );
// sagaMiddleware.run(sagas);
