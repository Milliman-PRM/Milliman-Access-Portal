import * as yup from 'yup';

import { postJsonData } from '../../../shared';
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
export type PasswordInputState = Pick<PendingInputState,
  | 'new'
  | 'confirm'
  >;

const validatePassword = async (requestModel: { proposedPassword: string }) =>
  await postJsonData<{ valid: boolean, messages?: string[] }>('/Account/CheckPasswordValidity2', requestModel);

const userSchema = yup.object<UserInputState>({
  firstName: yup.string().required('This field is required.'),
  lastName: yup.string().required('This field is required.'),
  phone: yup.string(),
  employer: yup.string(),
});
const passwordSchema = yup.object<PasswordInputState>({
  new: yup.string().test('password', '0_0', (value) =>
    validatePassword({ proposedPassword: value })
      .then((response) => response.valid)),
  confirm: yup.string().oneOf([yup.ref('new')], 'Passwords must match.').required(),
});

export const fetchUser =
  createJsonRequestor<AccountActions.FetchUser, AccountActions.FetchUserSucceeded>
  ('GET', '/Account/AccountSettings2');

export const validateUserInput = async (value: UserInputState) => {
  return await userSchema.validate(value);
};

export const validatePasswordInput = async (value: PasswordInputState) => {
  return await passwordSchema.validate(value);
};
