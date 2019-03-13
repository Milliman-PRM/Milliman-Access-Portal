import * as _ from 'lodash';

import { AccountState, AccountStateForm, PendingInputState } from './store';

/**
 * Select input values pending submission.
 * @param state Redux store
 */
export function pendingInputValues(state: AccountState) {
  const data: Partial<PendingInputState> = { ...state.data.user };
  _.forEach(state.pending.inputs, (value, key: keyof PendingInputState) => {
    if (value !== null) {
      data[key] = value;
    } else if (!(key in data)) {
      data[key] = '';
    }
  });
  return data;
}

/**
 * Select whether each input is modified or not
 * @param state Redux store
 */
export function modifiedInputs(state: AccountState) {
  const data: Partial<PendingInputState> = { ...state.data.user };
  const pending = { ...state.pending.inputs };
  return _.mapValues(pending, (value, key: keyof PendingInputState) => ({
    modified: (!state.pending.requests.fetchUser && (value !== null) && (data[key]
      ? data[key] !== value
      : value && value.length > 0)),
  }));
}

/**
 * Select whether any input is modified
 * @param state Redux store
 */
export function anyInputModified(state: AccountState) {
  return _.reduce(modifiedInputs(state), (prev, cur) => prev || cur.modified, false);
}

/**
 * Select whether all inputs are valid
 * @param state Redux store
 */
export function allInputsValid(state: AccountState) {
  return _.reduce(state.form, (prev, cur) => prev && cur.valid, true);
}

/**
 * Select account settings input property
 * @param state Redux store
 */
export function inputProps(state: AccountState) {
  const values = pendingInputValues(state);
  return {
    username: state.data.user.userName,
    firstName: values.firstName,
    lastName: values.lastName,
    phone: values.phone,
    employer: values.employer,
    currentPassword: values.current,
    newPassword: values.new,
    confirmPassword: values.confirm,
  };
}

/**
 * Select account settings valid property
 * @param state Redux store
 */
export function validProps(state: AccountState) {
  const valid = state.form;
  return {
    firstName: valid.firstName,
    lastName: valid.lastName,
    phone: valid.phone,
    employer: valid.employer,
    currentPassword: valid.current,
    newPassword: valid.new,
    confirmPassword: valid.confirm,
  };
}
