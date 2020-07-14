import { createJsonRequestorCreator } from '../../shared-components/redux/api';
import { RequestAccessAction, ResponseAccessAction } from './actions';
import * as AccessActions from './actions';

/**
 * Function for handling request actions.
 * @param method HTTP method to use
 * @param url Request URL
 */
const createJsonRequestor = createJsonRequestorCreator<RequestAccessAction, ResponseAccessAction>();

export const fetchClients =
  createJsonRequestor<AccessActions.FetchClients, AccessActions.FetchClientsSucceeded>
    ('GET', '/ClientAdmin/Clients');
export const fetchClientDetails =
  createJsonRequestor<AccessActions.FetchClientDetails, AccessActions.FetchClientDetailsSucceeded>
    ('GET', '/ClientAdmin/ClientDetail');
