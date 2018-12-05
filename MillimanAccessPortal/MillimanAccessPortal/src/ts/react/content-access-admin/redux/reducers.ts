import { combineReducers } from 'redux';

import { PublicationStatus, ReductionStatus } from '../../../view-models/content-publishing';
import { Guid } from '../../models';
import { Card, CardAttributes } from '../../shared-components/card/card';
import { AccessAction } from './actions';
import {
  AccessStateData, AccessStateSelected, FilterState, ModalState, PendingGroupState,
  PendingGroupUserState,
} from './store';

const _initialData: AccessStateData = {
  clients: [
    { id: 'client1', name: 'client1', code: 'c1', eligibleUsers: ['user1', 'user2', 'user3', 'user4', 'user5'] },
    { id: 'client2', name: 'client2', code: 'c2', eligibleUsers: ['user1', 'user2', 'user3', 'user4', 'user5'] },
    { id: 'client3', name: 'client3', code: 'c3', eligibleUsers: ['user1', 'user2', 'user3'] },
    { id: 'client4', name: 'client4', code: 'c4', eligibleUsers: [] },
  ],
  items: [
    { id: 'item1', clientId: 'client1', contentTypeId: '1', name: 'item1', doesReduce: true, isSuspended: false },
    { id: 'item2', clientId: 'client1', contentTypeId: '1', name: 'item2', doesReduce: true, isSuspended: false },
    { id: 'item3', clientId: 'client1', contentTypeId: '1', name: 'item3', doesReduce: true, isSuspended: true },
    { id: 'item4', clientId: 'client2', contentTypeId: '1', name: 'item4', doesReduce: true, isSuspended: false },
    { id: 'item5', clientId: 'client2', contentTypeId: '1', name: 'item5', doesReduce: true, isSuspended: false },
    { id: 'item6', clientId: 'client3', contentTypeId: '1', name: 'item6', doesReduce: false, isSuspended: false },
  ],
  groups: [
    { id: 'group1', rootContentItemId: 'item1', name: 'group1', isMaster: true, isSuspended: false,
      selectedValues: [], assignedUsers: [ 'user1', 'user2' ] },
    { id: 'group2', rootContentItemId: 'item1', name: 'group2', isMaster: false, isSuspended: true,
      selectedValues: [], assignedUsers: [ 'user3' ] },
    { id: 'group3', rootContentItemId: 'item1', name: 'group3', isMaster: false, isSuspended: false,
      selectedValues: [], assignedUsers: [] },
    { id: 'group4', rootContentItemId: 'item2', name: 'group4', isMaster: false, isSuspended: false,
      selectedValues: [], assignedUsers: [] },
    { id: 'group5', rootContentItemId: 'item3', name: 'group5', isMaster: false, isSuspended: false,
      selectedValues: [], assignedUsers: [] },
  ],
  users: [
    { id: 'user1', firstName: 'Ichi', lastName: 'One',   userName: 'user1', email: 'user1@a.a',
      activated: true, isSuspended: false },
    { id: 'user2', firstName: 'Ni',   lastName: 'Two',   userName: 'user2', email: 'user2@a.a',
      activated: true, isSuspended: false },
    { id: 'user3', firstName: 'San',  lastName: 'Three', userName: 'user3', email: 'user3@a.a',
      activated: true, isSuspended: false },
    { id: 'user4', firstName: 'Shi',  lastName: 'Four',  userName: 'user4', email: 'user4@a.a',
      activated: true, isSuspended: false },
    { id: 'user5', firstName: 'Go',   lastName: 'Five',  userName: 'user5', email: 'user5@a.a',
      activated: true, isSuspended: false },
  ],
  fields: [
    { id: 'field1', fieldName: 'field1', displayName: 'field1', rootContentItemId: 'item1', valueDelimiter: '' },
    { id: 'field2', fieldName: 'field2', displayName: 'field2', rootContentItemId: 'item1', valueDelimiter: '' },
    { id: 'field3', fieldName: 'field3', displayName: 'field3', rootContentItemId: 'item1', valueDelimiter: '' },
  ],
  values: [
    { id: 'value1', reductionFieldId: 'field1', value: 'value1' },
    { id: 'value2', reductionFieldId: 'field1', value: 'value2' },
    { id: 'value3', reductionFieldId: 'field1', value: 'value3' },
    { id: 'value4', reductionFieldId: 'field2', value: 'value4' },
    { id: 'value5', reductionFieldId: 'field2', value: 'value5' },
  ],
  contentTypes: [
    { id: '1', name: 'QlikView', canReduce: true, fileExtensions: [] },
    { id: '2', name: 'HTML', canReduce: false, fileExtensions: [] },
    { id: '3', name: 'PDF', canReduce: false, fileExtensions: [] },
  ],
  publications: [
    { id: 'p1', applicationUserId: 'user1', rootContentItemId: 'item2',
      createDateTimeUtc: '2018-04-02T00:00:00.000Z', requestStatus: PublicationStatus.Queued },
  ],
  publicationQueue: [
    { publicationId: 'p1', queuePosition: 2 },
  ],
  reductions: [
    { id: 'r1', applicationUserId: 'user2', selectionGroupId: 'group1',
      createDateTimeUtc: '2018-02-11T00:00:00.000Z', taskStatus: ReductionStatus.Queued,
      selectedValues: [] },
  ],
  reductionQueue: [
    { reductionId: 'r1', queuePosition: 1 },
  ],
};

