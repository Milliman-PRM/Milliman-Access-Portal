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
  ('GET', '/ContentAccessAdmin/Clients');

export const fetchItems =
  createJsonRequestor<AccessActions.FetchItems, AccessActions.FetchItemsSucceeded>
  ('GET', '/ContentAccessAdmin/ContentItems');

export const fetchGroups =
  createJsonRequestor<AccessActions.FetchGroups, AccessActions.FetchGroupsSucceeded>
  ('GET', '/ContentAccessAdmin/SelectionGroups');

export const fetchSelections =
  createJsonRequestor<AccessActions.FetchSelections, AccessActions.FetchSelectionsSucceeded>
  ('GET', '/ContentAccessAdmin/Selections');

export const fetchStatusRefresh =
  createJsonRequestor<AccessActions.FetchStatusRefresh, AccessActions.FetchStatusRefreshSucceeded>
  ('GET', '/ContentAccessAdmin/Status');

export const fetchSessionCheck =
  createJsonRequestor<AccessActions.FetchSessionCheck, AccessActions.FetchSessionCheckSucceeded>
  ('GET', '/Account/SessionStatus');

export const createGroup =
  createJsonRequestor<AccessActions.CreateGroup, AccessActions.CreateGroupSucceeded>
  ('POST', '/ContentAccessAdmin/CreateGroup');

export const updateGroup =
  createJsonRequestor<AccessActions.UpdateGroup, AccessActions.UpdateGroupSucceeded>
  ('POST', '/ContentAccessAdmin/UpdateGroup');

export const deleteGroup =
  createJsonRequestor<AccessActions.DeleteGroup, AccessActions.DeleteGroupSucceeded>
  ('POST', '/ContentAccessAdmin/DeleteGroup');

export const suspendGroup =
  createJsonRequestor<AccessActions.SuspendGroup, AccessActions.SuspendGroupSucceeded>
  ('POST', '/ContentAccessAdmin/SuspendGroup');

export const updateSelections =
  createJsonRequestor<AccessActions.UpdateSelections, AccessActions.UpdateSelectionsSucceeded>
  ('POST', '/ContentAccessAdmin/UpdateSelections');

export const cancelReduction =
  createJsonRequestor<AccessActions.CancelReduction, AccessActions.CancelReductionSucceeded>
  ('POST', '/ContentAccessAdmin/CancelReduction');
