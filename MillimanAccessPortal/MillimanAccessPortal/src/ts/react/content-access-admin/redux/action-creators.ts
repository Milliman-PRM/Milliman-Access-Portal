import { Action } from 'redux';

import * as AccessActions from './actions';
import { ErrorAction, RequestAction, ResponseAction, TSError } from './actions';

export type ActionWithoutType<T> = Pick<T, Exclude<keyof T, 'type'>>;

/**
 * Create an action creator for a generic action.
 * @param type Action type
 */
function createActionCreator<T extends Action>(type: T['type']): (actionProps: ActionWithoutType<T>) => T {
  return (actionProps: ActionWithoutType<T>) => Object.assign(
    {} as T,  // TypeScript can't infer T from its parts because it is generic
    { type },
    actionProps,
  );
}
/**
 * Create an action creator for an action that contains a request property.
 * @param type Action type
 */
function createRequestActionCreator<T extends RequestAction>(type: T['type']):
  (request: T['request']) => T {
  return (request: T['request']) => Object.assign(
    {} as T,  // TypeScript can't infer T from its parts because it is generic
    { type },
    { request },
  );
}
/**
 * Create an action creator for an action that contains a response property.
 * This function is exported for use by sagas.
 * @param type Action type
 */
export function createResponseActionCreator<T extends ResponseAction>(type: T['type']):
  (response: T['response']) => T {
  return (response: T['response']) => Object.assign(
    {} as T,  // TypeScript can't infer T from its parts because it is generic
    { type },
    { response },
  );
}
/**
 * Create an action creator for an action that contains an error property.
 * This function is exported for use by sagas.
 * @param type Action type
 */
export function createErrorActionCreator<T extends ErrorAction>(type: T['type']):
  (error: TSError) => T {
  return (error: TSError) => Object.assign(
    {} as T,  // TypeScript can't infer T from its parts because it is generic
    { type },
    { error },
  );
}

export const selectClient =
  createActionCreator<AccessActions.SelectClient>('SELECT_CLIENT');
export const selectItem =
  createActionCreator<AccessActions.SelectItem>('SELECT_ITEM');
export const selectGroup =
  createActionCreator<AccessActions.SelectGroup>('SELECT_GROUP');

export const setExpandedGroup =
  createActionCreator<AccessActions.SetExpandedGroup>('SET_EXPANDED_GROUP');
export const setCollapsedGroup =
  createActionCreator<AccessActions.SetCollapsedGroup>('SET_COLLAPSED_GROUP');
export const setAllExpandedGroup =
  createActionCreator<AccessActions.SetAllExpandedGroup>('SET_ALL_EXPANDED_GROUP');
export const setAllCollapsedGroup =
  createActionCreator<AccessActions.SetAllCollapsedGroup>('SET_ALL_COLLAPSED_GROUP');

export const setFilterTextClient =
  createActionCreator<AccessActions.SetFilterTextClient>('SET_FILTER_TEXT_CLIENT');
export const setFilterTextItem =
  createActionCreator<AccessActions.SetFilterTextItem>('SET_FILTER_TEXT_ITEM');
export const setFilterTextGroup =
  createActionCreator<AccessActions.SetFilterTextGroup>('SET_FILTER_TEXT_GROUP');
export const setFilterTextSelections =
  createActionCreator<AccessActions.SetFilterTextSelections>('SET_FILTER_TEXT_SELECTIONS');

export const setPendingIsMaster =
  createActionCreator<AccessActions.SetPendingIsMaster>('SET_PENDING_IS_MASTER');
export const setPendingSelectionOn =
  createActionCreator<AccessActions.SetPendingSelectionOn>('SET_PENDING_SELECTION_ON');
export const setPendingSelectionOff =
  createActionCreator<AccessActions.SetPendingSelectionOff>('SET_PENDING_SELECTION_OFF');
export const setPendingAllSelectionsOn =
  createActionCreator<AccessActions.SetPendingAllSelectionsOn>('SET_PENDING_ALL_SELECTIONS_ON');
