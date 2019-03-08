import { getJsonData, postJsonData } from '../../../shared';
import { RequestAction, ResponseAction } from './actions';
import * as AccountActions from './actions';

const createJsonRequestor =
  <TRequestAction extends RequestAction, TResponseAction extends ResponseAction>
  (method: 'GET' | 'POST', url: string) =>
  async (requestModel: TRequestAction['request']) =>
    method === 'GET'
      ? await getJsonData<TResponseAction['response']>(url, requestModel)
      : await postJsonData<TResponseAction['response']>(url, requestModel);

export const fetchUser =
  createJsonRequestor<AccountActions.FetchUser, AccountActions.FetchUserSucceeded>
  ('GET', '/Account/AccountSettings2');
