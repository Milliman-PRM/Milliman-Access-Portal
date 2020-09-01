import * as yup from 'yup';

import { postJsonData } from '../../../shared';
import { PasswordValidation } from '../../models';
import { createJsonRequestorCreator } from '../../shared-components/redux/api';
import { RequestAccountAction, ResponseAccountAction } from './actions';
import * as AccountActions from './actions';
import { PendingInputState } from './store';

/**
 * Function for handling request actions.
 * @param method HTTP method to use
 * @param url Request URL
 */
const createJsonRequestor = createJsonRequestorCreator<RequestAccountAction, ResponseAccountAction>();

export type UserInputState = Pick<PendingInputState,
  | 'firstName'
  | 'lastName'
  | 'phone'
  | 'employer'
  >;

const userSchema = yup.object<UserInputState>({
  firstName: yup.string().required('This field is required'),
  lastName: yup.string().required('This field is required'),
  phone: yup.string(),
  employer: yup.string(),
});

export const fetchUser =
  createJsonRequestor<AccountActions.FetchUser, AccountActions.FetchUserSucceeded>
  ('GET', '/Account/AccountSettings2');

export const fetchSessionCheck =
  createJsonRequestor<AccountActions.FetchSessionCheck, AccountActions.FetchSessionCheckSucceeded>
  ('GET', '/Account/SessionStatus');

export const updateAccount =
  createJsonRequestor<AccountActions.UpdateAccount, AccountActions.UpdateAccountSucceeded>
  ('POST', '/Account/UpdateAccount');

export const requestPasswordReset =
  createJsonRequestor<AccountActions.RequestPasswordReset, AccountActions.RequestPasswordResetSucceeded>
  ('POST', '/Account/RequestPasswordResetForExistingUser');

export const validateUserInput = async (value: UserInputState, inputName: string) => {
  if (inputName) {
    return await userSchema.validateAt(inputName, value);
  }
  return await userSchema.validate(value);
};
