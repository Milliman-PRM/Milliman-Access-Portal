import { Guid } from '../../models';

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
  FetchClients = 'FETCH_CLIENTS',
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
export const fetchClients = () => ({ type: AccessAction.FetchClients });
