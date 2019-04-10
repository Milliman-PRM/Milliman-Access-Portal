import {
    createActionCreator, createRequestActionCreator,
} from '../../shared-components/redux/action-creators';
import * as AccessActions from './actions';

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
export const openInactiveModal =
  createActionCreator<AccessActions.OpenInactiveModal>('OPEN_INACTIVE_MODAL');
export const closeInactiveModal =
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
export const promptStatusRefreshStopped =
  createActionCreator<AccessActions.PromptStatusRefreshStopped>('PROMPT_STATUS_REFRESH_STOPPED');

// Data fetches
export const fetchClients =
  createRequestActionCreator<AccessActions.FetchClients>('FETCH_CLIENTS');
export const fetchItems =
  createRequestActionCreator<AccessActions.FetchItems>('FETCH_ITEMS');
export const fetchGroups =
  createRequestActionCreator<AccessActions.FetchGroups>('FETCH_GROUPS');
export const fetchSelections =
  createRequestActionCreator<AccessActions.FetchSelections>('FETCH_SELECTIONS');
export const fetchStatusRefresh =
  createRequestActionCreator<AccessActions.FetchStatusRefresh>('FETCH_STATUS_REFRESH');
export const fetchSessionCheck =
  createRequestActionCreator<AccessActions.FetchSessionCheck>('FETCH_SESSION_CHECK');

// Updates
export const createGroup =
  createRequestActionCreator<AccessActions.CreateGroup>('CREATE_GROUP');
export const updateGroup =
  createRequestActionCreator<AccessActions.UpdateGroup>('UPDATE_GROUP');
export const deleteGroup =
  createRequestActionCreator<AccessActions.DeleteGroup>('DELETE_GROUP');
export const suspendGroup =
  createRequestActionCreator<AccessActions.SuspendGroup>('SUSPEND_GROUP');
export const updateSelections =
  createRequestActionCreator<AccessActions.UpdateSelections>('UPDATE_SELECTIONS');
export const cancelReduction =
  createRequestActionCreator<AccessActions.CancelReduction>('CANCEL_REDUCTION');

// Scheduled actions
export const scheduleStatusRefresh =
  createActionCreator<AccessActions.ScheduleStatusRefresh>('SCHEDULE_STATUS_REFRESH');
export const decrementStatusRefreshAttempts =
  createActionCreator<AccessActions.DecrementStatusRefreshAttempts>('DECREMENT_STATUS_REFRESH_ATTEMPTS');
export const scheduleSessionCheck =
  createActionCreator<AccessActions.ScheduleSessionCheck>('SCHEDULE_SESSION_CHECK');
