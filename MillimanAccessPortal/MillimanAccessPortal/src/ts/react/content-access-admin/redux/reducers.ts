import { Action } from 'redux';

import {
  Client, ReductionField, ReductionFieldValue, RootContentItem, SelectionGroup, User,
} from '../../models';
import { ActionWithId } from './actions';
import { ContentAccessAdminState } from './store';

const clients: Client[] = [
  { id: 'client1', name: 'client1', code: 'c1' },
  { id: 'client2', name: 'client2', code: 'c2' },
  { id: 'client3', name: 'client3', code: 'c3' },
  { id: 'client4', name: 'client4', code: 'c4' },
];
const items: RootContentItem[] = [
  { id: 'item1', name: 'item1', isSuspended: false, doesReduce: true, clientId: 'client1' },
  { id: 'item2', name: 'item2', isSuspended: false, doesReduce: true, clientId: 'client1' },
  { id: 'item3', name: 'item3', isSuspended: false, doesReduce: false, clientId: 'client1' },
  { id: 'item4', name: 'item4', isSuspended: false, doesReduce: true, clientId: 'client2' },
  { id: 'item5', name: 'item5', isSuspended: false, doesReduce: false, clientId: 'client2' },
  { id: 'item6', name: 'item6', isSuspended: false, doesReduce: false, clientId: 'client4' },
];
const groups: SelectionGroup[] = [
  { id: 'group1', name: 'group1', isSuspended: false, isMaster: false, rootContentItemId: 'item1',
    selectedValues: [ 'value1', 'value2', 'value3', 'value4', 'value5' ] },
  { id: 'group2', name: 'group2', isSuspended: false, isMaster: false, rootContentItemId: 'item1',
    selectedValues: [ 'value1' ] },
  { id: 'group3', name: 'group3', isSuspended: false, isMaster: false, rootContentItemId: 'item1',
    selectedValues: [ ] },
  { id: 'group4', name: 'group4', isSuspended: false, isMaster: false, rootContentItemId: 'item2',
    selectedValues: [ ] },
  { id: 'group5', name: 'group5', isSuspended: false, isMaster: false, rootContentItemId: 'item4',
    selectedValues: [ ] },
  { id: 'group6', name: 'group6', isSuspended: false, isMaster: false, rootContentItemId: 'item5',
    selectedValues: [ ] },
  { id: 'group7', name: 'group7', isSuspended: false, isMaster: false, rootContentItemId: 'item5',
    selectedValues: [ ] },
  { id: 'group8', name: 'group8', isSuspended: false, isMaster: false, rootContentItemId: 'item6',
    selectedValues: [ ] },
];
const users: User[] = [
  { id: 'user1', activated: true, firstName: 'first1', lastName: 'last1', userName: 'username1',
    email: 'email1', isSuspended: false },
  { id: 'user2', activated: true, firstName: 'first2', lastName: 'last2', userName: 'username2',
    email: 'email2', isSuspended: false },
  { id: 'user3', activated: true, firstName: 'first3', lastName: 'last3', userName: 'username3',
    email: 'email3', isSuspended: false },
  { id: 'user4', activated: true, firstName: 'first4', lastName: 'last4', userName: 'username4',
    email: 'email4', isSuspended: false },
  { id: 'user5', activated: true, firstName: 'first5', lastName: 'last5', userName: 'username5',
    email: 'email5', isSuspended: false },
  { id: 'user6', activated: true, firstName: 'first6', lastName: 'last6', userName: 'username6',
    email: 'email6', isSuspended: false },
];
const fields: ReductionField[] = [
  { id: 'field1', fieldName: 'field1', displayName: 'Field 1', valueDelimiter: '|',
    rootContentItemId: 'item1' },
  { id: 'field2', fieldName: 'field2', displayName: 'Field 2', valueDelimiter: '|',
    rootContentItemId: 'item1' },
  { id: 'field3', fieldName: 'field3', displayName: 'Field 3', valueDelimiter: '|',
    rootContentItemId: 'item1' },
];
const values: ReductionFieldValue[] = [
  { id: 'value1', value: 'value1', reductionFieldId: 'field1' },
  { id: 'value2', value: 'value2', reductionFieldId: 'field2' },
  { id: 'value3', value: 'value3', reductionFieldId: 'field2' },
  { id: 'value4', value: 'value4', reductionFieldId: 'field3' },
  { id: 'value5', value: 'value5', reductionFieldId: 'field3' },
  { id: 'value6', value: 'value6', reductionFieldId: 'field3' },
];

const clientCards = {
  client1: {
    expanded: false,
    profitCenterModalOpen: false,
  },
  client2: {
    expanded: false,
    profitCenterModalOpen: false,
  },
};
const itemCards = {
  item1: {
    expanded: false,
    profitCenterModalOpen: false,
  },
  item2: {
    expanded: false,
    profitCenterModalOpen: false,
  },
  item3: {
    expanded: false,
    profitCenterModalOpen: false,
  },
};
const groupCards = {
  group1: {
    expanded: false,
    profitCenterModalOpen: false,
  },
};
const initialState = {
  data: { clients, items, groups, users, fields, values },
  clientPanel: { cards: clientCards, selectedCard: null },
  itemPanel: { cards: itemCards, selectedCard: null },
  groupPanel: { cards: groupCards, selectedCard: null },
};

export function contentAccessAdmin(state: ContentAccessAdminState = initialState, action: Action) {
  const cardId = (action as ActionWithId).id;
  switch (action.type) {
    case 'SELECT_CARD_CLIENT':
      return {
        ...state,
        clientPanel: {
          ...state.clientPanel,
          selectedCard: cardId === state.clientPanel.selectedCard ? null : cardId,
        },
        itemPanel: {
          ...state.itemPanel,
          selectedCard: null,
        },
        groupPanel: {
          ...state.groupPanel,
          selectedCard: null,
        },
      };
    case 'SELECT_CARD_ITEM':
      return {
        ...state,
        itemPanel: {
          ...state.itemPanel,
          selectedCard: cardId === state.itemPanel.selectedCard ? null : cardId,
        },
        groupPanel: {
          ...state.groupPanel,
          selectedCard: null,
        },
      };
    case 'SELECT_CARD_GROUP':
      return {
        ...state,
        groupPanel: {
          ...state.groupPanel,
          selectedCard: cardId === state.groupPanel.selectedCard ? null : cardId,
        },
      };
    case 'NOP':
    default:
      return state;
  }
}
