import { Action } from 'redux';

import { Client, Guid, SelectionGroup } from '../../models';
import * as api from './api';

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
  FetchItems = 'FETCH_ITEMS',
  FetchGroups = 'FETCH_GROUPS',
  FetchSelections = 'FETCH_SELECTIONS',
  FetchStatus = 'FETCH_STATUS',
  CreateGroup = 'CREATE_GROUP',
  UpdateGroup = 'UPDATE_GROUP',
  DeleteGroup = 'DELETE_GROUP',
  SuspendGroup = 'SUSPEND_GROUP',
  UpdateSelections = 'UPDATE_SELECTIONS',
  CancelReduction = 'CANCEL_REDUCTION',
}

export enum DataSuffixes {
  None = '',
  Succeeded = '_SUCCEEDED',
  Failed = '_FAILED',
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

export type DataArgs = Array<number | string | boolean | object>;
export interface DataAction<T extends DataArgs, R> extends Action {
  callback: (...args: T) => R;
  args: T;
}
function createDataActionCreator<T extends DataArgs, R>(
    action: AccessAction, callback: (...args: T) => R): (...args: T) => DataAction<T, R> {
  return (...args: T) => {
    return {
      type: action,
      callback,
      args,
    };
  };
}

// Data fetches
export const fetchClients = createDataActionCreator(AccessAction.FetchClients, api.fetchClients);
export const fetchItems = createDataActionCreator(AccessAction.FetchItems, api.fetchItems);
export const fetchGroups = createDataActionCreator(AccessAction.FetchGroups, api.fetchGroups);
export const fetchSelections = createDataActionCreator(AccessAction.FetchSelections, api.fetchSelections);
export const fetchStatus = createDataActionCreator(AccessAction.FetchStatus, api.fetchStatus);

// Updates
export const createGroup = createDataActionCreator(AccessAction.CreateGroup, api.createGroup);
export const updateGroup = createDataActionCreator(AccessAction.UpdateGroup, api.updateGroup);
export const deleteGroup = createDataActionCreator(AccessAction.DeleteGroup, api.deleteGroup);
export const suspendGroup = createDataActionCreator(AccessAction.SuspendGroup, api.suspendGroup);
export const updateSelections = createDataActionCreator(AccessAction.UpdateSelections, api.updateSelections);
export const cancelReduction = createDataActionCreator(AccessAction.CancelReduction, api.cancelReduction);
