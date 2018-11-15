import { Action } from 'redux';
import { Guid } from '../../models';

export interface ActionWithId extends Action {
  id: Guid;
}

export interface ActionWithBoolean extends Action {
  bValue: boolean;
}

export interface ActionWithString extends Action {
  sValue: string;
}

export function nop(): Action {
  return {
    type: 'NOP',
  };
}

export function selectClientCard(id: Guid): ActionWithId {
  return {
    type: 'SELECT_CARD_CLIENT',
    id,
  };
}

export function selectItemCard(id: Guid): ActionWithId {
  return {
    type: 'SELECT_CARD_ITEM',
    id,
  };
}

export function selectGroupCard(id: Guid): ActionWithId {
  return {
    type: 'SELECT_CARD_GROUP',
    id,
  };
}

export function setGroupCardExpanded(id: Guid, bValue: boolean): ActionWithId & ActionWithBoolean {
  return {
    type: 'SET_GROUP_CARD_EXPANDED',
    id,
    bValue,
  };
}

export function expandAllGroups(): Action {
  return {
    type: 'EXPAND_ALL_GROUPS',
  };
}

export function collapseAllGroups(): Action {
  return {
    type: 'COLLAPSE_ALL_GROUPS',
  };
}

export function setClientFilterText(sValue: string): ActionWithString {
  return {
    type: 'SET_CLIENT_FILTER_TEXT',
    sValue,
  };
}

export function setItemFilterText(sValue: string): ActionWithString {
  return {
    type: 'SET_ITEM_FILTER_TEXT',
    sValue,
  };
}

export function setGroupFilterText(sValue: string): ActionWithString {
  return {
    type: 'SET_GROUP_FILTER_TEXT',
    sValue,
  };
}

export function setValueFilterText(sValue: string): ActionWithString {
  return {
    type: 'SET_VALUE_FILTER_TEXT',
    sValue,
  };
}

export function setMasterSelected(bValue: boolean): ActionWithBoolean {
  return {
    type: 'SET_MASTER_SELECTED',
    bValue,
  };
}

export function setValueSelected(id: Guid, bValue: boolean): ActionWithId & ActionWithBoolean {
  return {
    type: 'SET_VALUE_SELECTED',
    id,
    bValue,
  };
}

export function openAddGroupModal(): Action {
  return {
    type: 'OPEN_ADD_GROUP_MODAL',
  };
}

export function closeAddGroupModal(): Action {
  return {
    type: 'CLOSE_ADD_GROUP_MODAL',
  };
}

export function setValueAddGroupModal(sValue: string): ActionWithString {
  return {
    type: 'SET_VALUE_ADD_GROUP_MODAL',
    sValue,
  };
}
