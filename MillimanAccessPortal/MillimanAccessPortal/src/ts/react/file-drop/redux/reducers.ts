import * as _ from 'lodash';
import { reducer as toastrReducer } from 'react-redux-toastr';
import { combineReducers } from 'redux';

import * as Action from './actions';
import * as State from './store';

import { FileDropWithStats, Guid, PermissionGroupsReturnModel } from '../../models';
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

// TODO: Remove this once the calls are hooked up
const _dummyPermissionGroupsData: PermissionGroupsReturnModel = {
  eligibleUsers: {
    'user-1': {
      id: 'user-1',
      firstName: 'User',
      lastName: 'One',
      username: 'user.1@domain.com',
      isFileDropAdmin: true,
    },
    'user-2': {
      id: 'user-2',
      firstName: 'User',
      lastName: 'Two',
      username: 'user.2@domain.com',
      isFileDropAdmin: false,
    },
    'user-3': {
      id: 'user-3',
      firstName: 'User',
      lastName: 'Three',
      username: 'user.3@domain.com',
      isFileDropAdmin: false,
    },
    'user-4': {
      id: 'user-4',
      firstName: 'User',
      lastName: 'Four',
      username: 'user.4@domain.com',
      isFileDropAdmin: false,
    },
    'user-5': {
      id: 'user-5',
      firstName: 'User',
      lastName: 'Five',
      username: 'user.5@domain.com',
      isFileDropAdmin: false,
    },
    'user-6': {
      id: 'user-6',
      firstName: 'User',
      lastName: 'Six',
      username: 'user.6@domain.com',
      isFileDropAdmin: false,
    },
    'user-7': {
      id: 'user-7',
      firstName: 'User',
      lastName: 'Seven',
      username: 'user.7@domain.com',
      isFileDropAdmin: false,
    },
    'user-8': {
      id: 'user-8',
      firstName: 'User',
      lastName: 'Eight',
      username: 'user.8@domain.com',
      isFileDropAdmin: false,
    },
    'user-9': {
      id: 'user-9',
      firstName: 'User',
      lastName: 'Nine',
      username: 'user.9@domain.com',
      isFileDropAdmin: false,
    },
    'user-10': {
      id: 'user-10',
      firstName: 'User',
      lastName: 'Ten',
      username: 'user.10@domain.com',
      isFileDropAdmin: false,
    },
  },
  fileDropId: '',
  permissionGroups: {
    'pg-1': {
      id: 'pg-1',
      name: 'User One',
      isPersonalGroup: true,
      authorizedMapUsers: ['user-1'],
      authorizedSftpAccounts: [],
      deleteAccess: true,
      readAccess: true,
      writeAccess: true,
    },
    'pg-4': {
      id: 'pg-4',
      name: 'No Users Group',
      isPersonalGroup: false,
      authorizedMapUsers: [],
      authorizedSftpAccounts: [],
      deleteAccess: true,
      readAccess: true,
      writeAccess: true,
    },
    'pg-2': {
      id: 'pg-2',
      name: 'Permission Group #2',
      isPersonalGroup: false,
      authorizedMapUsers: ['user-2', 'user-3'],
      authorizedSftpAccounts: [],
      deleteAccess: false,
      readAccess: true,
      writeAccess: false,
    },
    'pg-3': {
      id: 'pg-3',
      name: 'Permission Group #3',
      isPersonalGroup: false,
      authorizedMapUsers: ['user-4', 'user-5'],
      authorizedSftpAccounts: [],
      deleteAccess: false,
      readAccess: true,
      writeAccess: true,
    },
  },
};

const _initialData: State.FileDropDataState = {
  clients: {},
  fileDrops: {},
  permissionGroups: null,
};

const _initialPendingData: State.FileDropPendingReturnState = {
  globalData: false,
  clients: false,
  fileDrops: false,
  createFileDrop: false,
  deleteFileDrop: false,
  updateFileDrop: false,
  permissions: false,
  permissionsUpdate: false,
};

const _initialPermissionGroupsTab: PermissionGroupsReturnModel = {
  fileDropId: '',
  eligibleUsers: {},
  permissionGroups: {},
};

