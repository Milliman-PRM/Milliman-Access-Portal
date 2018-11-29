import { Client, Guid, SelectionGroup } from '../../models';

export enum AccessAction {
  SelectClient = 'SELECT_CLIENT',
  SelectItem = 'SELECT_ITEM',
  SelectGroup = 'SELECT_GROUP',
  SetExpandedGroup = 'SET_EXPANDED_GROUP',
  SetCollapsedGroup = 'SET_COLLAPSED_GROUP',
  SetAllExpandedGroup = 'SET_ALL_EXPANDED_GROUP',
  SetAllCollapsedGroup = 'SET_ALL_COLLAPSED_GROUP',
  SetFilterTextClient = 'SET_FILTER_TEXT_CLIENT',
  SetFilterTextItem = 'SET_FILTER_TEXT_ITEM',
  SetFilterTextGroup = 'SET_FILTER_TEXT_GROUP',
  SetFilterTextSelections = 'SET_FILTER_TEXT_SELECTIONS',
  SetPendingIsMaster = 'SET_PENDING_IS_MASTER',
  SetPendingSelectionOn = 'SET_PENDING_SELECTION_ON',
  SetPendingSelectionOff = 'SET_PENDING_SELECTION_OFF',
  OpenAddGroupModal = 'OPEN_ADD_GROUP_MODAL',
  CloseAddGroupModal = 'CLOSE_ADD_GROUP_MODAL',
  SetPendingNewGroupName = 'SET_PENDING_NEW_GROUP_NAME',
  SetGroupEditingOn = 'SET_GROUP_EDITING_ON',
  SetGroupEditingOff = 'SET_GROUP_EDITING_OFF',
  SetPendingGroupName = 'SET_PENDING_GROUP_NAME',
  SetPendingGroupUserQuery = 'SET_PENDING_GROUP_USER_QUERY',
  SetPendingGroupUserAssigned = 'SET_PENDING_GROUP_USER_ASSIGNED',
  SetPendingGroupUserRemoved = 'SET_PENDING_GROUP_USER_REMOVED',
  FetchClientsRequested = 'FETCH_CLIENTS_REQUESTED',
  FetchClientsSucceeded = 'FETCH_CLIENTS_SUCCEEDED',
  FetchClientsFailed = 'FETCH_CLIENTS_FAILED',
  FetchItemsRequested = 'FETCH_ITEMS_REQUESTED',
  FetchItemsSucceeded = 'FETCH_ITEMS_SUCCEEDED',
  FetchItemsFailed = 'FETCH_ITEMS_FAILED',
  FetchGroupsRequested = 'FETCH_GROUPS_REQUESTED',
  FetchGroupsSucceeded = 'FETCH_GROUPS_SUCCEEDED',
  FetchGroupsFailed = 'FETCH_GROUPS_FAILED',
  FetchSelectionsRequested = 'FETCH_SELECTIONS_REQUESTED',
  FetchSelectionsSucceeded = 'FETCH_SELECTIONS_SUCCEEDED',
  FetchSelectionsFailed = 'FETCH_SELECTIONS_FAILED',
  FetchStatusRequested = 'FETCH_STATUS_REQUESTED',
  FetchStatusSucceeded = 'FETCH_STATUS_SUCCEEDED',
  FetchStatusFailed = 'FETCH_STATUS_FAILED',
  CreateGroupRequested = 'CREATE_GROUP_REQUESTED',
  CreateGroupSucceeded = 'CREATE_GROUP_SUCCEEDED',
  CreateGroupFailed = 'CREATE_GROUP_FAILED',
  UpdateGroupRequested = 'UPDATE_GROUP_REQUESTED',
  UpdateGroupSucceeded = 'UPDATE_GROUP_SUCCEEDED',
  UpdateGroupFailed = 'UPDATE_GROUP_FAILED',
  DeleteGroupRequested = 'DELETE_GROUP_REQUESTED',
  DeleteGroupSucceeded = 'DELETE_GROUP_SUCCEEDED',
  DeleteGroupFailed = 'DELETE_GROUP_FAILED',
  SuspendGroupRequested = 'SUSPEND_GROUP_REQUESTED',
  SuspendGroupSucceeded = 'SUSPEND_GROUP_SUCCEEDED',
  SuspendGroupFailed = 'SUSPEND_GROUP_FAILED',
  UpdateSelectionsRequested = 'UPDATE_SELECTIONS_REQUESTED',
  UpdateSelectionsSucceeded = 'UPDATE_SELECTIONS_SUCCEEDED',
  UpdateSelectionsFailed = 'UPDATE_SELECTIONS_FAILED',
  CancelReductionRequested = 'CANCEL_REDUCTION_REQUESTED',
  CancelReductionSucceeded = 'CANCEL_REDUCTION_SUCCEEDED',
  CancelReductionFailed = 'CANCEL_REDUCTION_FAILED',
}

// ~~ Page actions ~~

// Card selection
export const selectClient = (id: Guid) => ({ type: AccessAction.SelectClient, id });
export const selectItem = (id: Guid) => ({ type: AccessAction.SelectItem, id });
export const selectGroup = (id: Guid) => ({ type: AccessAction.SelectGroup, id });

// Group card expansion
export const setExpandedGroup = (id: Guid) => ({ type: AccessAction.SetExpandedGroup, id });
export const setCollapsedGroup = (id: Guid) => ({ type: AccessAction.SetCollapsedGroup, id });
export const setAllExpandedGroup = () => ({ type: AccessAction.SetAllExpandedGroup });
export const setAllCollapsedGroup = () => ({ type: AccessAction.SetAllCollapsedGroup });

