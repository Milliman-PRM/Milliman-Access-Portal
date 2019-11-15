import { getJsonData, postJsonData } from '../../../shared';
import { RequestAction, ResponseAction } from './actions';

/**
 * Return a function that creates JSON requestors for a set of requests and responses
 */
export function createJsonRequestorCreator<TReq extends RequestAction, TRes extends ResponseAction>() {
  return <TReqAction extends TReq, TResAction extends TRes>
  (method: 'GET' | 'POST' | 'DELETE', url: string) =>
  async (requestModel: TReqAction['request']) =>
    method === 'GET'
      ? await getJsonData<TResAction['response']>(url, requestModel)
      : await postJsonData<TResAction['response']>(url, requestModel, method);
}
