import { combineReducers } from 'redux';

import { Guid } from '../../models';
import { CardAttributes } from '../../shared-components/card/card';
import { AccessAction, DataSuffixes } from './actions';
import {
  AccessStateData, AccessStateSelected, FilterState, ModalState, PendingGroupState,
  PendingGroupUserState,
} from './store';

const _initialData: AccessStateData = {
  clients: [],
  items: [],
  groups: [],
  users: [],
  fields: [],
  values: [],
  contentTypes: [],
  publications: [],
  publicationQueue: [],
  reductions: [],
  reductionQueue: [],
};
const _initialCards = new Map<Guid, CardAttributes>([]);
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
    [AccessAction.FetchGroups + DataSuffixes.Succeeded]: (state, action) => {
      const clone = new Map(state);
      action.payload.groups.forEach((group) => {
        if (!clone.has(group.id)) {
          clone.set(group.id, {});
        }
      });
      return clone;
    },
  },
);
const pendingIsMaster = createReducer<boolean>(false, {
  [AccessAction.SetPendingIsMaster]: (_, action) => action.isMaster,
});
const pendingSelections = createReducer<Map<Guid, { selected: boolean }>>(new Map(), {
  [AccessAction.SetPendingSelectionOn]: (state, action) =>
    updateMap(state, action.id, { selected: true }),
  [AccessAction.SetPendingSelectionOff]: (state, action) =>
    updateMap(state, action.id, { selected: false }),
  [AccessAction.SelectGroup]: () => new Map(),
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
  [AccessAction.FetchClients + DataSuffixes.Succeeded]: (state, action) => ({
    ...state,
    clients: action.payload.clients,
    users: action.payload.users,
  }),
  [AccessAction.FetchItems + DataSuffixes.Succeeded]: (state, action) => ({
    ...state,
    items: action.payload.items,
    contentTypes: action.payload.contentTypes,
    publications: action.payload.publications,
    publicationQueue: action.payload.publicationQueue,
  }),
  [AccessAction.FetchGroups + DataSuffixes.Succeeded]: (state, action) => ({
    ...state,
    groups: action.payload.groups,
    reductions: action.payload.reductions,
    reductionQueue: action.payload.reductionQueue,
  }),
  [AccessAction.FetchSelections + DataSuffixes.Succeeded]: (state, action) => ({
    ...state,
    groups: state.groups.map((g) => {
      const grp = action.payload.groups.find((f) => f.id === g.id);
      return {
        ...g,
        selectedValues: grp ? grp.selectedValues : [],
      };
    }),
    fields: action.payload.fields,
    values: action.payload.values,
  }),
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
