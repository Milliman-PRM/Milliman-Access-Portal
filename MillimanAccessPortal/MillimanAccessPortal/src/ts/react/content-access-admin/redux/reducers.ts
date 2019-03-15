import * as _ from 'lodash';
import { reducer as toastrReducer } from 'react-redux-toastr';
import { combineReducers } from 'redux';

import { Guid } from '../../models';
import { CardAttributes } from '../../shared-components/card/card';
import * as AccessActions from './actions';
import { AccessAction, FilterAction, OpenAction } from './actions';
import {
    AccessStateData, AccessStateSelected, Dict, FilterState, ModalState, PendingDataState,
    PendingGroupState,
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
  users: {},
};

// An object of actions and their state transformations
type Handlers<TState, TAction extends AccessAction> = {
  [type in TAction['type']]?: (state: TState, action: TAction) => TState;
};

/**
 * Create a reducer for a subtree of the redux store
 * @param initialState Subtree of state these handlers can influence
 * @param handlers Actions and their state transformations
 */
const createReducer =
  <TState, TAction extends AccessAction = AccessAction>
  (initialState: TState, handlers: Handlers<TState, TAction>) =>
    (state: TState = initialState, action: TAction) => action.type in handlers
      ? handlers[action.type](state, action)
      : state;
/**
 * Create a reducer for a filter
 * @param actionType Single filter action
 */
const createFilterReducer = (actionType: FilterAction['type']) =>
  createReducer({ text: '' }, {
    [actionType]: (state: FilterState, action: FilterAction) => ({
      ...state,
      text: action.text,
    }),
  });
/**
 * Create a reducer for a modal
 * @param openActions Actions that cause the modal to open
 * @param closeActions Actions that cause the modal to close
 */
