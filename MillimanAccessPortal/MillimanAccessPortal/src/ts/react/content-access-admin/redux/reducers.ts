import * as _ from 'lodash';
import { reducer as toastrReducer } from 'react-redux-toastr';
import { combineReducers } from 'redux';

import { Guid } from '../../models';
import { CardAttributes } from '../../shared-components/card/card';
import { AccessAction, DataSuffixes } from './actions';
import {
    AccessStateData, AccessStateSelected, FilterState, ModalState, PendingDataState,
    PendingGroupState, PendingGroupUserState,
} from './store';

const _initialData: AccessStateData = {
  clients: {},
  items: {},
  groups: {},
  users: {},
  fields: {},
  values: {},
  contentTypes: {},
  publications: {},
  publicationQueue: {},
  reductions: {},
  reductionQueue: {},
};
const _initialCards = new Map<Guid, CardAttributes>([]);
const _initialPendingData: PendingDataState = {
  clients: false,
  items: false,
  groups: false,
  selections: false,
  createGroup: false,
  updateGroup: false,
  deleteGroup: false,
  suspendGroup: false,
  updateSelections: false,
  cancelReduction: false,
};
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
  return (state: TState = initialState, action: any) => {
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
const createModalReducer = (openActions: string[], closeActions: string[]) => {
  const handlers: Handlers<ModalState, any> = {};
  openActions.forEach((action) => {
    handlers[action] = (state) => ({
      ...state,
      isOpen: true,
    });
  });
  closeActions.forEach((action) => {
    handlers[action] = (state) => ({
      ...state,
      isOpen: false,
    });
  });
  return createReducer<ModalState>({ isOpen: false }, handlers);
};
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
      Object.keys(action.payload.groups).forEach((group) => {
        if (!clone.has(group)) {
          clone.set(group, {});
        }
      });
      return clone;
    },
  },
);
const pendingData = createReducer<PendingDataState>(_initialPendingData, {
  [AccessAction.FetchClients]: (state) => ({
    ...state,
    clients: true,
  }),
  [AccessAction.FetchClients + DataSuffixes.Succeeded]: (state) => ({
    ...state,
    clients: false,
  }),
  [AccessAction.FetchClients + DataSuffixes.Failed]: (state) => ({
    ...state,
    clients: false,
  }),
  [AccessAction.FetchItems]: (state) => ({
    ...state,
    items: true,
  }),
  [AccessAction.FetchItems + DataSuffixes.Succeeded]: (state) => ({
    ...state,
    items: false,
  }),
  [AccessAction.FetchItems + DataSuffixes.Failed]: (state) => ({
    ...state,
    items: false,
  }),
  [AccessAction.FetchGroups]: (state) => ({
    ...state,
    groups: true,
  }),
  [AccessAction.FetchGroups + DataSuffixes.Succeeded]: (state) => ({
    ...state,
    groups: false,
  }),
  [AccessAction.FetchGroups + DataSuffixes.Failed]: (state) => ({
    ...state,
    groups: false,
  }),
  [AccessAction.FetchSelections]: (state) => ({
    ...state,
    selections: true,
  }),
  [AccessAction.FetchSelections + DataSuffixes.Succeeded]: (state) => ({
    ...state,
    selections: false,
  }),
  [AccessAction.FetchSelections + DataSuffixes.Failed]: (state) => ({
    ...state,
    selections: false,
  }),
  [AccessAction.CreateGroup]: (state) => ({
    ...state,
    createGroup: true,
  }),
  [AccessAction.CreateGroup + DataSuffixes.Succeeded]: (state) => ({
    ...state,
    createGroup: false,
  }),
  [AccessAction.CreateGroup + DataSuffixes.Failed]: (state) => ({
    ...state,
    createGroup: false,
  }),
  [AccessAction.DeleteGroup]: (state) => ({
    ...state,
    deleteGroup: true,
  }),
  [AccessAction.DeleteGroup + DataSuffixes.Succeeded]: (state) => ({
    ...state,
    deleteGroup: false,
  }),
  [AccessAction.DeleteGroup + DataSuffixes.Failed]: (state) => ({
    ...state,
    deleteGroup: false,
  }),
  [AccessAction.SuspendGroup]: (state) => ({
    ...state,
    suspendGroup: true,
  }),
  [AccessAction.SuspendGroup + DataSuffixes.Succeeded]: (state) => ({
    ...state,
    suspendGroup: false,
  }),
  [AccessAction.SuspendGroup + DataSuffixes.Failed]: (state) => ({
    ...state,
    suspendGroup: false,
  }),
  [AccessAction.UpdateSelections]: (state) => ({
    ...state,
    updateSelections: true,
  }),
  [AccessAction.UpdateSelections + DataSuffixes.Succeeded]: (state) => ({
    ...state,
    updateSelections: false,
  }),
  [AccessAction.UpdateSelections + DataSuffixes.Failed]: (state) => ({
    ...state,
    updateSelections: false,
  }),
  [AccessAction.CancelReduction]: (state) => ({
    ...state,
    cancelReduction: true,
  }),
  [AccessAction.CancelReduction + DataSuffixes.Succeeded]: (state) => ({
    ...state,
    cancelReduction: false,
  }),
  [AccessAction.CancelReduction + DataSuffixes.Failed]: (state) => ({
    ...state,
    cancelReduction: false,
  }),
});
const pendingIsMaster = createReducer<boolean>(null, {
  [AccessAction.SetPendingIsMaster]: (_state, action) => action.isMaster,
  [AccessAction.SelectGroup]: () => null,
  [AccessAction.UpdateSelections + DataSuffixes.Succeeded]: () => null,
  [AccessAction.CancelReduction + DataSuffixes.Succeeded]: () => null,
});
const pendingSelections = createReducer<Map<Guid, { selected: boolean }>>(new Map(), {
  [AccessAction.SetPendingSelectionOn]: (state, action) =>
    updateMap(state, action.id, { selected: true }),
  [AccessAction.SetPendingSelectionOff]: (state, action) =>
    updateMap(state, action.id, { selected: false }),
  [AccessAction.SelectGroup]: () => new Map(),
  [AccessAction.UpdateSelections + DataSuffixes.Succeeded]: () => new Map(),
  [AccessAction.CancelReduction + DataSuffixes.Succeeded]: () => new Map(),
});
const pendingNewGroupName = createReducer<string>('', {
  [AccessAction.SetPendingNewGroupName]: (_state, action) => action.name,
});
const pendingGroups = createReducer<PendingGroupState>(_initialPendingGroups, {
  [AccessAction.SetGroupEditingOn]: (state, action) => ({
    ...state,
    id: action.id,
  }),
  [AccessAction.UpdateGroup + DataSuffixes.Succeeded]: () => _initialPendingGroups,
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
const pendingDeleteGroup = createReducer<Guid>(null, {
  [AccessAction.OpenDeleteGroupModal]: (_state, action) => action.id,
  [AccessAction.CloseDeleteGroupModal]: () => null,
  [AccessAction.DeleteGroup + DataSuffixes.Succeeded]: () => null,
});

const data = createReducer<AccessStateData>(_initialData, {
  [AccessAction.FetchClients + DataSuffixes.Succeeded]: (state, action) => ({
    ...state,
    clients: action.payload.clients,
    users: action.payload.users,
  }),
  [AccessAction.FetchItems + DataSuffixes.Succeeded]: (state, action) => {
    const { contentItems, contentTypes, publications, publicationQueue, clientStats } = action.payload;
    return {
      ...state,
      items: contentItems,
      contentTypes,
      publications,
      publicationQueue,
      clients: {
        ...state.clients,
        [clientStats.id]: {
          ...state.clients[clientStats.id],
          ...clientStats,
        },
      },
    };
  },
  [AccessAction.FetchGroups + DataSuffixes.Succeeded]: (state, action) => {
    const { groups, reductions, reductionQueue, contentItemStats, clientStats } = action.payload;
    return {
      ...state,
      groups,
      reductions,
      reductionQueue,
      items: {
        ...state.items,
        [contentItemStats.id]: {
          ...state.items[contentItemStats.id],
          ...contentItemStats,
        },
      },
      clients: {
        ...state.clients,
        [clientStats.id]: {
          ...state.clients[clientStats.id],
          ...clientStats,
        },
      },
    };
  },
  [AccessAction.FetchSelections + DataSuffixes.Succeeded]: (state, action) => {
    const { id, liveSelections, reductionSelections, fields, values } = action.payload;
    const reduction = _.find(state.reductions, (r) => r.selectionGroupId === id);
    const reductions = reduction
      ? {
        ...state.reductions,
        [reduction.id]: {
          ...state.reductions[reduction.id],
          selectedValues: reductionSelections,
        },
      }
      : { ...state.reductions };
    return {
      ...state,
      groups: {
        ...state.groups,
        [id]: {
          ...state.groups[id],
          selectedValues: liveSelections,
        },
      },
      reductions,
      fields,
      values,
    };
  },
  [AccessAction.FetchStatusRefresh + DataSuffixes.Succeeded]: (state, action) => {
    const { liveSelectionsSet } = action.payload;
    const groups = { ...state.groups };
    const items = { ...state.items };
    _.forEach(liveSelectionsSet, (liveSelections, groupId) => {
      if (groups[groupId]) {
        groups[groupId].selectedValues = liveSelections;
      }
    });
    _.forEach(groups, (group, groupId) => {
      if (action.payload.groups[groupId]) {
        groups[groupId] = {
            ...group,
            ...action.payload.groups[groupId],
        };
      }
    });
    _.forEach(items, (item, itemId) => {
      if (action.payload.groups[itemId]) {
        groups[itemId] = {
            ...item,
            ...action.payload.groups[itemId],
        };
      }
    });

    return {
      ...state,
      groups,
      items,
      publications: action.payload.publications,
      publicationQueue: action.payload.publicationQueue,
      reductions: action.payload.reductions,
      reductionQueue: action.payload.reductionQueue,
    };
  },
  [AccessAction.CreateGroup + DataSuffixes.Succeeded]: (state, action) => {
    const { group, contentItemStats } = action.payload;
    return {
      ...state,
      groups: {
        ...state.groups,
        [group.id]: {
          ...group,
        },
      },
      items: {
        ...state.items,
        [contentItemStats.id]: {
          ...state.items[contentItemStats.id],
          ...contentItemStats,
        },
      },
    };
  },
  [AccessAction.UpdateGroup + DataSuffixes.Succeeded]: (state, action) => {
    const { group, contentItemStats } = action.payload;
    return {
      ...state,
      groups: {
        ...state.groups,
        [group.id]: {
          ...state.groups[group.id],
          ...group,
        },
      },
      items: {
        ...state.items,
        [contentItemStats.id]: {
          ...state.items[contentItemStats.id],
          ...contentItemStats,
        },
      },
    };
  },
  [AccessAction.DeleteGroup + DataSuffixes.Succeeded]: (state, action) => {
    const { groupId, contentItemStats } = action.payload;
    const groups = { ...state.groups };
    delete groups[groupId];
    return {
      ...state,
      groups,
      items: {
        ...state.items,
        [contentItemStats.id]: {
          ...state.items[contentItemStats.id],
          ...contentItemStats,
        },
      },
    };
  },
  [AccessAction.SuspendGroup + DataSuffixes.Succeeded]: (state, action) => {
    const group = action.payload;
    return {
      ...state,
      groups: {
        ...state.groups,
        [group.id]: {
          ...state.groups[group.id],
          ...group,
        },
      },
    };
  },
  [AccessAction.UpdateSelections + DataSuffixes.Succeeded]: (state, action) => {
    const { group, reduction, reductionQueue: queue, liveSelections } = action.payload;
    const reductions = reduction
      ? {
        ...state.reductions,
        [reduction.id]: {
          ...state.reductions[reduction.id],
          ...reduction,
        },
      }
      : { ...state.reductions };
    const reductionQueue = queue
      ? {
        ...state.reductionQueue,
        [queue.reductionId]: {
          ...state.reductionQueue[queue.reductionId],
          ...queue,
        },
      }
      : { ...state.reductionQueue };
    return {
      ...state,
      groups: {
        ...state.groups,
        [group.id]: {
          ...state.groups[group.id],
          ...group,
          selectedValues: liveSelections,
        },
      },
      reductions,
      reductionQueue,
    };
  },
  [AccessAction.CancelReduction + DataSuffixes.Succeeded]: (state, action) => {
    const { group, reduction, reductionQueue: queue } = action.payload;
    const reductions = reduction
      ? {
        ...state.reductions,
        [reduction.id]: {
          ...state.reductions[reduction.id],
          ...reduction,
        },
      }
      : { ...state.reductions };
    const reductionQueue = queue
      ? {
        ...state.reductionQueue,
        [queue.reductionId]: {
          ...state.reductionQueue[queue.reductionId],
          ...queue,
        },
      }
      : { ...state.reductionQueue };
    return {
      ...state,
      groups: {
        ...state.groups,
        [group.id]: {
          ...state.groups[group.id],
          ...group,
        },
      },
      reductions,
      reductionQueue,
    };
  },
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
  data: pendingData,
  isMaster: pendingIsMaster,
  selections: pendingSelections,
  newGroupName: pendingNewGroupName,
  group: pendingGroups,
  deleteGroup: pendingDeleteGroup,
});
const filters = combineReducers({
  client: createFilterReducer(AccessAction.SetFilterTextClient),
  item: createFilterReducer(AccessAction.SetFilterTextItem),
  group: createFilterReducer(AccessAction.SetFilterTextGroup),
  selections: createFilterReducer(AccessAction.SetFilterTextSelections),
});
const modals = combineReducers({
  addGroup: createModalReducer([ AccessAction.OpenAddGroupModal ], [
    AccessAction.CloseAddGroupModal,
    AccessAction.CreateGroup + DataSuffixes.Succeeded,
    AccessAction.CreateGroup + DataSuffixes.Failed,
  ]),
  deleteGroup: createModalReducer([ AccessAction.OpenDeleteGroupModal ], [
    AccessAction.CloseDeleteGroupModal,
    AccessAction.DeleteGroup + DataSuffixes.Succeeded,
    AccessAction.DeleteGroup + DataSuffixes.Failed,
  ]),
  invalidate: createModalReducer([ AccessAction.OpenInvalidateModal ], [
    AccessAction.CloseInvalidateModal,
    AccessAction.UpdateSelections + DataSuffixes.Succeeded,
    AccessAction.UpdateSelections + DataSuffixes.Failed,
  ]),
});

export const contentAccessAdmin = combineReducers({
  data,
  selected,
  cardAttributes,
  pending,
  filters,
  modals,
  toastr: toastrReducer,
});
