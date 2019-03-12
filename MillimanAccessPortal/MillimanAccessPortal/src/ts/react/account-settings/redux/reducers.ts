import { reducer as toastrReducer } from 'react-redux-toastr';
import { combineReducers } from 'redux';

import { createReducerCreator } from '../../shared-components/redux/reducers';
import {
    AccountAction, FetchUserSucceeded, SetPendingTextInputValue, ValidateInput, ValidateInputFailed,
    ValidateInputSucceeded,
} from './actions';
import {
    AccountStateData, AccountStateForm, PendingInputState, PendingRequestState,
    PendingValidationState,
} from './store';

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
const _initialValidation: AccountStateForm = {
  firstName: { valid: true },
  lastName: { valid: true },
  phone: { valid: true },
  employer: { valid: true },
  current: { valid: true },
  new: { valid: true },
  confirm: { valid: true },
};
const _initialPendingInputs: PendingInputState = {
  firstName: null,
  lastName: null,
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
const _initialPendingValidation: PendingValidationState = {
  firstName: false,
  lastName: false,
  phone: false,
  employer: false,
  current: false,
  new: false,
  confirm: false,
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
const pendingValidation = createReducer<PendingValidationState>(_initialPendingValidation, ({
  VALIDATE_INPUT: (state, { inputName }: ValidateInput) => ({
    ...state,
    [inputName]: true,
  }),
  VALIDATE_INPUT_SUCCEEDED: (state, { inputName }: ValidateInputSucceeded) => ({
    ...state,
    [inputName]: false,
  }),
  VALIDATE_INPUT_FAILED: (state, { inputName }: ValidateInputFailed) => ({
    ...state,
    [inputName]: false,
  }),
}));
const pending = combineReducers({
  inputs: pendingInputs,
  requests: pendingRequests,
  validation: pendingValidation,
});
const form = createReducer<AccountStateForm>(_initialValidation, ({
  VALIDATE_INPUT_SUCCEEDED: (state, { inputName }: ValidateInputSucceeded) => ({
    ...state,
    [inputName]: {
      valid: true,
    },
  }),
  VALIDATE_INPUT_FAILED: (state, { inputName, result }: ValidateInputFailed) => ({
    ...state,
    [inputName]: {
      valid: false,
      message: result.message,
    },
  }),
}));
export const accountSettings = combineReducers({
  data,
  pending,
  form,
  toastr: toastrReducer,
});