const _initialFilterValues: State.FileDropFilterState = {
  client: { text: '' },
  fileDrop: { text: '' },
  permissions: { text: '' },
  activityLog: { text: '' },
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
  FETCH_PERMISSION_GROUPS: (state) => ({
    ...state,
    permissions: true,
  }),
  FETCH_PERMISSION_GROUPS_SUCCEEDED: (state) => ({
    ...state,
    permissions: false,
  }),
  FETCH_PERMISSION_GROUPS_FAILED: (state) => ({
    ...state,
    permissions: false,
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
  DELETE_FILE_DROP_SUCCEEDED: () => ({
    ..._initialFileDropWithStats,
  }),
});

/** Reducer for swiching the active File Drop tab */
const selectedFileDropTab = createReducer<State.AvailableFileDropTabs>(null, {
  SELECT_FILE_DROP_TAB: (_state, action: Action.SelectFileDropTab) => action.tab,
});

/** Reducer for Permission Groups form data */
const permissionGroupsTab = createReducer<PermissionGroupsReturnModel>(_initialPermissionGroupsTab, {
  // TODO: Change this reducer to the FetchPermissionGroupsSucceeded action
  FETCH_PERMISSION_GROUPS: () => JSON.parse(JSON.stringify(_dummyPermissionGroupsData)),
  SET_PERMISSION_GROUP_PERMISSION_VALUE: (state, action: Action.SetPermissionGroupPermissionValue) => ({
    ...state,
    permissionGroups: {
      ...state.permissionGroups,
      [action.pgId]: {
        ...state.permissionGroups[action.pgId],
        [action.permission]: action.value,
      },
    },
  }),
  REMOVE_PERMISSION_GROUP: (state, action: Action.RemovePermissionGroup) => {
    const { permissionGroups } = state;
    delete permissionGroups[action.pgId];
    return {
      ...state,
      permissionGroups,
    };
  },
  DISCARD_PENDING_PERMISSION_GROUP_CHANGES: (_state, action: Action.DiscardPendingPermissionGroupChanges) => ({
    // Convert to string and then back to json to allow these two sections of state to be independent
    ...JSON.parse(JSON.stringify(action.originalValues)),
  }),
});

/** Reducer for setting the edit mode state of the Permission Groups tab */
const permissionGroupsEditMode = createReducer<boolean>(false, {
  SET_EDIT_MODE_FOR_PERMISSION_GROUPS:
    (_state, action: Action.SetEditModeForPermissionGroups) => action.editModeEnabled,
});

/** Reducer that combines the pending reducers */
const pending = combineReducers({
  async: pendingData,
  statusTries: pendingStatusTries,
  createFileDrop: pendingCreateFileDropForm,
  editFileDrop: pendingEditFileDropData,
  fileDropToDelete: pendingFileDropToDelete,
  selectedFileDropTab,
  permissionGroupsTab,
  permissionGroupsEditMode,
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
    SELECT_FILE_DROP: (state, action: Action.SelectFileDrop) => ({
      ...state,
      fileDrop: action.id === state.fileDrop ? null : action.id,
    }),
    CREATE_FILE_DROP_SUCCEEDED: (state, action: Action.CreateFileDropSucceeded) => ({
      ...state,
      fileDrop: (action.response.currentFileDropId) ? action.response.currentFileDropId : null,
    }),
    CLOSE_CREATE_FILE_DROP_MODAL: (state) => ({
      ...state,
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

/** Create a reducer for the filters */
const filters = createReducer<State.FileDropFilterState>(_initialFilterValues,
  {
    SET_FILTER_TEXT: (state, action: Action.SetFilterText) => ({
      ...state,
      [action.filter]: {
        text: action.text,
      },
    }),
    SELECT_CLIENT: () => ({
      ..._initialFilterValues,
    }),
    SELECT_FILE_DROP: (state) => ({
      ...state,
      permissions: _initialFilterValues.permissions,
      activityLog: _initialFilterValues.activityLog,
    }),
  },
);

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
    permissionGroups: null,
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
    permissionGroups: null,
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
    permissionGroups: null,
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
    permissionGroups: null,
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
  // FETCH_PERMISSION_GROUPS_SUCCEEDED: (state, action: Action.FetchPermissionGroupsSucceeded) => ({
  //   ...state,
  //   permissionGroups: {
  //     ...action.response,
  //   },
  // }),
  FETCH_PERMISSION_GROUPS: (state) => ({
    // TODO: Remove this reducer (It's hard-coded fake data)
    ...state,
    permissionGroups: JSON.parse(JSON.stringify(_dummyPermissionGroupsData)),
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
