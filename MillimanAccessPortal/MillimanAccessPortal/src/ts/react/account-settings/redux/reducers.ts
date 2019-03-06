import { reducer as toastrReducer } from 'react-redux-toastr';
import { Action } from 'redux';

import { AccountState } from './store';

const _initialState: AccountState = {
  data: {
    information: {
      employer: '',
      firstName: '',
      lastName: '',
      phone: '',
    },
    password: {
      confirm: '',
      current: '',
      new: '',
    },
  },
  form: null,
  pending: {
    data: {
      information: {
        employer: '',
        firstName: '',
        lastName: '',
        phone: '',
      },
      password: {
        confirm: '',
        current: '',
        new: '',
      },
    },
    requests: {
      update: false,
      validatePassword: false,
    },
  },
  toastr: undefined,
};
export const accountSettings = (state: AccountState = _initialState, action: Action) => ({
  ...state,
  toastr: toastrReducer(state.toastr, action),
});
