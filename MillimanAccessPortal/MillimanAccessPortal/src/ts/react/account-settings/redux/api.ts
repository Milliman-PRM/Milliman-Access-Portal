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
  | 'current'
  | 'new'
  | 'confirm'
  >;

const validatePassword = async (requestModel: { proposedPassword: string }) =>
  await postJsonData<{ valid: boolean, messages?: string[] }>('/Account/CheckPasswordValidity2', requestModel);

let msg: string = null;
const userSchema = yup.object<UserInputState>({
  firstName: yup.string().required('This field is required'),
  lastName: yup.string().required('This field is required'),
  phone: yup.string(),
  employer: yup.string(),
});
const passwordSchema = yup.object<PasswordInputState>({
  current: yup.string()
    .required('This field is required'),
  new: yup.string()
    .notOneOf([yup.ref('current')], 'Your new password must be different from your current one')
    .test('password', () => msg, (value) =>
      validatePassword({ proposedPassword: value })
        .then((response) => {
          msg = response.messages
            ? response.messages.join('\r\n')
            : null;
          return response.valid;
        }))
    .required('This field is required'),
  confirm: yup.string()
    .oneOf([yup.ref('new')], 'Does not match new password')
    .required('This field is required'),
}).notRequired();

export const fetchUser =
  createJsonRequestor<AccountActions.FetchUser, AccountActions.FetchUserSucceeded>
  ('GET', '/Account/AccountSettings2');

export const fetchSessionCheck =
  createJsonRequestor<AccountActions.FetchSessionCheck, AccountActions.FetchSessionCheckSucceeded>
  ('GET', '/Account/SessionStatus');

export const updateAccount =
  createJsonRequestor<AccountActions.UpdateAccount, AccountActions.UpdateAccountSucceeded>
  ('POST', '/Account/UpdateAccount');

export const validateUserInput = async (value: UserInputState, inputName: string) => {
  if (inputName) {
    return await userSchema.validateAt(inputName, value);
  }
  return await userSchema.validate(value);
};

export const validatePasswordInput = async (value: PasswordInputState, inputName: string) => {
  if (inputName) {
    return await passwordSchema.validateAt(inputName, value);
  }
  return await passwordSchema.validate(value);
};
