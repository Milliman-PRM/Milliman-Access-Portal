import * as toastr from 'react-redux-toastr';
import { applyMiddleware, createStore } from 'redux';
import { composeWithDevTools } from 'redux-devtools-extension';
import createSagaMiddleware from 'redux-saga';

import { FileDropClientWithStats, FileDropWithStats, Guid, PermissionGroupsReturnModel } from '../../models';
import { CardAttributes } from '../../shared-components/card/card';
import { Dict, FilterState, ModalState } from '../../shared-components/redux/store';
import { fileDropReducerState } from './reducers';
import sagas from './sagas';

// ~~~~~~~~~~~~~~~~~~~~
// Define State Objects
// ~~~~~~~~~~~~~~~~~~~~

/** Flags indicating whether the page is waiting on new data for an entity type */
export interface FileDropPendingReturnState {
  globalData: boolean;
  clients: boolean;
  fileDrops: boolean;
  createFileDrop: boolean;
  deleteFileDrop: boolean;
  updateFileDrop: boolean;
  permissions: boolean;
  permissionsUpdate: boolean;
}

/** Data used in the Create File Drop modal form */
export interface FileDropFormStateData {
  clientId: Guid;
  id?: Guid;
  fileDropName: string;
  fileDropDescription: string;
  errors: {
    fileDropName: string;
    fileDropDescription: string;
  };
}

/** Available File Drop tabs */
export type AvailableFileDropTabs = 'files' | 'permissions' | 'activityLog' | 'settings';

/** Permission Group form data for modal */
export interface PermissionGroupFormData {
  name: string;
  isPersonalGroup: boolean;
  assignedMapUserIds: Guid[];
}

/** All state that represents the user interactions with the page */
export interface FileDropPendingState {
  async: FileDropPendingReturnState;
  statusTries: number;
  createFileDrop: FileDropFormStateData;
  editFileDrop: FileDropFormStateData;
  fileDropToDelete: FileDropWithStats;
  fileDropToEdit: FileDropWithStats;
  selectedFileDropTab: AvailableFileDropTabs;
  permissionGroupsTab: PermissionGroupsReturnModel;
}

/** State representing user-selected entities */
export interface FileDropSelectedState {
  client: Guid;
  fileDrop: Guid | 'NEW FILE DROP';
}

/** State representing raw (unaltered) data returned from the server */
export interface FileDropDataState {
  clients: Dict<FileDropClientWithStats>;
  fileDrops: Dict<FileDropWithStats>;
  permissionGroups: PermissionGroupsReturnModel;
}

/** State representing entity Card attribute collections */
export interface FileDropCardAttributesState {
  clients: Dict<CardAttributes>;
  fileDrops: Dict<CardAttributes>;
}

/** State representing filter strings */
export interface FileDropFilterState {
  client: FilterState;
  fileDrop: FilterState;
  permissions: FilterState;
  activityLog: FilterState;
}

/** State representing modals */
export interface FileDropModals {
  createFileDrop: ModalState;
  deleteFileDrop: ModalState;
  confirmDeleteFileDrop: ModalState;
  createNewPermissionGroup: ModalState;
  addNewPermissionGroupUser: ModalState;
}

/** Top-Level File Drop state */
export interface FileDropState {
  pending: FileDropPendingState;
  selected: FileDropSelectedState;
  cardAttributes: FileDropCardAttributesState;
  filters: FileDropFilterState;
  modals: FileDropModals;
  data: FileDropDataState;
  toastr: toastr.ToastrState;
}

// ~~~~~~~~~~~~~~~~~~~~~~~
// Instantiate Redux Store
// ~~~~~~~~~~~~~~~~~~~~~~~

const sagaMiddleware = createSagaMiddleware();

/** File Drop Redux Store with Saga Middleware */
export const store = createStore(
  fileDropReducerState,
  composeWithDevTools(
    applyMiddleware(sagaMiddleware),
  ));
sagaMiddleware.run(sagas);
