import * as _ from 'lodash';
import { reducer as toastrReducer } from 'react-redux-toastr';
import { combineReducers } from 'redux';

import * as Action from './actions';
import * as State from './store';

import { Guid } from '../../models';
import { CardAttributes } from '../../shared-components/card/card';
import { createReducerCreator, Handlers } from '../../shared-components/redux/reducers';
import { Dict, FilterState, ModalState } from '../../shared-components/redux/store';

const defaultIfUndefined = (purpose: any, value: string, defaultValue = '') => {
  return (purpose !== undefined) && purpose.hasOwnProperty(value) ? purpose[value] : defaultValue;
};

const _initialData: State.FileDropDataState = {
  clients: {},
};

const _initialPendingData: State.FileDropPendingReturnState = {
  globalData: false,
  clients: false,
};

/**
 * Create reducers for a subtree of the redux store
 * @param initialState Subtree of state these handlers can influence
 * @param handlers Actions and their state transformations
 */
const createReducer = createReducerCreator<Action.FileDropActions>();

/**
 * Create a reducer for a filter
 * @param actionType Single filter action
 */
const createFilterReducer = (actionType: Action.FilterActions['type']) =>
  createReducer({ text: '' }, {
    [actionType]: (state: FilterState, action: Action.FilterActions) => ({
      ...state,
      text: action.text,
    }),
  });

const clientCardAttributes = createReducer<Dict<CardAttributes>>({},
  {
    FETCH_CLIENTS_SUCCEEDED: (__, { response }: Action.FetchClientsSucceeded) => ({
      ..._.mapValues(response.clients, (client) => ({ disabled: !client.canManage })),
    }),
  },
);

const pendingData = createReducer<State.FileDropPendingReturnState>(_initialPendingData, {
  FETCH_GLOBAL_DATA: (state) => ({
    ...state,
    globalData: true,
  }),
  FETCH_GLOBAL_DATA_SUCCEEDED: (state) => ({
    ...state,
    globalData: false,
  }),
  FETCH_GLOBAL_DATA_FAILED: (state) => ({
    ...state,
    globalData: false,
  }),
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
});

const pendingStatusTries = createReducer<number>(5, {
  DECREMENT_STATUS_REFRESH_ATTEMPTS: (state) => state ? state - 1 : 0,
  FETCH_STATUS_REFRESH_SUCCEEDED: () => 5,
});

const data = createReducer<State.FileDropDataState>(_initialData, {
  FETCH_GLOBAL_DATA_SUCCEEDED: (state, _action: Action.FetchGlobalDataSucceeded) => ({
    ...state,
  }),
  FETCH_CLIENTS_SUCCEEDED: (state, action: Action.FetchClientsSucceeded) => ({
    ...state,
    clients: {
      ...action.response.clients,
    },
  }),
});

const selected = createReducer<State.FileDropSelectedState>(
  {
    client: null,
  },
  {
    SELECT_CLIENT: (state, action: Action.SelectClient) => ({
      client: action.id === state.client ? null : action.id,
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
});

export const fileDropReducerState = combineReducers({
  data,
  selected,
  cardAttributes,
  pending,
  filters,
  toastr: toastrReducer,
});
