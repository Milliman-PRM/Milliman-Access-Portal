import * as _ from 'lodash';
import { reducer as toastrReducer } from 'react-redux-toastr';
import { combineReducers } from 'redux';

import * as Action from './actions';
import * as State from './store';

import { generateUniqueId } from '../../../generate-unique-identifier';
import { ProgressSummary } from '../../../upload/progress-monitor';
import * as UploadActions from '../../../upload/Redux/actions';
import {
  FileDropDirectoryContentModel, FileDropSettings, FileDropWithStats, PermissionGroupsReturnModel,
} from '../../models';
import { CardAttributes } from '../../shared-components/card/card';
import { createReducerCreator, Handlers } from '../../shared-components/redux/reducers';
import { Dict, ModalState } from '../../shared-components/redux/store';

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

/** Reusable logic for changing cardAttributes on a subsequent directory content fetch or change. */
function setFileDropDirectoryContentModel(response: FileDropDirectoryContentModel) {
  const returnObject: Dict<State.FileAndFolderAttributes> = {};
  _.forEach(response.directories, (folder) => {
    returnObject[folder.id] = {
      editing: false,
      expanded: false,
      fileName: '',
      description: folder.description,
      fileNameRaw: '',
      descriptionRaw: folder.description,
      saving: false,
    };
  });
  _.forEach(response.files, (file) => {
    returnObject[file.id] = {
      editing: false,
      expanded: false,
      fileName: file.fileName,
      description: file.description,
      fileNameRaw: file.fileName,
      descriptionRaw: file.description,
      saving: false,
    };
  });
  return returnObject;
}

function unsetSaveStatusForFileDropDirectoryContentModel(currentState: Dict<State.FileAndFolderAttributes>) {
  const returnObject: Dict<State.FileAndFolderAttributes> = {};
  _.forEach(currentState, (item, key) => {
    returnObject[key] = {
      editing: false,
      expanded: false,
      fileName: '',
      description: item.description,
      fileNameRaw: '',
      descriptionRaw: item.description,
      saving: false,
    };
  });
  return returnObject;
}

// ~~~~~~~~~~~~~~~
// Default Objects
// ~~~~~~~~~~~~~~~

const _initialPendingData: State.FileDropPendingReturnState = {
  clients: false,
  fileDrops: false,
  createFileDrop: false,
  deleteFileDrop: false,
  updateFileDrop: false,
  permissions: false,
  permissionsUpdate: false,
  activityLog: false,
  settings: false,
  move: false,
  createFolderMoveMode: false,
  deleteItem: false,
  createFileDropFolder: false,
};

const _initialPermissionGroupsTab: PermissionGroupsReturnModel = {
  fileDropId: '',
  eligibleUsers: {},
  permissionGroups: {},
  clientModel: null,
};

const _initialFilterValues: State.FileDropFilterState = {
  client: { text: '' },
  fileDrop: { text: '' },
  permissions: { text: '' },
  activityLog: { text: '' },
  fileDropContents: { text: '' },
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
  isSuspended: false,
  userCount: null,
  currentUserPermissions: null,
};

const _initialMoveItem: State.MoveItemData = {
  fileDropName: null,
  itemType: null,
  itemId: null,
  itemName: null,
  initialCanonicalPath: null,
  currentCanonicalPath: null,
  breadcrumbs: null,
  newFolderId: null,
  createNewFolderMode: false,
  newFolderName: null,
};

const _initialAfterFormModal: State.AfterFormModal = {
  entityType: null,
  entityToSelect: null,
};

const _initialFileDropSettings: FileDropSettings = {
  fingerprint: '',
  isPasswordExpired: true,
  isSuspended: true,
  assignedPermissionGroupId: null,
  notifications: [],
  sftpHost: '',
  sftpPort: '',
  sftpUserName: '',
  userHasPassword: false,
};

const _initialData: State.FileDropDataState = {
  clients: {},
  fileDrops: {},
  fileDropContents: null,
  fileDropContentsForMove: null,
  permissionGroups: null,
  activityLogEvents: [],
  fileDropSettings: _initialFileDropSettings,
};

const _initialUpload: State.FileDropUploadState = {
  uploadId: null,
  clientId: null,
  fileDropId: null,
  folderId: null,
  canonicalPath: null,
  fileName: null,
  cancelable: false,
  canceled: false,
  checksumProgress: ProgressSummary.empty(),
  uploadProgress: ProgressSummary.empty(),
  errorMsg: null,
};

const _initialItemToDelete: State.DeleteItemData = {
  itemType: null,
  itemName: null,
  itemId: null,
};

// ~~~~~~~~~~~~~~~~
// Pending Reducers
// ~~~~~~~~~~~~~~~~

