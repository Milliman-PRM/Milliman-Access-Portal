import { createJsonRequestorCreator } from '../../shared-components/redux/api';
import { RequestAccountAction, ResponseAccountAction } from './actions';
import * as AccountActions from './actions';

/**
 * Function for handling request actions.
 * @param method HTTP method to use
 * @param url Request URL
 */
const createJsonRequestor = createJsonRequestorCreator<RequestAccountAction, ResponseAccountAction>();

export const fetchUser =
  createJsonRequestor<AccountActions.FetchUser, AccountActions.FetchUserSucceeded>
  ('GET', '/Account/AccountSettings2');
