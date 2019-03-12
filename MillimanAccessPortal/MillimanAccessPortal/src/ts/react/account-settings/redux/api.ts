import * as yup from 'yup';

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

const validationSchema = yup.object<Partial<PendingInputState>>({
  firstName: yup.string().required('This field is required.'),
  lastName: yup.string().required('This field is required.'),
  phone: yup.string(),
  employer: yup.string(),
  new: yup.string(),
  confirm: yup.ref('new'),
});

export const fetchUser =
  createJsonRequestor<AccountActions.FetchUser, AccountActions.FetchUserSucceeded>
  ('GET', '/Account/AccountSettings2');

export const validateInput = async (inputName: string, value: string) => {
  return await validationSchema.validateAt(inputName, { [inputName]: value });
};
