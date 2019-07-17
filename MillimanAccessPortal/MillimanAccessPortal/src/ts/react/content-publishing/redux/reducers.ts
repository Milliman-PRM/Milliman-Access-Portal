import * as _ from 'lodash';
import { reducer as toastrReducer } from 'react-redux-toastr';
import { combineReducers } from 'redux';

import { CardAttributes } from '../../shared-components/card/card';
import { createReducerCreator } from '../../shared-components/redux/reducers';
import { Dict, FilterState } from '../../shared-components/redux/store';
import * as AccessActions from './actions';
import { FilterPublishingAction, PublishingAction } from './actions';
import { PendingDataState, PublishingStateData, PublishingStateSelected } from './store';

const _initialData: PublishingStateData = {
  clients: {},
  items: {},
  users: {},
  contentTypes: {},
  publications: {},
  publicationQueue: {},
  reductions: {},
  reductionQueue: {},
};
const _initialPendingData: PendingDataState = {
  clients: false,
  items: false,
};

/**
 * Create reducers for a subtree of the redux store
 * @param initialState Subtree of state these handlers can influence
 * @param handlers Actions and their state transformations
 */
const createReducer = createReducerCreator<PublishingAction>();

/**
 * Create a reducer for a filter
 * @param actionType Single filter action
 */
const createFilterReducer = (actionType: FilterPublishingAction['type']) =>
  createReducer({ text: '' }, {
    [actionType]: (state: FilterState, action: FilterPublishingAction) => ({
      ...state,
      text: action.text,
    }),
  });

const clientCardAttributes = createReducer<Dict<CardAttributes>>({},
  {
    FETCH_CLIENTS_SUCCEEDED: (__, { response }: AccessActions.FetchClientsSucceeded) => ({
      ..._.mapValues(response.clients, () => ({ disabled: false })),
      ..._.mapValues(response.parentClients, () => ({ disabled: true })),
    }),
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
});

const pendingStatusTries = createReducer<number>(5, {
  DECREMENT_STATUS_REFRESH_ATTEMPTS: (state) => state ? state - 1 : 0,
  FETCH_STATUS_REFRESH_SUCCEEDED: () => 5,
});

const data = createReducer<PublishingStateData>(_initialData, {
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
  FETCH_STATUS_REFRESH_SUCCEEDED: (state, action: AccessActions.FetchStatusRefreshSucceeded) => {
    const items = { ...state.items };
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
      items,
      publications: action.response.publications,
      publicationQueue: action.response.publicationQueue,
      reductions: action.response.reductions,
      reductionQueue: action.response.reductionQueue,
    };
  },
});

const selected = createReducer<PublishingStateSelected>(
  {
    client: null,
    item: null,
  },
  {
    SELECT_CLIENT: (state, action: AccessActions.SelectClient) => ({
      client: action.id === state.client ? null : action.id,
      item: null,
    }),
    SELECT_ITEM: (state, action: AccessActions.SelectItem) => ({
      ...state,
      item: action.id === state.item ? null : action.id,
      group: null,
    }),
  },
);

const cardAttributes = combineReducers({
  client: clientCardAttributes,
});

const pending = combineReducers({
  data: pendingData,
  statusTries: pendingStatusTries,
});

const filters = combineReducers({
  client: createFilterReducer('SET_FILTER_TEXT_CLIENT'),
  item: createFilterReducer('SET_FILTER_TEXT_ITEM'),
});

export const contentPublishing = combineReducers({
  data,
  selected,
  cardAttributes,
  pending,
  filters,
  toastr: toastrReducer,
});
