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
export const fetchGlobalData =
  createJsonRequestor<AccessActions.FetchGlobalData, AccessActions.FetchGlobalDataSucceeded>
    ('GET', '/ClientAdmin/PageGlobalData');
export const fetchClientDetails =
  createJsonRequestor<AccessActions.FetchClientDetails, AccessActions.FetchClientDetailsSucceeded>
    ('GET', '/ClientAdmin/ClientDetail');
export const updateAllUserRolesInClient =
  createJsonRequestor<AccessActions.UpdateAllUserRolesInClient, AccessActions.UpdateAllUserRolesInClientSucceeded>
    ('POST', '/ClientAdmin/UpdateAllUserRolesInClient');
export const saveNewClient =
  createJsonRequestor<AccessActions.SaveNewClient, AccessActions.SaveNewClientSucceeded>
    ('POST', '/ClientAdmin/SaveNewClient');
export const editClient =
  createJsonRequestor<AccessActions.EditClient, AccessActions.EditClientSucceeded>
    ('POST', '/ClientAdmin/EditClient');
export const deleteClient =
  createJsonRequestor<AccessActions.DeleteClient, AccessActions.DeleteClientSucceeded>
    ('DELETE', '/ClientAdmin/DeleteClient');
export const saveNewClientUser =
  createJsonRequestor<AccessActions.SaveNewClientUser, AccessActions.SaveNewClientUserSucceeded>
    ('POST', '/ClientAdmin/SaveNewUser');
export const removeClientUser =
  createJsonRequestor<AccessActions.RemoveClientUser, AccessActions.RemoveClientUserSucceeded>
    ('POST', '/ClientAdmin/RemoveUserFromClient');
export const requestReenableUserAccount =
  createJsonRequestor<AccessActions.RequestReenableUserAccount, AccessActions.RequestReenableUserAccountSucceeded>
    ('POST', '/ClientAdmin/RequestReenableUserAccount');
