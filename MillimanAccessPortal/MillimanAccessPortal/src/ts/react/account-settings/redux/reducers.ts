import { reducer as toastrReducer } from 'react-redux-toastr';
import { combineReducers } from 'redux';

import { createReducerCreator } from '../../shared-components/redux/reducers';
import { AccountAction, FetchUserSucceeded, SetPendingTextInputValue } from './actions';
import { AccountStateData, PendingInputState, PendingRequestState } from './store';

const _initialData: AccountStateData = {
  user: {
    id: '',
    isActivated: false,
    isSuspended: false,
    isLocal: false,
    firstName: '',
    lastName: '',
    userName: '',
    email: '',
    phone: '',
    employer: '',
  },
};
const _initialPendingInputs: PendingInputState = {
  id: null,
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
  fetchUser: true,
  update: false,
  validatePassword: false,
};

/**
 * Create reducers for a subtree of the redux store
 * @param initialState Subtree of state these handlers can influence
 * @param handlers Actions and their state transformations
 */
const createReducer = createReducerCreator<AccountAction>();

const data = createReducer<AccountStateData>(_initialData, ({
  FETCH_USER_SUCCEEDED: (state, { response }: FetchUserSucceeded) => ({
    ...state,
    user: {
      ...state.user,
      ...response,
    },
  }),
}));
const pendingInputs = createReducer<PendingInputState>(_initialPendingInputs, ({
  SET_PENDING_TEXT_INPUT_VALUE: (state, action: SetPendingTextInputValue) => ({
    ...state,
    [action.inputName]: action.value,
  }),
  RESET_FORM: () => _initialPendingInputs,
}));
const pendingRequests = createReducer<PendingRequestState>(_initialPendingRequests, ({
  FETCH_USER: (state) => ({
    ...state,
    fetchUser: true,
  }),
  FETCH_USER_SUCCEEDED: (state) => ({
    ...state,
    fetchUser: false,
  }),
  FETCH_USER_FAILED: (state) => ({
    ...state,
    fetchUser: false,
  }),
}));
const pending = combineReducers({
  inputs: pendingInputs,
  requests: pendingRequests,
});
const form: null = null;
export const accountSettings = combineReducers({
  data,
  pending,
  form,
  toastr: toastrReducer,
});
