import * as _ from 'lodash';

import { AccountState, PendingFieldsState } from './store';

export function pendingFieldValues(state: AccountState) {
  const data: Partial<PendingFieldsState> = { ...state.data.user };
  _.forEach(state.pending.fields, (value, key: keyof PendingFieldsState) => {
    if (value !== null) {
      data[key] = value;
    } else if (!(key in data)) {
      data[key] = '';
    }
  });
  return data;
}

export function modifiedFields(state: AccountState) {
  const data: Partial<PendingFieldsState> = { ...state.data.user };
  const pending = { ...state.pending.fields };
  return _.mapValues(pending, (value, key: keyof PendingFieldsState) => ({
    modified: (!state.pending.requests.fetchUser && (value !== null) && (data[key]
      ? data[key] !== value
      : value && value.length > 0)),
  }));
}

export function anyFieldModified(state: AccountState) {
  return _.reduce(modifiedFields(state), (prev, cur) => prev || cur.modified, false);
}

export function allFieldsValid(state: AccountState) {
  return anyFieldModified(state);
}

export function fieldProps(state: AccountState) {
  const values = pendingFieldValues(state);
  return {
    username: values.userName,
    firstName: values.firstName,
    lastName: values.lastName,
    phone: values.phone,
    employer: values.employer,
    currentPassword: values.current,
    newPassword: values.new,
    confirmPassword: values.confirm,
  };
}
