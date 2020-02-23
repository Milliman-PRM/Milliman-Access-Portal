import * as _ from 'lodash';
import { reducer as toastrReducer } from 'react-redux-toastr';
import { combineReducers } from 'redux';

import * as Action from './actions';
import * as State from './store';

import { FileDropWithStats, Guid } from '../../models';
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
  deleteFileDrop: false,
  updateFileDrop: false,
};

const _initialCreateFileDropData: State.FileDropFormStateData = {
  clientId: '',
  id: '',
  fileDropName: '',
  fileDropDescription: '',
  errors: {
    fileDropName: null,
    fileDropDescription: null,
  },
};

const _initialFileDropWithStats: FileDropWithStats = {
  clientId: null,
  id: null,
  name: null,
  description: null,
  userCount: null,
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
  FETCH_FILE_DROPS: (state) => ({
    ...state,
    fileDrops: true,
  }),
  FETCH_FILE_DROPS_SUCCEEDED: (state) => ({
    ...state,
    fileDrops: false,
  }),
  FETCH_FILE_DROPS_FAILED: (state) => ({
    ...state,
    fileDrops: false,
  }),
  CREATE_FILE_DROP: (state) => ({
    ...state,
    createFileDrop: true,
  }),
  CREATE_FILE_DROP_SUCCEEDED: (state) => ({
    ...state,
    createFileDrop: false,
  }),
  CREATE_FILE_DROP_FAILED: (state) => ({
    ...state,
    createFileDrop: false,
  }),
  DELETE_FILE_DROP: (state) => ({
    ...state,
    deleteFileDrop: true,
  }),
  DELETE_FILE_DROP_SUCCEEDED: (state) => ({
    ...state,
    deleteFileDrop: false,
  }),
  DELETE_FILE_DROP_FAILED: (state) => ({
    ...state,
    deleteFileDrop: false,
  }),
});

/** Reducer for the statusTries value in the pending state object */
const pendingStatusTries = createReducer<number>(5, {
  DECREMENT_STATUS_REFRESH_ATTEMPTS: (state) => state ? state - 1 : 0,
  FETCH_STATUS_REFRESH_SUCCEEDED: () => 5,
});

/** Reducer for the Create File Drop modal form */
const pendingCreateFileDropForm = createReducer<State.FileDropFormStateData>(_initialCreateFileDropData, {
  OPEN_CREATE_FILE_DROP_MODAL: (_state, action: Action.OpenCreateFileDropModal) => ({
    ..._initialCreateFileDropData,
    clientId: action.clientId,
  }),
  CLOSE_CREATE_FILE_DROP_MODAL: () => ({
    ..._initialCreateFileDropData,
  }),
  CREATE_FILE_DROP_SUCCEEDED: () => ({
    ..._initialCreateFileDropData,
  }),
  UPDATE_FILE_DROP_FORM_DATA: (state, action: Action.UpdateFileDropFormData) => {
    if (action.updateType === 'create') {
      return {
        ...state,
        [action.field]: action.value,
      };
    } else {
      return {
        ...state,
      };
    }
  },
});

/** Reducer for editing the File Drop information */
const pendingEditFileDropData = createReducer<State.FileDropFormStateData>(_initialCreateFileDropData, {
  EDIT_FILE_DROP: (_state, action: Action.EditFileDrop) => ({
    ..._initialCreateFileDropData,
    clientId: action.fileDrop.clientId,
    id: action.fileDrop.id,
    fileDropName: action.fileDrop.name,
    fileDropDescription: action.fileDrop.description,
  }),
  CANCEL_FILE_DROP_EDIT: () => ({
    ..._initialCreateFileDropData,
  }),
  UPDATE_FILE_DROP_SUCCEEDED: () => ({
    ..._initialCreateFileDropData,
  }),
  UPDATE_FILE_DROP_FORM_DATA: (state, action: Action.UpdateFileDropFormData) => {
    if (action.updateType === 'edit') {
      return {
        ...state,
        [action.field]: action.value,
      };
    } else {
      return {
        ...state,
      };
    }
  },
});

