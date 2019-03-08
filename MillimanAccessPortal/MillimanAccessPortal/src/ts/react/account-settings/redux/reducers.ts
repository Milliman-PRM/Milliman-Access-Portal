import { reducer as toastrReducer } from 'react-redux-toastr';
import { Action, combineReducers } from 'redux';

import { UserFull } from '../../models';
import { AccountAction, SetPendingTextInputValue } from './actions';
import { AccountState, AccountStateData, PendingFieldsState, PendingRequestState } from './store';

const _initialData: AccountStateData = {
  user: {
    id: '',
    isActivated: false,
    isSuspended: false,
    firstName: '',
    lastName: '',
    userName: '',
    email: '',
    phone: '',
    employer: '',
  },
};
const _initialPendingFields: PendingFieldsState = {
  id: null,
  isActivated: null,
  isSuspended: null,
  firstName: null,
  lastName: null,
  userName: null,
  email: null,
  phone: null,
  employer: null,
  current: null,
  new: null,
  confirm: null,
};
const _initialPendingRequests: PendingRequestState = {
  update: false,
  validatePassword: false,
};

type Handlers<TState, TAction extends AccountAction> = {
  [type in TAction['type']]?: (state: TState, action: TAction) => TState;
};
const createReducer =
  <TState, TAction extends AccountAction = AccountAction>
  (initialState: TState, handlers: Handlers<TState, TAction>) =>
    (state: TState = initialState, action: TAction) => action.type in handlers
      ? handlers[action.type as TAction['type']](state, action)
      : state;

const data = createReducer<AccountStateData>(_initialData, ({
}));
const pendingFields = createReducer<PendingFieldsState>(_initialPendingFields, ({
  SET_PENDING_TEXT_INPUT_VALUE: (state, action) => ({
    ...state,
    [action.inputName]: action.value,
  }),
}));
const pendingRequests = createReducer<PendingRequestState>(_initialPendingRequests, ({
}));
const pending = combineReducers({
  fields: pendingFields,
  requests: pendingRequests,
});
const form: null = null;
export const accountSettings = combineReducers({
  data,
  pending,
  form,
  toastr: toastrReducer,
});
