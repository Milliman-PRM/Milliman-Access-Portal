import { Action } from 'redux';
import { Guid } from '../../models';

export interface ActionWithId extends Action {
  id: Guid;
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