/** Reducer for the async state object in the pending state object */
const pendingData = createReducer<State.FileDropPendingReturnState>(_initialPendingData, {
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
  UPDATE_PERMISSION_GROUPS: (state) => ({
    ...state,
    permissionsUpdate: true,
  }),
  UPDATE_PERMISSION_GROUPS_SUCCEEDED: (state) => ({
    ...state,
    permissionsUpdate: false,
  }),
  UPDATE_PERMISSION_GROUPS_FAILED: (state) => ({
    ...state,
    permissionsUpdate: false,
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
  FETCH_ACTIVITY_LOG: (state) => ({
    ...state,
    activityLog: true,
  }),
  FETCH_ACTIVITY_LOG_SUCCEEDED: (state) => ({
    ...state,
    activityLog: false,
  }),
  FETCH_ACTIVITY_LOG_FAILED: (state) => ({
    ...state,
    activityLog: false,
  }),
  FETCH_SETTINGS: (state) => ({
    ...state,
    settings: true,
  }),
  FETCH_SETTINGS_SUCCEEDED: (state) => ({
    ...state,
    settings: false,
  }),
  FETCH_SETTINGS_FAILED: (state) => ({
    ...state,
    settings: false,
  }),
  RENAME_FILE_DROP_FILE: (state) => ({
    ...state,
    move: true,
  }),
  RENAME_FILE_DROP_FILE_SUCCEEDED: (state) => ({
    ...state,
    move: false,
  }),
  RENAME_FILE_DROP_FILE_FAILED: (state) => ({
    ...state,
    move: false,
  }),
  RENAME_FILE_DROP_FOLDER: (state) => ({
    ...state,
    move: true,
  }),
  RENAME_FILE_DROP_FOLDER_SUCCEEDED: (state) => ({
    ...state,
    move: false,
  }),
  RENAME_FILE_DROP_FOLDER_FAILED: (state) => ({
    ...state,
    move: false,
  }),
  CREATE_FILE_DROP_FOLDER_FOR_MOVE: (state) => ({
    ...state,
    createFolderMoveMode: true,
  }),
  CREATE_FILE_DROP_FOLDER_FOR_MOVE_SUCCEEDED: (state) => ({
    ...state,
    createFolderMoveMode: false,
  }),
  CREATE_FILE_DROP_FOLDER_FOR_MOVE_FAILED: (state) => ({
    ...state,
    createFolderMoveMode: false,
  }),
  DELETE_FILE_DROP_FILE: (state) => ({
    ...state,
    deleteItem: true,
  }),
  DELETE_FILE_DROP_FILE_SUCCEEDED: (state) => ({
    ...state,
    deleteItem: false,
  }),
  DELETE_FILE_DROP_FILE_FAILED: (state) => ({
    ...state,
    deleteItem: false,
  }),
  DELETE_FILE_DROP_FOLDER: (state) => ({
    ...state,
    deleteItem: true,
  }),
  DELETE_FILE_DROP_FOLDER_SUCCEEDED: (state) => ({
    ...state,
    deleteItem: false,
  }),
  DELETE_FILE_DROP_FOLDER_FAILED: (state) => ({
    ...state,
    deleteItem: false,
  }),
  CREATE_FILE_DROP_FOLDER: (state) => ({
    ...state,
    createFileDropFolder: true,
  }),
  CREATE_FILE_DROP_FOLDER_SUCCEEDED: (state) => ({
    ...state,
    createFileDropFolder: false,
  }),
  CREATE_FILE_DROP_FOLDER_FAILED: (state) => ({
    ...state,
    createFileDropFolder: false,
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

/** Reducer for the Move File Drop Item modal */
const pendingMoveFileDropItem = createReducer<State.MoveItemData>(_initialMoveItem, {
  OPEN_MOVE_FILE_DROP_ITEM_MODAL: (state, action: Action.OpenMoveFileDropItemModal) => ({
    ...state,
    itemType: action.itemType,
    fileDropName: action.fileDropName,
    itemId: action.itemId,
    itemName: action.itemName,
    initialCanonicalPath: action.initialCanonicalPath,
    createNewFolderMode: false,
    newFolderName: null,
  }),
  FETCH_FOLDER_CONTENTS_FOR_MOVE: (state, action: Action.FetchFolderContentsForMove) => ({
    ...state,
    currentCanonicalPath: decodeURIComponent(action.request.canonicalPath),
    breadcrumbs: decodeURIComponent(action.request.canonicalPath).split('/').slice(1),
    createNewFolderMode: false,
  }),
  FETCH_FOLDER_CONTENTS_FOR_MOVE_SUCCEEDED: (state, action: Action.FetchFolderContentsForMoveSucceeded) => ({
    ...state,
    newFolderId: action.response.thisDirectory.id,
  }),
  SET_NEW_FOLDER_MODE_STATUS: (state, action: Action.SetNewFolderModeStatus) => ({
    ...state,
    createNewFolderMode: action.value,
    newFolderName: '',
  }),
  SET_NEW_FOLDER_NAME_FOR_MOVE: (state, action: Action.SetNewFolderNameForMove) => ({
    ...state,
    newFolderName: action.newFolderName,
  }),
  CREATE_FILE_DROP_FOLDER_FOR_MOVE_SUCCEEDED: (state) => ({
    ...state,
    createNewFolderMode: false,
  }),
  CREATE_FILE_DROP_FOLDER_FOR_MOVE_FAILED: (state) => ({
    ...state,
    createNewFolderMode: false,
  }),
});

/** Reducer for swiching the active File Drop tab */
const selectedFileDropTab = createReducer<State.AvailableFileDropTabs>(null, {
  SELECT_FILE_DROP_TAB: (_state, action: Action.SelectFileDropTab) => action.tab,
  CREATE_FILE_DROP_SUCCEEDED: () => 'permissions',
});

/** Reducer for Permission Groups form data */
const permissionGroupsTab = createReducer<PermissionGroupsReturnModel>(_initialPermissionGroupsTab, {
  CREATE_FILE_DROP_SUCCEEDED: (_state, action: Action.CreateFileDropSucceeded) =>
    _.cloneDeep(action.response.permissionGroups),
  FETCH_PERMISSION_GROUPS_SUCCEEDED: (_state, action: Action.FetchPermissionGroupsSucceeded) =>
    _.cloneDeep(action.response),
  UPDATE_PERMISSION_GROUPS_SUCCEEDED: (_state, action: Action.FetchPermissionGroupsSucceeded) =>
    _.cloneDeep(action.response),
  OPEN_CREATE_FILE_DROP_MODAL: () => _.cloneDeep(_initialPermissionGroupsTab),
  FETCH_PERMISSION_GROUPS: () => _.cloneDeep(_initialPermissionGroupsTab),
  DELETE_FILE_DROP_SUCCEEDED: (state, action: Action.DeleteFileDropSucceeded) => {
    if (state.fileDropId === action.response.currentFileDropId) {
      return _.cloneDeep(_initialPermissionGroupsTab);
    } else {
      return state;
    }
  },
  SET_PERMISSION_GROUP_PERMISSION_VALUE: (state, action: Action.SetPermissionGroupPermissionValue) => ({
    ...state,
    permissionGroups: {
      ...state.permissionGroups,
      [action.pgId]: {
        ...state.permissionGroups[action.pgId],
        permissions: {
          ...state.permissionGroups[action.pgId].permissions,
          [action.permission]: action.value,
        },
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
  DISCARD_PENDING_PERMISSION_GROUP_CHANGES: (_state, action: Action.DiscardPendingPermissionGroupChanges) =>
    action.originalValues ? _.cloneDeep(action.originalValues) : _initialPermissionGroupsTab,
  ADD_USER_TO_PERMISSION_GROUP: (state, action: Action.AddUserToPermissionGroup) => {
    const { assignedMapUserIds } = state.permissionGroups[action.pgId];
    if (assignedMapUserIds.indexOf(action.userId) === -1) {
      assignedMapUserIds.push(action.userId);
    }
    const pgName = (state.permissionGroups[action.pgId].isPersonalGroup && action.userName)
      ? action.userName
      : state.permissionGroups[action.pgId].name;
    return {
      ...state,
      permissionGroups: {
        ...state.permissionGroups,
        [action.pgId]: {
          ...state.permissionGroups[action.pgId],
          name: pgName,
          assignedMapUserIds,
        },
      },
    };
  },
  REMOVE_USER_FROM_PERMISSION_GROUP: (state, action: Action.RemoveUserFromPermissionGroup) => {
    const { assignedMapUserIds: existingAssignedMapUsers } = state.permissionGroups[action.pgId];
    const assignedMapUserIds = _.filter(existingAssignedMapUsers, (userId) => userId !== action.userId);
    return {
      ...state,
      permissionGroups: {
        ...state.permissionGroups,
        [action.pgId]: {
          ...state.permissionGroups[action.pgId],
          assignedMapUserIds,
        },
      },
    };
  },
  SET_PERMISSION_GROUP_NAME_TEXT: (state, action: Action.SetPermissionGroupNameText) => ({
    ...state,
    permissionGroups: {
      ...state.permissionGroups,
      [action.pgId]: {
        ...state.permissionGroups[action.pgId],
        name: action.value,
      },
    },
  }),
  ADD_NEW_PERMISSION_GROUP: (state, action: Action.AddNewPermissionGroup) => ({
    ...state,
    permissionGroups: {
      ...state.permissionGroups,
      [action.tempPGId]: {
        id: action.tempPGId,
        name: '',
        isPersonalGroup: action.isSingleGroup,
        assignedMapUserIds: [],
        assignedSftpAccountIds: [],
        permissions: {
          readAccess: false,
          writeAccess: false,
          deleteAccess: false,
        },
      },
    },
  }),
});

/** Reducer for setting the edit mode state of the Permission Groups tab */
const permissionGroupsEditMode = createReducer<boolean>(false, {
  SET_EDIT_MODE_FOR_PERMISSION_GROUPS:
    (_state, action: Action.SetEditModeForPermissionGroups) => action.editModeEnabled,
  CREATE_FILE_DROP_SUCCEEDED: () => true,
  UPDATE_PERMISSION_GROUPS_SUCCEEDED: () => false,
  SELECT_CLIENT: () => false,
  SELECT_FILE_DROP: () => false,
  SELECT_FILE_DROP_TAB: () => false,
});

const afterFormModal = createReducer<State.AfterFormModal>(_initialAfterFormModal, {
  OPEN_MODIFIED_FORM_MODAL: (_state, action: Action.OpenModifiedFormModal) => ({
    entityToSelect: action.afterFormModal.entityToSelect,
    entityType: action.afterFormModal.entityType,
  }),
  CLOSE_MODIFIED_FORM_MODAL: () => _initialAfterFormModal,
});

const pendingUploads = createReducer<Dict<State.FileDropUploadState>>({}, {
  INITIALIZE_FIRST_UPLOAD_OBJECT: (state) => {
    if (Object.keys(state).length === 0) {
      const uniqueId = generateUniqueId('FileDropUpload');
      return {
        [uniqueId]: {
          ..._initialUpload,
          uploadId: uniqueId,
        },
      };
    } else {
      return {};
    }
  },
  BEGIN_FILE_DROP_FILE_UPLOAD: (state, action: Action.BeginFileDropFileUpload) => {
    const uniqueId = generateUniqueId('FileDropUpload');
    return {
      ...state,
      [action.uploadId]: {
        ...state[action.uploadId],
        clientId: action.clientId,
        fileDropId: action.fileDropId,
        folderId: action.folderId,
        canonicalPath: action.canonicalPath,
        fileName: action.fileName,
        cancelable: true,
      },
      [uniqueId]: {
        ..._initialUpload,
        uploadId: uniqueId,
      },
    };
  },
  UPDATE_CHECKSUM_PROGRESS: (state, action: UploadActions.UpdateChecksumProgress) => {
    if (action.uploadId in state) {
      return {
        ...state,
        [action.uploadId]: {
          ...state[action.uploadId],
          checksumProgress: action.progress,
        },
      };
    } else {
      return state;
    }
  },
  UPDATE_UPLOAD_PROGRESS: (state, action: UploadActions.UpdateUploadProgress) => {
    if (action.uploadId in state) {
      return {
        ...state,
        [action.uploadId]: {
          ...state[action.uploadId],
          uploadProgress: action.progress,
        },
      };
    } else {
      return state;
    }
  },
  SET_UPLOAD_ERROR: (state, action: UploadActions.SetUploadError) => ({
    ...state,
    [action.uploadId]: {
      ...state[action.uploadId],
      errorMsg: action.errorMsg,
    },
  }),
  SET_UPLOAD_CANCELABLE: (state, action: UploadActions.SetUploadCancelable) => ({
    ...state,
    [action.uploadId]: {
      ...state[action.uploadId],
      cancelable: action.cancelable,
    },
  }),
  BEGIN_FILE_DROP_UPLOAD_CANCEL: (state, action: Action.BeginFileDropUploadCancel) => ({
    ...state,
    [action.uploadId]: {
      ...state[action.uploadId],
      canceled: true,
    },
  }),
  CANCEL_FILE_UPLOAD: (state, action: UploadActions.CancelFileUpload) => {
    const uploads = { ...state };
    delete uploads[action.uploadId];
    if (Object.keys(uploads).length === 0) {
      const uniqueId = generateUniqueId('FileDropUpload');
      return {
        [uniqueId]: {
          ..._initialUpload,
          uploadId: uniqueId,
        },
      };
    } else {
      return {
        ...uploads,
      };
    }
  },
  FINALIZE_FILE_DROP_UPLOAD: (state, action: Action.FinalizeFileDropUpload) => {
    const uploads = { ...state };
    delete uploads[action.uploadId];
    if (Object.keys(uploads).length === 0) {
      const uniqueId = generateUniqueId('FileDropUpload');
      return {
        [uniqueId]: {
          ..._initialUpload,
          uploadId: uniqueId,
        },
      };
    } else {
      return {
        ...uploads,
      };
    }
  },
});

/** Reducer for the Create Folder mode */
const createFolder = createReducer<State.CreateFolderData>(null, {
  ENTER_CREATE_FOLDER_MODE: () => ({
    name: '',
    description: '',
  }),
  EXIT_CREATE_FOLDER_MODE: () => null,
  UPDATE_CREATE_FOLDER_VALUES: (state, action: Action.UpdateCreateFolderValues) => ({
    ...state,
    [action.field]: action.value,
  }),
  CREATE_FILE_DROP_FOLDER_SUCCEEDED: () => null,
});

const deleteItem = createReducer<State.DeleteItemData>(_initialItemToDelete, {
  OPEN_DELETE_FILE_DROP_ITEM_MODAL: (_state, action: Action.OpenDeleteFileDropItemModal) => ({
    itemId: action.itemId,
    itemName: action.itemName,
    itemType: action.itemType,
  }),
  CLOSE_DELETE_FILE_DROP_ITEM_MODAL: () => _initialItemToDelete,
  DELETE_FILE_DROP_FILE_SUCCEEDED: () => _initialItemToDelete,
  DELETE_FILE_DROP_FILE_FAILED: () => _initialItemToDelete,
  DELETE_FILE_DROP_FOLDER_SUCCEEDED: () => _initialItemToDelete,
  DELETE_FILE_DROP_FOLDER_FAILED: () => _initialItemToDelete,
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
  afterFormModal,
  uploads: pendingUploads,
  createFolder,
  moveItem: pendingMoveFileDropItem,
  itemToDelete: deleteItem,
});

// ~~~~~~~~~~~~~~~~
// Selected Reducers
// ~~~~~~~~~~~~~~~~

/** Reducer for the selected state object */
const selected = createReducer<State.FileDropSelectedState>(
  {
    client: null,
    fileDrop: null,
    fileDropFolder: {
      folderId: null,
      canonicalPath: null,
    },
  },
  {
    SELECT_CLIENT: (state, action: Action.SelectClient) => ({
      client: action.id === state.client ? null : action.id,
      fileDrop: null,
      fileDropFolder: {
        folderId: null,
        canonicalPath: null,
      },
    }),
    SELECT_FILE_DROP: (state, action: Action.SelectFileDrop) => ({
      ...state,
      fileDrop: action.id === state.fileDrop ? null : action.id,
      fileDropFolder: {
        folderId: null,
        canonicalPath: null,
      },
    }),
    CREATE_FILE_DROP_SUCCEEDED: (state, action: Action.CreateFileDropSucceeded) => ({
      ...state,
      fileDrop: (action.response.currentFileDropId) ? action.response.currentFileDropId : null,
      fileDropFolder: {
        folderId: null,
        canonicalPath: null,
      },
    }),
    CLOSE_CREATE_FILE_DROP_MODAL: (state) => ({
      ...state,
      fileDrop: null,
      fileDropFolder: {
        folderId: null,
        canonicalPath: null,
      },
    }),
    DELETE_FILE_DROP_SUCCEEDED: (state, action: Action.DeleteFileDropSucceeded) => ({
      ...state,
      fileDrop: (state.fileDrop === action.response.currentFileDropId) ? null : state.fileDrop,
      fileDropFolder: (state.fileDrop === action.response.currentFileDropId) ? {
        folderId: null,
        canonicalPath: null,
      } : state.fileDropFolder,
    }),
    OPEN_CREATE_FILE_DROP_MODAL: (state) => ({
      ...state,
      fileDrop: null,
      fileDropFolder: {
        folderId: null,
        canonicalPath: null,
      },
    }),
    FETCH_FOLDER_CONTENTS_SUCCEEDED: (state, action: Action.FetchFolderContentsSucceeded) => ({
      ...state,
      fileDropFolder: {
        folderId: action.response.thisDirectory.id,
        canonicalPath: action.response.thisDirectory.canonicalPath,
      },
    }),
    FETCH_FOLDER_CONTENTS_FOR_MOVE_SUCCEEDED: (state, action: Action.FetchFolderContentsForMoveSucceeded) => ({
      ...state,
      fileDropFolder: {
        folderId: action.response.thisDirectory.id,
        canonicalPath: action.response.thisDirectory.canonicalPath,
      },
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
    TOGGLE_FILE_DROP_CARD_EXPANSION: (state, action: Action.ToggleFileDropCardExpansion) => ({
      ...state,
      [action.fileDropId]: {
        expanded: state[action.fileDropId].expanded ? false : true,
      },
    }),
  },
);

/** Reducer for File Drop contents in the cardAttributes state object */
const fileDropContentAttributes = createReducer<Dict<State.FileAndFolderAttributes>>({},
  {
    FETCH_FOLDER_CONTENTS_SUCCEEDED: (__, { response }: Action.FetchFolderContentsSucceeded) =>
      setFileDropDirectoryContentModel(response),
    CREATE_FILE_DROP_FOLDER_SUCCEEDED: (__, { response }: Action.CreateFileDropFolderSucceeded) =>
      setFileDropDirectoryContentModel(response),
    SET_FILE_OR_FOLDER_EXPANSION: (state, action: Action.SetFileOrFolderExpansion) => ({
      ...state,
      [action.id]: {
        ...state[action.id],
        expanded: action.expanded,
      },
    }),
    RENAME_FILE_DROP_FILE: (state, action: Action.RenameFileDropFile) => {
      const { fileId } = action.request;
      return {
        ...state,
        [fileId]: {
          ...state[fileId],
          expanded: state[fileId].description !== state[fileId].descriptionRaw ? true : false,
          saving: true,
        },
      };
    },
    RENAME_FILE_DROP_FILE_FAILED: (state) => unsetSaveStatusForFileDropDirectoryContentModel(state),
    UPDATE_FILE_DROP_FILE: (state, action: Action.UpdateFileDropFile) => {
      const { fileId } = action.request;
      return {
        ...state,
        [fileId]: {
          ...state[fileId],
          expanded: state[fileId].description !== state[fileId].descriptionRaw ? true : false,
          saving: true,
        },
      };
    },
    SET_FILE_OR_FOLDER_EDITING: (state, action: Action.SetFileOrFolderEditing) => ({
      ...state,
      [action.id]: {
        expanded: true,
        editing: action.editing,
        fileName: action.editing ? action.fileName : '',
        description: action.editing ? action.description : '',
        fileNameRaw: action.editing ? action.fileName : '',
        descriptionRaw: action.editing ? action.description : '',
      },
    }),
    UPDATE_FILE_OR_FOLDER_DESCRIPTION: (state, action: Action.UpdateFileOrFolderDescription) => ({
      ...state,
      [action.id]: {
        ...state[action.id],
        description: action.description,
      },
    }),
    UPDATE_FILE_OR_FOLDER_NAME: (state, action: Action.UpdateFileOrFolderName) => ({
      ...state,
      [action.id]: {
        ...state[action.id],
        fileName: action.name,
      },
    }),
    RENAME_FILE_DROP_FOLDER: (state, action: Action.RenameFileDropFolder) => ({
      ...state,
      [action.request.directoryId]: {
        ...state[action.request.directoryId],
        saving: true,
      },
    }),
    RENAME_FILE_DROP_FOLDER_FAILED: (state) => unsetSaveStatusForFileDropDirectoryContentModel(state),
    UPDATE_FILE_DROP_FOLDER: (state, action: Action.UpdateFileDropFolder) => ({
      ...state,
      [action.request.folderId]: {
        ...state[action.request.folderId],
        saving: true,
      },
    }),
    DELETE_FILE_DROP_FILE_SUCCEEDED: (__, { response }: Action.DeleteFileDropFileSucceeded) =>
      setFileDropDirectoryContentModel(response),
    DELETE_FILE_DROP_FOLDER_SUCCEEDED: (__, { response }: Action.DeleteFileDropFolderSucceeded) =>
      setFileDropDirectoryContentModel(response),
    UPDATE_FILE_DROP_FILE_SUCCEEDED: (__, { response }: Action.UpdateFileDropFileSucceeded) =>
      setFileDropDirectoryContentModel(response),
    UPDATE_FILE_DROP_FOLDER_SUCCEEDED: (__, { response }: Action.UpdateFileDropFolderSucceeded) =>
      setFileDropDirectoryContentModel(response),
    RENAME_FILE_DROP_FILE_SUCCEEDED: (__, { response }: Action.RenameFileDropFileSucceeded) =>
      setFileDropDirectoryContentModel(response),
    RENAME_FILE_DROP_FOLDER_SUCCEEDED: (__, { response }: Action.RenameFileDropFolderSucceeded) =>
      setFileDropDirectoryContentModel(response),
    SELECT_CLIENT: () => {
      return {};
    },
    SELECT_FILE_DROP: () => {
      return {};
    },
    OPEN_CREATE_FILE_DROP_MODAL: () => {
      return {};
    },
    SELECT_FILE_DROP_TAB: () => {
      return {};
    },
  },
);

/** Reducer that combines the cardAttributes reducers */
const cardAttributes = combineReducers({
  clients: clientCardAttributes,
  fileDrops: fileDropCardAttributes,
  fileDropContents: fileDropContentAttributes,
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
      fileDropContents: _initialFilterValues.fileDropContents,
    }),
    FETCH_FOLDER_CONTENTS_SUCCEEDED: (state) => ({
      ...state,
      fileDropContents: _initialFilterValues.fileDropContents,
    }),
    ENTER_CREATE_FOLDER_MODE: (state) => ({
      ...state,
      fileDropContents: _initialFilterValues.fileDropContents,
    }),
    CREATE_FILE_DROP_FOLDER_SUCCEEDED: (state) => ({
      ...state,
      fileDropContents: _initialFilterValues.fileDropContents,
    }),
    BEGIN_FILE_DROP_FILE_UPLOAD: (state) => ({
      ...state,
      fileDropContents: _initialFilterValues.fileDropContents,
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
  formModified: createModalReducer(['OPEN_MODIFIED_FORM_MODAL'], [
    'CLOSE_MODIFIED_FORM_MODAL',
    'DISCARD_PENDING_PERMISSION_GROUP_CHANGES',
    'SELECT_CLIENT',
    'SELECT_FILE_DROP',
    'CREATE_FILE_DROP',
    'EDIT_FILE_DROP',
    'DELETE_FILE_DROP',
    'SELECT_FILE_DROP_TAB',
  ]),
  passwordNotification: createModalReducer(['GENERATE_NEW_SFTP_PASSWORD_SUCCEEDED'], [
    'CLOSE_PASSWORD_NOTIFICATION_MODAL',
  ]),
  moveFileDropItem: createModalReducer(['OPEN_MOVE_FILE_DROP_ITEM_MODAL'], [
    'CLOSE_MOVE_FILE_DROP_ITEM_MODAL',
    'RENAME_FILE_DROP_FILE_SUCCEEDED',
    'RENAME_FILE_DROP_FILE_FAILED',
    'RENAME_FILE_DROP_FOLDER_SUCCEEDED',
    'RENAME_FILE_DROP_FOLDER_FAILED',
  ]),
  deleteFileDropItem: createModalReducer(['OPEN_DELETE_FILE_DROP_ITEM_MODAL'], [
    'CLOSE_DELETE_FILE_DROP_ITEM_MODAL',
    'DELETE_FILE_DROP_FILE_SUCCEEDED',
    'DELETE_FILE_DROP_FILE_FAILED',
    'DELETE_FILE_DROP_FOLDER_SUCCEEDED',
    'DELETE_FILE_DROP_FOLDER_FAILED',
  ]),
});

// ~~~~~~~~~~~~~
// Data Reducers
// ~~~~~~~~~~~~~

/** Reducer for the data state object */
const data = createReducer<State.FileDropDataState>(_initialData, {
  FETCH_CLIENTS_SUCCEEDED: (state, action: Action.FetchClientsSucceeded) => ({
    ...state,
    clients: {
      ...action.response.clients,
    },
    permissionGroups: null,
    fileDropSettings: _initialFileDropSettings,
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
    fileDropSettings: _initialFileDropSettings,
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
    permissionGroups: {
      ...action.response.permissionGroups,
    },
  }),
  DELETE_FILE_DROP_SUCCEEDED: (state, action: Action.DeleteFileDropSucceeded) => {
    const deletedActive = action.response.currentFileDropId === state.permissionGroups.fileDropId;
    return {
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
      activityLogEvents: deletedActive ? null : state.activityLogEvents,
      fileDropSettings: deletedActive ? _initialFileDropSettings : state.fileDropSettings,
      permissionGroups: deletedActive ? _initialPermissionGroupsTab : state.permissionGroups,
    };
  },
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
  FETCH_PERMISSION_GROUPS: (state) => ({
    ...state,
    permissionGroups: {
      ..._.cloneDeep(_initialPermissionGroupsTab),
    },
    activityLogEvents: [],
    fileDropSettings: _initialFileDropSettings,
  }),
  FETCH_PERMISSION_GROUPS_SUCCEEDED: (state, action: Action.FetchPermissionGroupsSucceeded) => ({
    ...state,
    permissionGroups: {
      ...action.response,
    },
    clients: {
      ...state.clients,
      [action.response.clientModel.id]: action.response.clientModel,
    },
  }),
  UPDATE_PERMISSION_GROUPS_SUCCEEDED: (state, action: Action.UpdatePermissionGroupsSucceeded) => {
    let userCount = 0;
    for (const pG in action.response.permissionGroups) {
      userCount = userCount + action.response.permissionGroups[pG].assignedMapUserIds.length;
    }
    return {
      ...state,
      fileDrops: {
        ...state.fileDrops,
        [action.response.fileDropId]: {
          ...state.fileDrops[action.response.fileDropId],
          userCount,
        },
      },
      permissionGroups: {
        ...action.response,
      },
      clients: {
        ...state.clients,
        [action.response.clientModel.id]: action.response.clientModel,
      },
    };
  },
  FETCH_ACTIVITY_LOG_SUCCEEDED: (state, action: Action.FetchActivityLogSucceeded) => ({
    ...state,
    activityLogEvents: action.response,
  }),
  FETCH_SETTINGS_SUCCEEDED: (state, action: Action.FetchSettingsSucceeded) => ({
    ...state,
    fileDropSettings: action.response,
  }),
  GENERATE_NEW_SFTP_PASSWORD: (state) => ({
    ...state,
    fileDropSettings: {
      ...state.fileDropSettings,
      fileDropPassword: null,
    },
  }),
  GENERATE_NEW_SFTP_PASSWORD_SUCCEEDED: (state, action: Action.GenerateNewSftpPasswordSucceeded) => ({
    ...state,
    fileDropSettings: {
      ...state.fileDropSettings,
      userHasPassword: true,
      isPasswordExpired: false,
      fileDropPassword: action.response.password,
    },
  }),
  SET_FILE_DROP_NOTIFICATION_SETTING_SUCCEEDED: (state, action: Action.SetFileDropNotificationSettingSucceeded) => ({
    ...state,
    fileDropSettings: action.response,
  }),
  SELECT_CLIENT: (state) => ({
    ...state,
    fileDrops: _initialData.fileDrops,
    fileDropContents: _initialData.fileDropContents,
    permissionGroups: _initialData.permissionGroups,
    activityLogEvents: _initialData.activityLogEvents,
    fileDropSettings: _initialData.fileDropSettings,
    fileDropContentsForMove: _initialData.fileDropContentsForMove,
  }),
  SELECT_FILE_DROP: (state) => ({
    ...state,
    fileDropContents: _initialData.fileDropContents,
    permissionGroups: _initialData.permissionGroups,
    activityLogEvents: _initialData.activityLogEvents,
    fileDropSettings: _initialData.fileDropSettings,
    fileDropContentsForMove: _initialData.fileDropContentsForMove,
  }),
  FETCH_FOLDER_CONTENTS_SUCCEEDED: (state, action: Action.FetchFolderContentsSucceeded) => ({
    ...state,
    fileDropContents: action.response,
  }),
  FETCH_FOLDER_CONTENTS_FOR_MOVE_SUCCEEDED: (state, action: Action.FetchFolderContentsForMoveSucceeded) => ({
    ...state,
    fileDropContentsForMove: action.response,
  }),
  CREATE_FILE_DROP_FOLDER_FOR_MOVE_SUCCEEDED: (state, action: Action.CreateFileDropFolderForMoveSucceeded) => ({
    ...state,
    fileDropContents: action.response,
    fileDropContentsForMove: action.response,
  }),
  DELETE_FILE_DROP_FILE_SUCCEEDED: (state, action: Action.DeleteFileDropFileSucceeded) => ({
    ...state,
    fileDropContents: action.response,
  }),
  DELETE_FILE_DROP_FOLDER_SUCCEEDED: (state, action: Action.DeleteFileDropFolderSucceeded) => ({
    ...state,
    fileDropContents: action.response,
  }),
  UPDATE_FILE_DROP_FILE_SUCCEEDED: (state, action: Action.UpdateFileDropFileSucceeded) => ({
    ...state,
    fileDropContents: action.response,
  }),
  CREATE_FILE_DROP_FOLDER_SUCCEEDED: (state, action: Action.CreateFileDropFolderSucceeded) => ({
    ...state,
    fileDropContents: action.response,
  }),
  UPDATE_FILE_DROP_FOLDER_SUCCEEDED: (state, action: Action.UpdateFileDropFolderSucceeded) => ({
    ...state,
    fileDropContents: action.response,
  }),
  RENAME_FILE_DROP_FILE_SUCCEEDED: (state, action: Action.RenameFileDropFileSucceeded) => ({
    ...state,
    fileDropContents: action.response,
  }),
  RENAME_FILE_DROP_FOLDER_SUCCEEDED: (state, action: Action.RenameFileDropFolderSucceeded) => ({
    ...state,
    fileDropContents: action.response,
  }),
  OPEN_CREATE_FILE_DROP_MODAL: (state) => ({
    ...state,
    permissionGroups: null,
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
