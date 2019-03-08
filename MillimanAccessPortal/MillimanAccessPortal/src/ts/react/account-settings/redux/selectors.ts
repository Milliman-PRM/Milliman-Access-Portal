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