const _initialCards = new Map<Guid, CardAttributes>([
  ['group1', {}],
  ['group2', {}],
  ['group3', {}],
  ['group4', {}],
  ['group5', {}],
]);
const _initialPendingGroups: PendingGroupState = {
  id: null,
  name: null,
  userQuery: '',
  users: new Map<Guid, PendingGroupUserState>(),
};

// utility functions
interface Handlers<TState, TAction> {
  [type: string]: (state: TState, action: TAction) => TState;
}
function createReducer<TState>(initialState: TState, handlers: Handlers<TState, any>) {
  return (state: TState = initialState, action) => {
    return action.type in handlers
      ? handlers[action.type](state, action)
      : state;
  };
}
const createFilterReducer = (actionType: AccessAction) =>
  createReducer<FilterState>({ text: '' }, {
    [actionType]: (state, action) => ({
      ...state,
      text: action.text,
    }),
  });
const createModalReducer = (openActionType: AccessAction, closeActionType: AccessAction) =>
  createReducer<ModalState>({ isOpen: false }, {
    [openActionType]: (state) => ({
      ...state,
      isOpen: true,
    }),
    [closeActionType]: (state) => ({
      ...state,
      isOpen: false,
    }),
  });
function updateList<T>(list: T[], selector: (item: T) => boolean, value?: T): T[] {
  const filtered = list.filter(selector);
  return value === undefined
    ? filtered
    : [...filtered, value].sort();
}
// Have to cast value to object and back because TypeScript does not support spread for generics
// (as of 3.1.4)
function updateMap<T extends object>(map: Map<Guid, T>, key: Guid, value: Partial<T>) {
  const clone = new Map<Guid, T>(map);
  return clone.set(key, { ...(clone.has(key) ? clone.get(key) : {}), ...(value as object) } as T);
}
function updateAllMap<T extends object>(map: Map<Guid, T>, value: Partial<T>) {
  const clone = new Map<Guid, T>(map);
  for (const key of clone.keys()) {
    clone.set(key, { ...(clone.has(key) ? clone.get(key) : {}), ...(value as object) } as T);
  }
  return clone;
}

const groupCardAttributes = createReducer<Map<Guid, CardAttributes>>(_initialCards,
  {
    [AccessAction.SetExpandedGroup]: (state, action) =>
      updateMap(state, action.id, { expanded: true }),
    [AccessAction.SetCollapsedGroup]: (state, action) =>
      updateMap(state, action.id, { expanded: false }),
    [AccessAction.SetAllExpandedGroup]: (state) =>
      updateAllMap(state, { expanded: true }),
    [AccessAction.SetAllCollapsedGroup]: (state) =>
      updateAllMap(state, { expanded: false }),
  },
);
const pendingIsMaster = createReducer<boolean>(false, {
  [AccessAction.SetPendingIsMaster]: (_, action) => action.isMaster,
});
const pendingSelections = createReducer<Guid[]>([], {
  [AccessAction.SetPendingSelectionOn]: (state, action) =>
    updateList(state, (i) => i !== action.id, action.id),
  [AccessAction.SetPendingSelectionOff]: (state, action) =>
    updateList(state, (i) => i !== action.id),
});
const pendingNewGroupName = createReducer<string>('', {
  [AccessAction.SetPendingNewGroupName]: (_, action) => action.name,
});
const pendingGroups = createReducer<PendingGroupState>(_initialPendingGroups, {
  [AccessAction.SetGroupEditingOn]: (state, action) => ({
    ...state,
    id: action.id,
  }),
  [AccessAction.SetGroupEditingOff]: () => _initialPendingGroups,
  [AccessAction.SetPendingGroupName]: (state, action) => ({
    ...state,
    name: action.name,
  }),
  [AccessAction.SetPendingGroupUserQuery]: (state, action) => ({
    ...state,
    userQuery: action.query,
  }),
  [AccessAction.SetPendingGroupUserAssigned]: (state, action) => ({
    ...state,
    users: updateMap(state.users, action.id, { assigned: true }),
    userQuery: '',
  }),
  [AccessAction.SetPendingGroupUserRemoved]: (state, action) => ({
    ...state,
    users: updateMap(state.users, action.id, { assigned: false }),
  }),
});

const data = createReducer<AccessStateData>(_initialData, {
});
const selected = createReducer<AccessStateSelected>(
  {
    client: null,
    item: null,
    group: null,
  },
  {
    [AccessAction.SelectClient]: (state, action) => ({
      client: action.id === state.client ? null : action.id,
      item: null,
      group: null,
    }),
    [AccessAction.SelectItem]: (state, action) => ({
      ...state,
      item: action.id === state.item ? null : action.id,
      group: null,
    }),
    [AccessAction.SelectGroup]: (state, action) => ({
      ...state,
      group: action.id === state.group ? null : action.id,
    }),
  },
);
const cardAttributes = combineReducers({
  group: groupCardAttributes,
});
const pending = combineReducers({
  isMaster: pendingIsMaster,
  selections: pendingSelections,
  newGroupName: pendingNewGroupName,
  group: pendingGroups,
});
const filters = combineReducers({
  client: createFilterReducer(AccessAction.SetFilterTextClient),
  item: createFilterReducer(AccessAction.SetFilterTextItem),
  group: createFilterReducer(AccessAction.SetFilterTextGroup),
  selections: createFilterReducer(AccessAction.SetFilterTextSelections),
});
const modals = combineReducers({
  addGroup: createModalReducer(AccessAction.OpenAddGroupModal, AccessAction.CloseAddGroupModal),
});

export const contentAccessAdmin = combineReducers({
  data,
  selected,
  cardAttributes,
  pending,
  filters,
  modals,
});
