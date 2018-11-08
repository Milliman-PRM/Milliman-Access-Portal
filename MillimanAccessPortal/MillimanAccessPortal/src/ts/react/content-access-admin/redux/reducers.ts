import { Action } from 'redux';

import { PublicationStatus, ReductionStatus } from '../../../view-models/content-publishing';
import {
  Client, ContentPublicationRequest, ContentReductionTask, Guid, PublicationQueueDetails,
  ReductionField, ReductionFieldValue, ReductionQueueDetails, RootContentItem, SelectionGroup, User,
} from '../../models';
import { ActionWithBoolean, ActionWithId } from './actions';
import { ContentAccessAdminState } from './store';

const _clients: Client[] = [
  { id: 'client1', name: 'client1', code: 'c1' },
  { id: 'client2', name: 'client2', code: 'c2' },
  { id: 'client3', name: 'client3', code: 'c3' },
  { id: 'client4', name: 'client4', code: 'c4' },
];
const _items: RootContentItem[] = [
  { id: 'item1', name: 'item1', isSuspended: false, doesReduce: true, clientId: 'client1' },
  { id: 'item2', name: 'item2', isSuspended: false, doesReduce: true, clientId: 'client1' },
  { id: 'item3', name: 'item3', isSuspended: false, doesReduce: false, clientId: 'client1' },
  { id: 'item4', name: 'item4', isSuspended: false, doesReduce: true, clientId: 'client2' },
  { id: 'item5', name: 'item5', isSuspended: false, doesReduce: false, clientId: 'client2' },
  { id: 'item6', name: 'item6', isSuspended: false, doesReduce: false, clientId: 'client4' },
];
const _groups: SelectionGroup[] = [
  { id: 'group1', name: 'group1', isSuspended: true, isMaster: false, rootContentItemId: 'item1',
    selectedValues: [ 'value1', 'value2', 'value3', 'value4', 'value5' ] },
  { id: 'group2', name: 'group2', isSuspended: false, isMaster: false, rootContentItemId: 'item1',
    selectedValues: [ 'value1' ] },
  { id: 'group3', name: 'group3', isSuspended: false, isMaster: false, rootContentItemId: 'item1',
    selectedValues: [ ] },
  { id: 'group4', name: 'group4', isSuspended: false, isMaster: true, rootContentItemId: 'item2',
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
const _users: User[] = [
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
const _fields: ReductionField[] = [
  { id: 'field1', fieldName: 'field1', displayName: 'Field 1', valueDelimiter: '|',
    rootContentItemId: 'item1' },
  { id: 'field2', fieldName: 'field2', displayName: 'Field 2', valueDelimiter: '|',
    rootContentItemId: 'item1' },
  { id: 'field3', fieldName: 'field3', displayName: 'Field 3', valueDelimiter: '|',
    rootContentItemId: 'item1' },
];
const _values: ReductionFieldValue[] = [
  { id: 'value1', value: 'value1', reductionFieldId: 'field1' },
  { id: 'value2', value: 'value2', reductionFieldId: 'field2' },
  { id: 'value3', value: 'value3', reductionFieldId: 'field2' },
  { id: 'value4', value: 'value4', reductionFieldId: 'field3' },
  { id: 'value5', value: 'value5', reductionFieldId: 'field3' },
  { id: 'value6', value: 'value6', reductionFieldId: 'field3' },
];
const _publications: ContentPublicationRequest[] = [
  { id: 'publication1', applicationUserId: 'user1', requestStatus: PublicationStatus.Queued,
    createDateTimeUtc: '', rootContentItemId: 'item2' },
];
const _publicationQueue: PublicationQueueDetails[] = [
  { publicationId: 'publication1', queuePosition: 1, queuedDurationMs: 42 },
];
const _reductions: ContentReductionTask[] = [
  { id: 'reduction1', applicationUserId: 'user1', contentPublicationRequestId: null,
    selectionGroupId: 'group1', reductionStatus: ReductionStatus.Queued, createDateTimeUtc: '' },
];
const _reductionQueue: ReductionQueueDetails[] = [
  { reductionId: 'reduction1', queuePosition: 1, queuedDurationMs: 42 },
];

const _clientCards = {
  client1: {
    expanded: false,
    profitCenterModalOpen: false,
  },
  client2: {
    expanded: false,
    profitCenterModalOpen: false,
  },
};
const _itemCards = {
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
const _groupCards = {
  group1: {
    expanded: false,
    profitCenterModalOpen: false,
  },
};
const _initialState: ContentAccessAdminState = {
  data: {
    clients: _clients,
    items: _items,
    groups: _groups,
    users: _users,
    fields: _fields,
    values: _values,
    publications: _publications,
    publicationQueue: _publicationQueue,
    reductions: _reductions,
    reductionQueue: _reductionQueue,
  },
  clientPanel: { cards: _clientCards, selectedCard: null },
  itemPanel: { cards: _itemCards, selectedCard: null },
  groupPanel: { cards: _groupCards, selectedCard: null },
  selectionsPanel: { isMaster: null, values: {} },
};

export function contentAccessAdmin(state: ContentAccessAdminState = _initialState, action: Action) {
  const id = (action as ActionWithId).id;
  switch (action.type) {
    case 'SELECT_CARD_CLIENT':
      return {
        ...state,
        clientPanel: {
          ...state.clientPanel,
          selectedCard: id === state.clientPanel.selectedCard ? null : id,
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
          selectedCard: id === state.itemPanel.selectedCard ? null : id,
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
          selectedCard: id === state.groupPanel.selectedCard ? null : id,
        },
        selectionsPanel: {
          isMaster: null,
          values: {},
        },
      };
    case 'SET_MASTER_SELECTED':
      return {
        ...state,
        selectionsPanel: {
          ...state.selectionsPanel,
          isMaster: (action as ActionWithBoolean).bValue,
        },
      };
    case 'SET_VALUE_SELECTED':
      return {
        ...state,
        selectionsPanel: {
          ...state.selectionsPanel,
          values: {
            ...state.selectionsPanel.values,
            [id]: (action as ActionWithBoolean).bValue,
          },
        },
      };
    case 'NOP':
    default:
      return state;
  }
}
