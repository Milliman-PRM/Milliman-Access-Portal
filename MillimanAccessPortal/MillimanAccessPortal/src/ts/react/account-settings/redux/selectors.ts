import * as _ from 'lodash';

import { AccountState, PendingInputState } from './store';

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
 * Select whether any user input is modified
 * @param state Redux store
 */
export function anyUserInputModified(state: AccountState) {
  return _.reduce(
    _.filter(modifiedInputs(state), (__, key) => ['firstName', 'lastName', 'phone', 'employer'].indexOf(key) !== -1)
    , (prev, cur) => prev || cur.modified, false);
}

/**
 * Select whether all user inputs are valid
 * @param state Redux store
 */
export function allUserInputsValid(state: AccountState) {
  return _.reduce(
    _.filter(state.form, (__, key) => ['firstName', 'lastName', 'phone', 'employer'].indexOf(key) !== -1)
    , (prev, cur) => prev && cur.valid, true);
}

/**
 * Select whether all password inputs are valid
 * @param state Redux store
 */
export function allPasswordInputsValid(state: AccountState) {
  return _.reduce(
    _.filter(state.form, (__, key) => ['current', 'new', 'confirm'].indexOf(key) !== -1)
    , (prev, cur) => prev && cur.valid, true);
}

/**
 * Select account settings input property
 * @param state Redux store
 */
export function inputProps(state: AccountState) {
  const values = pendingInputValues(state);
  return {
    userName: state.data.user.userName,
    firstName: values.firstName,
    lastName: values.lastName,
    phone: values.phone,
    employer: values.employer,
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
  };
}

/**
 * Select an object containing what should be submitted on form submission
 * @param state Redux store
 */
export function updateProps(state: AccountState) {
  const values = pendingInputValues(state);
  const userModified = anyUserInputModified(state);
  return {
    user: userModified
      ? {
        firstName: values.firstName,
        lastName: values.lastName,
        phone: values.phone,
        employer: values.employer,
      }
      : null,
  };
}
