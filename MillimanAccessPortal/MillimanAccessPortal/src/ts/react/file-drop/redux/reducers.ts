import * as _ from 'lodash';
import { reducer as toastrReducer } from 'react-redux-toastr';
import { combineReducers } from 'redux';

import * as Action from './actions';
import * as State from './store';

import { Guid } from '../../models';
import { CardAttributes } from '../../shared-components/card/card';
import { createReducerCreator, Handlers } from '../../shared-components/redux/reducers';
import { Dict, FilterState, ModalState } from '../../shared-components/redux/store';

// ~~~~~~~~~~~~~~~~~
// Utility Functions
// ~~~~~~~~~~~~~~~~~

/**
 * Create reducers for a subtree of the redux store
 * @param initialState Subtree of state these handlers can influence
 * @param handlers Actions and their state transformations
 */
const createReducer = createReducerCreator<Action.FileDropActions>();

const defaultIfUndefined = (purpose: any, value: string, defaultValue = '') => {
  return (purpose !== undefined) && purpose.hasOwnProperty(value) ? purpose[value] : defaultValue;
};

// ~~~~~~~~~~~~~~~
// Default Objects
// ~~~~~~~~~~~~~~~

const _initialData: State.FileDropDataState = {
  clients: {},
  fileDrops: {},
};

const _initialPendingData: State.FileDropPendingReturnState = {
  globalData: false,
  clients: false,
  fileDrops: false,
  createFileDrop: false,
};

const _initialCreateFileDropData: State.CreateFileDropModalFormData = {
  clientId: null,
  fileDropName: null,
  fileDropDescription: null,
  errors: {
    fileDropName: null,
    fileDropDescription: null,
  },
};

// ~~~~~~~~~~~~~~~~
// Pending Reducers
// ~~~~~~~~~~~~~~~~

/** Reducer for the async state object in the pending state object */
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

/** Reducer for the statusTries value in the pending state object */
const pendingStatusTries = createReducer<number>(5, {
  DECREMENT_STATUS_REFRESH_ATTEMPTS: (state) => state ? state - 1 : 0,
  FETCH_STATUS_REFRESH_SUCCEEDED: () => 5,
});

/** Reducer for the Create File Drop modal form */
const pendingCreateFileDropForm = createReducer<State.CreateFileDropModalFormData>(_initialCreateFileDropData, {
  OPEN_CREATE_FILE_DROP_MODAL: (_state, action: Action.OpenCreateFileDropModal) => ({
    clientId: action.clientId,
    fileDropName: null,
    fileDropDescription: null,
    errors: {
      fileDropName: null,
      fileDropDescription: null,
    },
  }),
  CLOSE_CREATE_FILE_DROP_MODAL: () => ({
    clientId: null,
    fileDropName: null,
    fileDropDescription: null,
    errors: {
      fileDropName: null,
      fileDropDescription: null,
    },
  }),
  CREATE_FILE_DROP_SUCCEEDED: () => ({
    clientId: null,
    fileDropName: null,
    fileDropDescription: null,
    errors: {
      fileDropName: null,
      fileDropDescription: null,
    },
  }),
  UPDATE_CREATE_FILE_DROP_MODAL_FORM_VALUES: (state, action: Action.UpdateCreateFileDropModalFormValues) => ({
    ...state,
    [action.field]: action.value,
  }),
});

/** Reducer that combines the pending reducers */
const pending = combineReducers({
  async: pendingData,
  statusTries: pendingStatusTries,
  createFileDrop: pendingCreateFileDropForm,
});

// ~~~~~~~~~~~~~~~~
// Selected Reducers
// ~~~~~~~~~~~~~~~~

/** Reducer for the selected state object */
const selected = createReducer<State.FileDropSelectedState>(
  {
    client: null,
    fileDrop: null,
  },
  {
    SELECT_CLIENT: (state, action: Action.SelectClient) => ({
      client: action.id === state.client ? null : action.id,
      fileDrop: null,
    }),
  },
);

// ~~~~~~~~~~~~~~~~~~~~~~~~
// Card Attributes Reducers
// ~~~~~~~~~~~~~~~~~~~~~~~~

/** Reducer for Clients in the cardAttributes state object */
const clientCardAttributes = createReducer<Dict<CardAttributes>>({},
  {
    FETCH_CLIENTS_SUCCEEDED: (__, { response }: Action.FetchClientsSucceeded) => ({
      ..._.mapValues(response.clients, (client) => ({ disabled: !client.canManageFileDrops })),
    }),
  },
);

/** Reducer that combines the cardAttributes reducers */
const cardAttributes = combineReducers({
  client: clientCardAttributes,
});

// ~~~~~~~~~~~~~~~
// Filter Reducers
// ~~~~~~~~~~~~~~~

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

/** Reducer that combines the filters reducers */
const filters = combineReducers({
  client: createFilterReducer('SET_FILTER_TEXT_CLIENT'),
  fileDrop: createFilterReducer('SET_FILTER_TEXT_FILE_DROP'),
});

// ~~~~~~~~~~~~~~
// Modal Reducers
// ~~~~~~~~~~~~~~

/**
 * Create a reducer for a modal
 * @param openActions Actions that cause the modal to open
 * @param closeActions Actions that cause the modal to close
 */
const createModalReducer = (
  openActions: Array<Action.OpenModalAction['type']>,
  closeActions: Array<Action.FileDropActions['type']>,
) => {
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

const modals = combineReducers({
  createFileDrop: createModalReducer(['OPEN_CREATE_FILE_DROP_MODAL'], [
    'CLOSE_CREATE_FILE_DROP_MODAL',
    'CREATE_FILE_DROP_SUCCEEDED',
  ]),
});

// ~~~~~~~~~~~~~
// Data Reducers
// ~~~~~~~~~~~~~

/** Reducer for the data state object */
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

// ~~~~~~~~~~~~~~~~
// Combine Reducers
// ~~~~~~~~~~~~~~~~

/** Reducer that builds the final state object */
export const fileDropReducerState = combineReducers({
  data,
  selected,
  cardAttributes,
  pending,
  filters,
  modals,
  toastr: toastrReducer,
});
