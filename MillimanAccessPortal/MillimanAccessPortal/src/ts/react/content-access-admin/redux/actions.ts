import { Action } from 'redux';
import { Guid } from '../../models';

export interface ActionWithId extends Action {
  id: Guid;
}

export interface ActionWithBoolean extends Action {
  bValue: boolean;
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