export const setPendingAllSelectionsOff =
  createActionCreator<AccessActions.SetPendingAllSelectionsOff>('SET_PENDING_ALL_SELECTIONS_OFF');

export const openAddGroupModal =
  createActionCreator<AccessActions.OpenAddGroupModal>('OPEN_ADD_GROUP_MODAL');
export const closeAddGroupModal =
  createActionCreator<AccessActions.CloseAddGroupModal>('CLOSE_ADD_GROUP_MODAL');
export const setPendingNewGroupName =
  createActionCreator<AccessActions.SetPendingNewGroupName>('SET_PENDING_NEW_GROUP_NAME');
export const openDeleteGroupModal =
  createActionCreator<AccessActions.OpenDeleteGroupModal>('OPEN_DELETE_GROUP_MODAL');
export const closeDeleteGroupModal =
  createActionCreator<AccessActions.CloseDeleteGroupModal>('CLOSE_DELETE_GROUP_MODAL');
export const openInvalidateModal =
  createActionCreator<AccessActions.OpenInactiveModal>('OPEN_INACTIVE_MODAL');
export const closeInvalidateModal =
  createActionCreator<AccessActions.CloseInactiveModal>('CLOSE_INACTIVE_MODAL');

export const setGroupEditingOn =
  createActionCreator<AccessActions.SetGroupEditingOn>('SET_GROUP_EDITING_ON');
export const setGroupEditingOff =
  createActionCreator<AccessActions.SetGroupEditingOff>('SET_GROUP_EDITING_OFF');
export const setPendingGroupName =
  createActionCreator<AccessActions.SetPendingGroupName>('SET_PENDING_GROUP_NAME');
export const setPendingGroupUserQuery =
  createActionCreator<AccessActions.SetPendingGroupUserQuery>('SET_PENDING_GROUP_USER_QUERY');
export const setPendingGroupUserAssigned =
  createActionCreator<AccessActions.SetPendingGroupUserAssigned>('SET_PENDING_GROUP_USER_ASSIGNED');
export const setPendingGroupUserRemoved =
  createActionCreator<AccessActions.SetPendingGroupUserRemoved>('SET_PENDING_GROUP_USER_REMOVED');
export const promptGroupEditing =
  createActionCreator<AccessActions.PromptGroupEditing>('PROMPT_GROUP_EDITING');
export const promptGroupNameEmpty =
  createActionCreator<AccessActions.PromptGroupNameEmpty>('PROMPT_GROUP_NAME_EMPTY');

// Data fetches
export const fetchClients =
  createRequestActionCreator<AccessActions.FetchClients>('FETCH_CLIENTS');
export const fetchClientsSucceeded =
  createActionCreator<AccessActions.FetchClientsSucceeded>('FETCH_CLIENTS_SUCCEEDED');
export const fetchClientsFailed =
  createActionCreator<AccessActions.FetchClientsFailed>('FETCH_CLIENTS_FAILED');
export const fetchItems =
  createRequestActionCreator<AccessActions.FetchItems>('FETCH_ITEMS');
export const fetchItemsSucceeded =
  createActionCreator<AccessActions.FetchItemsSucceeded>('FETCH_ITEMS_SUCCEEDED');
export const fetchItemsFailed =
  createActionCreator<AccessActions.FetchItemsFailed>('FETCH_ITEMS_FAILED');
export const fetchGroups =
  createRequestActionCreator<AccessActions.FetchGroups>('FETCH_GROUPS');
export const fetchGroupsSucceeded =
  createActionCreator<AccessActions.FetchGroupsSucceeded>('FETCH_GROUPS_SUCCEEDED');
export const fetchGroupsFailed =
  createActionCreator<AccessActions.FetchGroupsFailed>('FETCH_GROUPS_FAILED');
export const fetchSelections =
  createRequestActionCreator<AccessActions.FetchSelections>('FETCH_SELECTIONS');
export const fetchSelectionsSucceeded =
  createActionCreator<AccessActions.FetchSelectionsSucceeded>('FETCH_SELECTIONS_SUCCEEDED');