/** Reducer for the Delete File Drop modal */
const pendingFileDropToDelete = createReducer<FileDropWithStats>(_initialFileDropWithStats, {
  OPEN_DELETE_FILE_DROP_MODAL: (_state, action: Action.OpenDeleteFileDropModal) => ({
    ...action.fileDrop,
  }),
  CLOSE_DELETE_FILE_DROP_MODAL: () => ({
    ..._initialFileDropWithStats,
  }),
  CLOSE_DELETE_FILE_DROP_CONFIRMATION_MODAL: () => ({
    ..._initialFileDropWithStats,
  }),
  DELETE_FILE_DROP: () => ({
    ..._initialFileDropWithStats,
  }),
});

/** Reducer that combines the pending reducers */
const pending = combineReducers({
  async: pendingData,
  statusTries: pendingStatusTries,
  createFileDrop: pendingCreateFileDropForm,
  editFileDrop: pendingEditFileDropData,
  fileDropToDelete: pendingFileDropToDelete,
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
    CREATE_FILE_DROP_SUCCEEDED: (state, action: Action.CreateFileDropSucceeded) => ({
      ...state,
      fileDrop: (action.response.currentFileDropId) ? action.response.currentFileDropId : null,
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

/** Reducer for File Drops in the cardAttributes state object */
const fileDropCardAttributes = createReducer<Dict<CardAttributes>>({},
  {
    FETCH_FILE_DROPS_SUCCEEDED: (_state, action: Action.FetchFileDropsSucceeded) => ({
      ..._.mapValues(action.response.fileDrops, () => ({ editing: false })),
    }),
    EDIT_FILE_DROP: (state, action: Action.EditFileDrop) => ({
      ..._.mapValues(state, () => ({ editing: false })),
      [action.fileDrop.id]: {
        editing: true,
      },
    }),
    CANCEL_FILE_DROP_EDIT: (state) => ({
      ..._.mapValues(state, () => ({ editing: false })),
    }),
    UPDATE_FILE_DROP_SUCCEEDED: (state) => ({
      ..._.mapValues(state, () => ({ editing: false })),
    }),
  },
);

/** Reducer that combines the cardAttributes reducers */
const cardAttributes = combineReducers({
  clients: clientCardAttributes,
  fileDrops: fileDropCardAttributes,
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
    'CREATE_FILE_DROP_FAILED',
  ]),
  deleteFileDrop: createModalReducer(['OPEN_DELETE_FILE_DROP_MODAL'], [
    'CLOSE_DELETE_FILE_DROP_MODAL',
    'OPEN_DELETE_FILE_DROP_CONFIRMATION_MODAL',
  ]),
  confirmDeleteFileDrop: createModalReducer(['OPEN_DELETE_FILE_DROP_CONFIRMATION_MODAL'], [
    'CLOSE_DELETE_FILE_DROP_CONFIRMATION_MODAL',
    'DELETE_FILE_DROP_SUCCEEDED',
    'DELETE_FILE_DROP_FAILED',
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
  FETCH_FILE_DROPS_SUCCEEDED: (state, action: Action.FetchFileDropsSucceeded) => ({
    ...state,
    clients: {
      ...state.clients,
      [action.response.clientCard.id]: {
        ...action.response.clientCard,
      },
    },
    fileDrops: {
      ...action.response.fileDrops,
    },
  }),
  CREATE_FILE_DROP_SUCCEEDED: (state, action: Action.CreateFileDropSucceeded) => ({
    ...state,
    clients: {
      ...state.clients,
      [action.response.clientCard.id]: {
        ...action.response.clientCard,
      },
    },
    fileDrops: {
      ...action.response.fileDrops,
    },
  }),
  DELETE_FILE_DROP_SUCCEEDED: (state, action: Action.DeleteFileDropSucceeded) => ({
    ...state,
    clients: {
      ...state.clients,
      [action.response.clientCard.id]: {
        ...action.response.clientCard,
      },
    },
    fileDrops: {
      ...action.response.fileDrops,
    },
  }),
  UPDATE_FILE_DROP_SUCCEEDED: (state, action: Action.UpdateFileDropSucceeded) => ({
    ...state,
    clients: {
      ...state.clients,
      [action.response.clientCard.id]: {
        ...action.response.clientCard,
      },
    },
    fileDrops: {
      ...action.response.fileDrops,
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
