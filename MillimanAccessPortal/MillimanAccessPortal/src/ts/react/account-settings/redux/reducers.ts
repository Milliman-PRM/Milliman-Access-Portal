import * as _ from 'lodash';
import { reducer as toastrReducer } from 'react-redux-toastr';
import { combineReducers } from 'redux';

import { createReducerCreator } from '../../shared-components/redux/reducers';
import { AccountAction } from './actions';
import * as AccountActions from './actions';
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
    timeZoneSelected: '',
    timeZoneSelections: [],
  },
};
const _initialValidation: AccountStateForm = {
  firstName: { valid: true },
  lastName: { valid: true },
  phone: { valid: true },
  employer: { valid: true },
  timeZoneSelected: { valid: true },
  timeZoneSelections: { valid: true },
};
const _initialPendingInputs: PendingInputState = {
  firstName: null,
  lastName: null,
  phone: null,
  employer: null,
  timeZoneSelected: null,
  timeZoneSelections: null,
};
const _initialPendingRequests: PendingRequestState = {
  fetchUser: true,
  update: false,
};
const _initialPendingValidation: PendingValidationState = {
  user: false,
};

/**
 * Create reducers for a subtree of the redux store
 * @param initialState Subtree of state these handlers can influence
 * @param handlers Actions and their state transformations
 */
const createReducer = createReducerCreator<AccountAction>();

const data = createReducer<AccountStateData>(_initialData, ({
  FETCH_ACCOUNT_SETTINGS_DATA_SUCCEEDED: (state, { response }: AccountActions.FetchAccountSettingsDataSucceeded) => ({
    ...state,
    user: {
      ...state.user,
      userName: response.userName,
      firstName: response.firstName,
      lastName: response.lastName,
      phone: response.phone,
      employer: response.employer,
      timeZoneSelected: (response.timeZoneSelected as { id: string, displayName: string }).id,
      timeZoneSelections: response.timeZoneSelections,
    },
  }),
  UPDATE_ACCOUNT_SUCCEEDED: (state, { response }: AccountActions.UpdateAccountSucceeded) => ({
    ...state,
    user: {
      ...state.user,
      ...state.user,
      userName: response.userName,
      firstName: response.firstName,
      lastName: response.lastName,
      phone: response.phone,
      employer: response.employer,
      timeZoneSelected: (response.timeZoneSelected as { id: string, displayName: string }).id,
      timeZoneSelections: response.timeZoneSelections,
    },
  }),
}));
const pendingInputs = createReducer<PendingInputState>(_initialPendingInputs, ({
  SET_PENDING_TEXT_INPUT_VALUE: (state, action: AccountActions.SetPendingTextInputValue) => ({
    ...state,
    [action.inputName]: action.value,
  }),
  RESET_FORM: () => _initialPendingInputs,
  UPDATE_ACCOUNT_SUCCEEDED: () => _initialPendingInputs,
}));
const pendingRequests = createReducer<PendingRequestState>(_initialPendingRequests, ({
  FETCH_ACCOUNT_SETTINGS_DATA: (state) => ({
    ...state,
    fetchUser: true,
  }),
  FETCH_ACCOUNT_SETTINGS_DATA_SUCCEEDED: (state) => ({
    ...state,
    fetchUser: false,
  }),
  FETCH_ACCOUNT_SETTINGS_DATA_FAILED: (state) => ({
    ...state,
    fetchUser: false,
  }),
}));
const pendingValidation = createReducer<PendingValidationState>(_initialPendingValidation, ({
  VALIDATE_INPUT_USER: (state) => ({
    ...state,
    user: true,
  }),
  VALIDATE_INPUT_USER_SUCCEEDED: (state) => ({
    ...state,
    user: false,
  }),
  VALIDATE_INPUT_USER_FAILED: (state) => ({
    ...state,
    user: false,
  }),
}));
const pending = combineReducers({
  inputs: pendingInputs,
  requests: pendingRequests,
  validation: pendingValidation,
});
const form = createReducer<AccountStateForm>(_initialValidation, ({
  VALIDATE_INPUT_USER_SUCCEEDED: (state, { inputName }: AccountActions.ValidateInputUserSucceeded) => inputName
    ? {
      ...state,
      [inputName]: {
        valid: true,
      },
    }
    : {
      ...state,
      firstName: {
        valid: true,
      },
      lastName: {
        valid: true,
      },
      phone: {
        valid: true,
      },
      employer: {
        valid: true,
      },
    },
  VALIDATE_INPUT_USER_FAILED: (state, action: AccountActions.ValidateInputUserFailed) => ({
    ...state,
    [action.result.path]: {
      valid: false,
      message: action.result.message,
    },
  }),
  RESET_FORM: () => _initialValidation,
  UPDATE_ACCOUNT_SUCCEEDED: () => _initialValidation,
}));
export const accountSettings = combineReducers({
  data,
  pending,
  form,
  toastr: toastrReducer,
});