export const fetchSelectionsFailed =
  createActionCreator<AccessActions.FetchSelectionsFailed>('FETCH_SELECTIONS_FAILED');
export const fetchStatusRefresh =
  createRequestActionCreator<AccessActions.FetchStatusRefresh>('FETCH_STATUS_REFRESH');
export const fetchStatusRefreshSucceeded =
  createActionCreator<AccessActions.FetchStatusRefreshSucceeded>('FETCH_STATUS_REFRESH_SUCCEEDED');
export const fetchStatusRefreshFailed =
  createActionCreator<AccessActions.FetchStatusRefreshFailed>('FETCH_STATUS_REFRESH_FAILED');
export const fetchSessionCheck =
  createRequestActionCreator<AccessActions.FetchSessionCheck>('FETCH_SESSION_CHECK');
export const fetchSessionCheckSucceeded =
  createActionCreator<AccessActions.FetchSessionCheckSucceeded>('FETCH_SESSION_CHECK_SUCCEEDED');
export const fetchSessionCheckFailed =
  createActionCreator<AccessActions.FetchSessionCheckFailed>('FETCH_SESSION_CHECK_FAILED');

// Updates
export const createGroup =
  createRequestActionCreator<AccessActions.CreateGroup>('CREATE_GROUP');
export const createGroupSucceeded =
  createActionCreator<AccessActions.CreateGroupSucceeded>('CREATE_GROUP_SUCCEEDED');
export const createGroupFailed =
  createActionCreator<AccessActions.CreateGroupFailed>('CREATE_GROUP_FAILED');
export const updateGroup =
  createRequestActionCreator<AccessActions.UpdateGroup>('UPDATE_GROUP');
export const updateGroupSucceeded =
  createActionCreator<AccessActions.UpdateGroupSucceeded>('UPDATE_GROUP_SUCCEEDED');
export const updateGroupFailed =
  createActionCreator<AccessActions.UpdateGroupFailed>('UPDATE_GROUP_FAILED');
export const deleteGroup =
  createRequestActionCreator<AccessActions.DeleteGroup>('DELETE_GROUP');
export const deleteGroupSucceeded =
  createActionCreator<AccessActions.DeleteGroupSucceeded>('DELETE_GROUP_SUCCEEDED');
export const deleteGroupFailed =
  createActionCreator<AccessActions.DeleteGroupFailed>('DELETE_GROUP_FAILED');
export const suspendGroup =
  createRequestActionCreator<AccessActions.SuspendGroup>('SUSPEND_GROUP');
export const suspendGroupSucceeded =
  createActionCreator<AccessActions.SuspendGroupSucceeded>('SUSPEND_GROUP_SUCCEEDED');
export const suspendGroupFailed =
  createActionCreator<AccessActions.SuspendGroupFailed>('SUSPEND_GROUP_FAILED');
export const updateSelections =
  createRequestActionCreator<AccessActions.UpdateSelections>('UPDATE_SELECTIONS');
export const updateSelectionsSucceeded =
  createActionCreator<AccessActions.UpdateSelectionsSucceeded>('UPDATE_SELECTIONS_SUCCEEDED');
export const updateSelectionsFailed =
  createActionCreator<AccessActions.UpdateSelectionsFailed>('UPDATE_SELECTIONS_FAILED');
export const cancelReduction =
  createRequestActionCreator<AccessActions.CancelReduction>('CANCEL_REDUCTION');
export const cancelReductionSucceeded =
  createActionCreator<AccessActions.CancelReductionSucceeded>('CANCEL_REDUCTION_SUCCEEDED');
export const cancelReductionFailed =
  createActionCreator<AccessActions.CancelReductionFailed>('CANCEL_REDUCTION_FAILED');

// Scheduled actions
export const scheduleStatusRefresh =
  createActionCreator<AccessActions.ScheduleStatusRefresh>('SCHEDULE_STATUS_REFRESH');
export const scheduleSessionCheck =
  createActionCreator<AccessActions.ScheduleSessionCheck>('SCHEDULE_SESSION_CHECK');