// Card filters
export const setFilterTextClient = (text: string) => ({ type: AccessAction.SetFilterTextClient, text });
export const setFilterTextItem = (text: string) => ({ type: AccessAction.SetFilterTextItem, text });
export const setFilterTextGroup = (text: string) => ({ type: AccessAction.SetFilterTextGroup, text });
export const setFilterTextSelections = (text: string) => ({ type: AccessAction.SetFilterTextSelections, text });

// Pending value selections
export const setPendingIsMaster = (isMaster: boolean) => ({ type: AccessAction.SetPendingIsMaster, isMaster });
export const setPendingSelectionOn = (id: Guid) => ({ type: AccessAction.SetPendingSelectionOn, id });
export const setPendingSelectionOff = (id: Guid) => ({ type: AccessAction.SetPendingSelectionOff, id });

// Add selection group modal
export const openAddGroupModal = () => ({ type: AccessAction.OpenAddGroupModal });
export const closeAddGroupModal = () => ({ type: AccessAction.CloseAddGroupModal });
export const setPendingNewGroupName = (name: string) => ({ type: AccessAction.SetPendingNewGroupName, name });

// Selection group card editing
export const setGroupEditingOn = (id: Guid) => ({ type: AccessAction.SetGroupEditingOn, id });
export const setGroupEditingOff = (id: Guid) => ({ type: AccessAction.SetGroupEditingOff, id });
export const setPendingGroupName = (name: string) => ({ type: AccessAction.SetPendingGroupName, name });
export const setPendingGroupUserQuery = (query: string) => ({ type: AccessAction.SetPendingGroupUserQuery, query });
export const setPendingGroupUserAssigned = (id: Guid) => ({ type: AccessAction.SetPendingGroupUserAssigned, id });
export const setPendingGroupUserRemoved = (id: Guid) => ({ type: AccessAction.SetPendingGroupUserRemoved, id });

// ~~ Server actions

// Data fetches
export const fetchClientsRequested = () => ({ type: AccessAction.FetchClientsRequested });
export const fetchClientsSucceeded = (payload) => ({ type: AccessAction.FetchClientsSucceeded, payload });
export const fetchClientsFailed = (error: Error) => ({ type: AccessAction.FetchClientsFailed, error });
export const fetchItemsRequested = (clientId: Guid) => ({ type: AccessAction.FetchItemsRequested, clientId });
export const fetchItemsSucceeded = (payload) => ({ type: AccessAction.FetchItemsSucceeded, payload });
export const fetchItemsFailed = (error: Error) => ({ type: AccessAction.FetchItemsFailed, error });
export const fetchGroupsRequested = (itemId: Guid) => ({ type: AccessAction.FetchGroupsRequested, itemId });
export const fetchGroupsSucceeded = (payload) => ({ type: AccessAction.FetchGroupsSucceeded, payload });
export const fetchGroupsFailed = (error: Error) => ({ type: AccessAction.FetchGroupsFailed, error });
export const fetchSelectionsRequested = (groupId: Guid) => ({ type: AccessAction.FetchSelectionsRequested, groupId });
export const fetchSelectionsSucceeded = (payload) => ({ type: AccessAction.FetchSelectionsSucceeded, payload });
export const fetchSelectionsFailed = (error: Error) => ({ type: AccessAction.FetchSelectionsFailed, error });
export const fetchStatusRequested = () => ({ type: AccessAction.FetchStatusRequested });
export const fetchStatusSucceeded = (payload) => ({ type: AccessAction.FetchStatusSucceeded, payload });
export const fetchStatusFailed = (error: Error) => ({ type: AccessAction.FetchStatusFailed, error });

// Updates
export const createGroupRequested = (itemId: Guid, name: string) =>
  ({ type: AccessAction.CreateGroupRequested, itemId, name });
export const createGroupSucceeded = (payload) => ({ type: AccessAction.CreateGroupSucceeded, payload });
export const createGroupFailed = (error) => ({ type: AccessAction.CreateGroupFailed, error });
export const updateGroupRequested = (group: SelectionGroup) => ({ type: AccessAction.UpdateGroupRequested, group });
export const updateGroupSucceeded = (payload) => ({ type: AccessAction.UpdateGroupSucceeded, payload });
export const updateGroupFailed = (error) => ({ type: AccessAction.UpdateGroupFailed, error });
export const deleteGroupRequested = (groupId: Guid) => ({ type: AccessAction.DeleteGroupRequested, groupId });
export const deleteGroupSucceeded = (payload) => ({ type: AccessAction.DeleteGroupSucceeded, payload });
export const deleteGroupFailed = (error) => ({ type: AccessAction.DeleteGroupFailed, error });
export const suspendGroupRequested = (groupId: Guid, isSuspended: boolean) =>
  ({ type: AccessAction.SuspendGroupRequested, groupId, isSuspended });
export const suspendGroupSucceeded = (payload) => ({ type: AccessAction.SuspendGroupSucceeded, payload });
export const suspendGroupFailed = (error) => ({ type: AccessAction.SuspendGroupFailed, error });
export const updateSelectionsRequested = (groupId: Guid, isMaster: boolean, selections: Guid[]) =>
  ({ type: AccessAction.UpdateSelectionsRequested, groupId, isMaster, selections });
export const updateSelectionsSucceeded = (payload) => ({ type: AccessAction.UpdateSelectionsSucceeded, payload });
export const updateSelectionsFailed = (error) => ({ type: AccessAction.UpdateSelectionsFailed, error });
export const cancelReductionRequested = (groupId: Guid) => ({ type: AccessAction.CancelReductionRequested, groupId });
export const cancelReductionSucceeded = (payload) => ({ type: AccessAction.CancelReductionSucceeded, payload });
export const cancelReductionFailed = (error) => ({ type: AccessAction.CancelReductionFailed, error });
