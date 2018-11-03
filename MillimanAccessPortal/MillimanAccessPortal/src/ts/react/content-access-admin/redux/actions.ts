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

export function setValueSelected(id: Guid, bValue: boolean): ActionWithId & ActionWithBoolean {
  console.log(`Set value of ${id} to ${bValue}`);
  return {
    type: 'SET_VALUE_SELECTED',
    id,
    bValue,
  };
}