const createModalReducer = (openActions: Array<OpenAction['type']>, closeActions: Array<AccessAction['type']>) => {
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

const clientCardAttributes = createReducer<Dict<CardAttributes>>({},
  {
    FETCH_CLIENTS_SUCCEEDED: (__, { response }: AccessActions.FetchClientsSucceeded) => ({
      ..._.mapValues(response.clients, () => ({ disabled: false })),
      ..._.mapValues(response.parentClients, () => ({ disabled: true })),
    }),
  },
);
const groupCardAttributes = createReducer<Dict<CardAttributes>>({},
  {
    SET_GROUP_EDITING_ON: (state, action: AccessActions.SetGroupEditingOn) => ({
      ...state,
      [action.id]: {
        expanded: true,
      },
    }),
    SET_EXPANDED_GROUP: (state, action: AccessActions.SetExpandedGroup) => ({
      ...state,
      [action.id]: {
        expanded: true,
      },
    }),
    SET_COLLAPSED_GROUP: (state, action: AccessActions.SetCollapsedGroup) => ({
      ...state,
      [action.id]: {
        expanded: false,
      },
    }),
    SET_ALL_EXPANDED_GROUP: (state) =>
      _.mapValues(state, (group) => ({
        ...group,
        expanded: true,
      })),
    SET_ALL_COLLAPSED_GROUP: (state) =>
      _.mapValues(state, (group) => ({
        ...group,
        expanded: false,
      })),
    FETCH_GROUPS_SUCCEEDED: (_state, action: AccessActions.FetchGroupsSucceeded) => {
      const state: Dict<CardAttributes> = {};
      Object.keys(action.response.groups).forEach((group) => {
        state[group] = {};
      });
      return state;
    },
  },
);
const pendingData = createReducer<PendingDataState>(_initialPendingData, {
  FETCH_CLIENTS: (state) => ({
    ...state,
    clients: true,
  }),
  FETCH_CLIENTS_SUCCEEDED: (state) => ({
    ...state,
    clients: false,
  }),
  FETCH_CLIENTS_FAILED: (state) => ({
    ...state,
    clients: false,
  }),
  FETCH_ITEMS: (state) => ({
    ...state,
    items: true,
  }),
  FETCH_ITEMS_SUCCEEDED: (state) => ({
    ...state,
    items: false,
  }),
  FETCH_ITEMS_FAILED: (state) => ({
    ...state,
    items: false,
  }),
  FETCH_GROUPS: (state) => ({
    ...state,
    groups: true,
  }),
  FETCH_GROUPS_SUCCEEDED: (state) => ({
    ...state,
    groups: false,
  }),
  FETCH_GROUPS_FAILED: (state) => ({
    ...state,
    groups: false,
  }),
  FETCH_SELECTIONS: (state) => ({
    ...state,
    selections: true,
  }),
  FETCH_SELECTIONS_SUCCEEDED: (state) => ({
    ...state,
    selections: false,
  }),
  FETCH_SELECTIONS_FAILED: (state) => ({
    ...state,
    selections: false,
  }),
  CREATE_GROUP: (state) => ({
    ...state,
    createGroup: true,
  }),
  CREATE_GROUP_SUCCEEDED: (state) => ({
    ...state,
    createGroup: false,
  }),
  CREATE_GROUP_FAILED: (state) => ({
    ...state,
    createGroup: false,
  }),
  DELETE_GROUP: (state) => ({
    ...state,
    deleteGroup: true,
  }),
  DELETE_GROUP_SUCCEEDED: (state) => ({
    ...state,
    deleteGroup: false,
  }),
  DELETE_GROUP_FAILED: (state) => ({
    ...state,
    deleteGroup: false,
  }),
  SUSPEND_GROUP: (state) => ({
    ...state,
    suspendGroup: true,
  }),
  SUSPEND_GROUP_SUCCEEDED: (state) => ({
    ...state,
    suspendGroup: false,
  }),
  SUSPEND_GROUP_FAILED: (state) => ({
    ...state,
    suspendGroup: false,
  }),
  UPDATE_SELECTIONS: (state) => ({
    ...state,
    updateSelections: true,
  }),
  UPDATE_SELECTIONS_SUCCEEDED: (state) => ({
    ...state,
    updateSelections: false,
  }),
  UPDATE_SELECTIONS_FAILED: (state) => ({
    ...state,
    updateSelections: false,
  }),
  CANCEL_REDUCTION: (state) => ({
    ...state,
    cancelReduction: true,
  }),
  CANCEL_REDUCTION_SUCCEEDED: (state) => ({
    ...state,
    cancelReduction: false,
  }),
  CANCEL_REDUCTION_FAILED: (state) => ({
    ...state,
    cancelReduction: false,
  }),
});
const pendingStatusTries = createReducer<number>(5, {
  DECREMENT_STATUS_REFRESH_ATTEMPTS: (state) => state ? state - 1 : 0,
  FETCH_STATUS_REFRESH_SUCCEEDED: () => 5,
});
const pendingIsMaster = createReducer<boolean>(null, {
  SET_PENDING_IS_MASTER: (_state, action: AccessActions.SetPendingIsMaster) => action.isMaster,
  SELECT_GROUP: () => null,
  UPDATE_SELECTIONS_SUCCEEDED: () => null,
  CANCEL_REDUCTION_SUCCEEDED: () => null,
});
const pendingSelections = createReducer<Dict<{ selected: boolean }>>({}, {
  SET_PENDING_SELECTION_ON: (state, action: AccessActions.SetPendingSelectionOn) => ({
    ...state,
    [action.id]: {
      selected: true,
    },
  }),
  SET_PENDING_SELECTION_OFF: (state, action: AccessActions.SetPendingSelectionOff) => ({
    ...state,
    [action.id]: {
      selected: false,
    },
  }),
  SELECT_GROUP: () => ({}),
  UPDATE_SELECTIONS_SUCCEEDED: () => ({}),
  CANCEL_REDUCTION_SUCCEEDED: () => ({}),
});
const pendingNewGroupName = createReducer<string>('', {
  OPEN_ADD_GROUP_MODAL: (_state) => '',
  SET_PENDING_NEW_GROUP_NAME: (_state, action: AccessActions.SetPendingNewGroupName) => action.name,
});
const pendingGroups = createReducer<PendingGroupState>(_initialPendingGroups, {
  SET_GROUP_EDITING_ON: (state, action: AccessActions.SetGroupEditingOn) => ({
    ...state,
    id: action.id,
  }),
  UPDATE_GROUP_SUCCEEDED: () => _initialPendingGroups,
  SET_GROUP_EDITING_OFF: () => _initialPendingGroups,
  SET_PENDING_GROUP_NAME: (state, action: AccessActions.SetPendingGroupName) => ({
    ...state,
    name: action.name,
  }),
  SET_PENDING_GROUP_USER_QUERY: (state, action: AccessActions.SetPendingGroupUserQuery) => ({
    ...state,
    userQuery: action.query,
  }),
  SET_PENDING_GROUP_USER_ASSIGNED: (state, action: AccessActions.SetPendingGroupUserAssigned) => ({
    ...state,
    users: {
      ...state.users,
      [action.id]: {
        assigned: true,
      },
    },
    userQuery: '',
  }),
  SET_PENDING_GROUP_USER_REMOVED: (state, action: AccessActions.SetPendingGroupUserRemoved) => ({
    ...state,
    users: {
      ...state.users,
      [action.id]: {
        assigned: false,
      },
    },
  }),
});
const pendingDeleteGroup = createReducer<Guid>(null, {
  OPEN_DELETE_GROUP_MODAL: (_state, action: AccessActions.OpenDeleteGroupModal) => action.id,
  CLOSE_DELETE_GROUP_MODAL: () => null,
  DELETE_GROUP_SUCCEEDED: () => null,
});

const data = createReducer<AccessStateData>(_initialData, {
  FETCH_CLIENTS_SUCCEEDED: (state, action: AccessActions.FetchClientsSucceeded) => ({
    ...state,
    clients: {
      ...action.response.clients,
      ...action.response.parentClients,
    },
    users: action.response.users,
  }),
  FETCH_ITEMS_SUCCEEDED: (state, action: AccessActions.FetchItemsSucceeded) => {
    const { contentItems, contentTypes, publications, publicationQueue, clientStats } = action.response;
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
  FETCH_GROUPS_SUCCEEDED: (state, action: AccessActions.FetchGroupsSucceeded) => {
    const { groups, reductions, reductionQueue, contentItemStats, clientStats } = action.response;
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
  FETCH_SELECTIONS_SUCCEEDED: (state, action: AccessActions.FetchSelectionsSucceeded) => {
    const { id, liveSelections, reductionSelections, fields, values } = action.response;
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
  FETCH_STATUS_REFRESH_SUCCEEDED: (state, action: AccessActions.FetchStatusRefreshSucceeded) => {
    const { liveSelectionsSet } = action.response;
    const groups = { ...state.groups };
    const items = { ...state.items };
    _.forEach(liveSelectionsSet, (liveSelections, groupId) => {
      if (groups[groupId]) {
        groups[groupId].selectedValues = liveSelections;
      }
    });
    _.forEach(groups, (group, groupId) => {
      if (action.response.groups[groupId]) {
        groups[groupId] = {
            ...group,
            ...action.response.groups[groupId],
        };
      }
    });
    _.forEach(items, (item, itemId) => {
      if (action.response.contentItems[itemId]) {
        items[itemId] = {
          ...item,
          ...action.response.contentItems[itemId],
        };
      }
    });

    return {
      ...state,
      groups,
      items,
      publications: action.response.publications,
      publicationQueue: action.response.publicationQueue,
      reductions: action.response.reductions,
      reductionQueue: action.response.reductionQueue,
    };
  },
  CREATE_GROUP_SUCCEEDED: (state, action: AccessActions.CreateGroupSucceeded) => {
    const { group, contentItemStats } = action.response;
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
  UPDATE_GROUP_SUCCEEDED: (state, action: AccessActions.UpdateGroupSucceeded) => {
    const { group, contentItemStats } = action.response;
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
  DELETE_GROUP_SUCCEEDED: (state, action: AccessActions.DeleteGroupSucceeded) => {
    const { groupId, contentItemStats } = action.response;
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
  SUSPEND_GROUP_SUCCEEDED: (state, action: AccessActions.SuspendGroupSucceeded) => {
    const group = action.response;
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
  UPDATE_SELECTIONS_SUCCEEDED: (state, action: AccessActions.UpdateSelectionsSucceeded) => {
    const { group, reduction, reductionQueue: queue, liveSelections } = action.response;
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
  CANCEL_REDUCTION_SUCCEEDED: (state, action: AccessActions.CancelReductionSucceeded) => {
    const { group, reduction, reductionQueue: queue } = action.response;
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
    SELECT_CLIENT: (state, action: AccessActions.SelectClient) => ({
      client: action.id === state.client ? null : action.id,
      item: null,
      group: null,
    }),
    SELECT_ITEM: (state, action: AccessActions.SelectItem) => ({
      ...state,
      item: action.id === state.item ? null : action.id,
      group: null,
    }),
    SELECT_GROUP: (state, action: AccessActions.SelectGroup) => ({
      ...state,
      group: action.id === state.group ? null : action.id,
    }),
    SET_GROUP_EDITING_ON: (state, action: AccessActions.SetGroupEditingOn) => ({
      ...state,
      group: action.id === state.group ? action.id : null,
    }),
  },
);
const cardAttributes = combineReducers({
  client: clientCardAttributes,
  group: groupCardAttributes,
});
const pending = combineReducers({
  data: pendingData,
  statusTries: pendingStatusTries,
  isMaster: pendingIsMaster,
  selections: pendingSelections,
  newGroupName: pendingNewGroupName,
  group: pendingGroups,
  deleteGroup: pendingDeleteGroup,
});
const filters = combineReducers({
  client: createFilterReducer('SET_FILTER_TEXT_CLIENT'),
  item: createFilterReducer('SET_FILTER_TEXT_ITEM'),
  group: createFilterReducer('SET_FILTER_TEXT_GROUP'),
  selections: createFilterReducer('SET_FILTER_TEXT_SELECTIONS'),
});
const modals = combineReducers({
  addGroup: createModalReducer([ 'OPEN_ADD_GROUP_MODAL' ], [
    'CLOSE_ADD_GROUP_MODAL',
    'CREATE_GROUP_SUCCEEDED',
    'CREATE_GROUP_FAILED',
  ]),
  deleteGroup: createModalReducer([ 'OPEN_DELETE_GROUP_MODAL' ], [
    'CLOSE_DELETE_GROUP_MODAL',
    'DELETE_GROUP_SUCCEEDED',
    'DELETE_GROUP_FAILED',
  ]),
  invalidate: createModalReducer([ 'OPEN_INACTIVE_MODAL' ], [
    'CLOSE_INACTIVE_MODAL',
    'UPDATE_SELECTIONS_SUCCEEDED',
    'UPDATE_SELECTIONS_FAILED',
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
