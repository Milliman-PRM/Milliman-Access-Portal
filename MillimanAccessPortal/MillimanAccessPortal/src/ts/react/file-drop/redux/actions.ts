import {
  FileDrop, FileDropClientWithStats, FileDropsReturnModel, FileDropWithStats, Guid,
} from '../../models';
import { TSError } from '../../shared-components/redux/actions';
import { Dict } from '../../shared-components/redux/store';

// ~~~~~~~~~~~~
// Page Actions
// ~~~~~~~~~~~~

/**
 *  Select the client card specified by id
 *  If id refers to the currently selected card, deselect it
 */
export interface SelectClient {
  type: 'SELECT_CLIENT';
  id: Guid;
}

/**
 *  Select the File Drop card specified by id
 *  If id refers to the currently selected card, deselect it
 */
export interface SelectFileDrop {
  type: 'SELECT_FILE_DROP';
  id: Guid | 'NEW FILE DROP';
}

/** Set filter text for the client card filter */
export interface SetFilterTextClient {
  type: 'SET_FILTER_TEXT_CLIENT';
  text: string;
}

/** Set filter text for the client card filter */
export interface SetFilterTextFileDrop {
  type: 'SET_FILTER_TEXT_FILE_DROP';
  text: string;
}

/** Open the Create File Drop Modal */
export interface OpenCreateFileDropModal {
  type: 'OPEN_CREATE_FILE_DROP_MODAL';
  clientId: Guid;
}

/** Close the Create File Drop Modal */
export interface CloseCreateFileDropModal {
  type: 'CLOSE_CREATE_FILE_DROP_MODAL';
}

/** Update the input values of the Create File Drop Modal form */
export interface UpdateFileDropFormData {
  type: 'UPDATE_FILE_DROP_FORM_DATA';
  updateType: 'create' | 'edit';
  field: 'fileDropName' | 'fileDropDescription';
  value: string;
}

/** Open the modal used to begin File Drop deletion */
export interface OpenDeleteFileDropModal {
  type: 'OPEN_DELETE_FILE_DROP_MODAL';
  fileDrop: FileDropWithStats;
}

/** Close the modal used to begin File Drop deletion */
export interface CloseDeleteFileDropModal {
  type: 'CLOSE_DELETE_FILE_DROP_MODAL';
}

/** Open the modal used to confirm File Drop deletion */
export interface OpenDeleteFileDropConfirmationModal {
  type: 'OPEN_DELETE_FILE_DROP_CONFIRMATION_MODAL';
}

/** Close the modal used to confirm File Drop deletion */
export interface CloseDeleteFileDropConfirmationModal {
  type: 'CLOSE_DELETE_FILE_DROP_CONFIRMATION_MODAL';
}

/** Put the File Drop in edit mode */
export interface EditFileDrop {
  type: 'EDIT_FILE_DROP';
  fileDrop: FileDropWithStats;
}

/** Take the File Drop out of edit mode */
export interface CancelFileDropEdit {
  type: 'CANCEL_FILE_DROP_EDIT';
}

// ~~~~~~~~~~~~~~~~~~~~
// Async/Server Actions
// ~~~~~~~~~~~~~~~~~~~~

/**
 * GET:
 *   Non-client/non-File Drop data used for the functioning of the page
 */
export interface FetchGlobalData {
  type: 'FETCH_GLOBAL_DATA';
  request: {};
}
/** Action called upon successful return of the FetchGlobalData API call */
export interface FetchGlobalDataSucceeded {
  type: 'FETCH_GLOBAL_DATA_SUCCEEDED';
  response: {};
}
/** Action called upon return of an error from the FetchGlobalData API call */
export interface FetchGlobalDataFailed {
  type: 'FETCH_GLOBAL_DATA_FAILED';
  error: TSError;
}

/**
 * GET:
 *   Clients the current user has access to publish for
 *   Users who are File Drop eligible in those clients
 */
export interface FetchClients {
  type: 'FETCH_CLIENTS';
  request: {};
}
/** Action called upon successful return of the FetchClients API call */
export interface FetchClientsSucceeded {
  type: 'FETCH_CLIENTS_SUCCEEDED';
  response: {
    clients: Dict<FileDropClientWithStats>;
  };
}
/** Action called upon return of an error from the FetchClients API call */
export interface FetchClientsFailed {
  type: 'FETCH_CLIENTS_FAILED';
  error: TSError;
}

/**
 * GET:
 *   File Drops the current user has access to
 */
export interface FetchFileDrops {
  type: 'FETCH_FILE_DROPS';
  request: {
    clientId: Guid;
  };
}
/** Action called upon successful return of the FetchFileDrops API call */
export interface FetchFileDropsSucceeded {
  type: 'FETCH_FILE_DROPS_SUCCEEDED';
  response: FileDropsReturnModel;
}
/** Action called upon return of an error from the FetchFileDrops API call */
export interface FetchFileDropsFailed {
  type: 'FETCH_FILE_DROPS_FAILED';
  error: TSError;
}

/**
 * POST:
 *   Create a new File Drop
 */
export interface CreateFileDrop {
  type: 'CREATE_FILE_DROP';
  request: FileDrop;
}
/** Action called upon successful return of the CreateFileDrop API call */
export interface CreateFileDropSucceeded {
  type: 'CREATE_FILE_DROP_SUCCEEDED';
  response: FileDropsReturnModel;
}
/** Action called upon return of an error from the CreateFileDrop API call */
export interface CreateFileDropFailed {
  type: 'CREATE_FILE_DROP_FAILED';
  error: TSError;
}

/**
 * DELETE:
 *   Delete a File Drop
 */
export interface DeleteFileDrop {
  type: 'DELETE_FILE_DROP';
  request: Guid;
}
/** Action called upon successful return of the DeleteFileDrop API call */
export interface DeleteFileDropSucceeded {
  type: 'DELETE_FILE_DROP_SUCCEEDED';
  response: FileDropsReturnModel;
}
/** Action called upon return of an error from the DeleteFileDrop API call */
export interface DeleteFileDropFailed {
  type: 'DELETE_FILE_DROP_FAILED';
  error: TSError;
}

/**
 * POST:
 *   Update a File Drop
 */
export interface UpdateFileDrop {
  type: 'UPDATE_FILE_DROP';
  request: FileDrop;
}
/** Action called upon successful return of the DeleteFileDrop API call */
export interface UpdateFileDropSucceeded {
  type: 'UPDATE_FILE_DROP_SUCCEEDED';
  response: FileDropsReturnModel;
}
/** Action called upon return of an error from the DeleteFileDrop API call */
export interface UpdateFileDropFailed {
  type: 'UPDATE_FILE_DROP_FAILED';
  error: TSError;
}

// ~~~~~~~~~~~~~~~~~~~~~~
// Status Refresh Actions
// ~~~~~~~~~~~~~~~~~~~~~~

/** Schedule a status refresh after a given delay */
export interface ScheduleStatusRefresh {
  type: 'SCHEDULE_STATUS_REFRESH';
  delay: number;
}

/**
 * GET:
 *   Updates to the selected Client specified by clientId
 */
export interface FetchStatusRefresh {
  type: 'FETCH_STATUS_REFRESH';
  request: {
    clientId: Guid;
  };
}
/** Action called upon successful return of the FetchStatusRefresh API call */
export interface FetchStatusRefreshSucceeded {
  type: 'FETCH_STATUS_REFRESH_SUCCEEDED';
  response: {};
}
/** Action called upon return of an error from the FetchStatusRefresh API call */
export interface FetchStatusRefreshFailed {
  type: 'FETCH_STATUS_REFRESH_FAILED';
  error: TSError;
}

/** Decrement remaining status refresh attempts */
export interface DecrementStatusRefreshAttempts {
  type: 'DECREMENT_STATUS_REFRESH_ATTEMPTS';
}

/** Display a toast indicating that the status refresh polling has stopped */
export interface PromptStatusRefreshStopped {
  type: 'PROMPT_STATUS_REFRESH_STOPPED';
}

// ~~~~~~~~~~~~~~~~~~~~~
// Session Check Actions
// ~~~~~~~~~~~~~~~~~~~~~

/**
 * GET:
 *   A bodiless response that serves as a session heartbeat
 */
export interface FetchSessionCheck {
  type: 'FETCH_SESSION_CHECK';
  request: {};
}
/** Action called upon successful return of the FetchSessionCheck API call */
export interface FetchSessionCheckSucceeded {
  type: 'FETCH_SESSION_CHECK_SUCCEEDED';
  response: {};
}
/** Action called upon return of an error from the FetchSessionCheck API call */
export interface FetchSessionCheckFailed {
  type: 'FETCH_SESSION_CHECK_FAILED';
  error: TSError;
}

/** Schedule a session check after a given delay */
export interface ScheduleSessionCheck {
  type: 'SCHEDULE_SESSION_CHECK';
  delay: number;
}

// ~~~~~~~~~~~~~
// Action Unions
// ~~~~~~~~~~~~~

/** Actions that change the state of the page */
export type FileDropPageActions =
  | SelectClient
  | SelectFileDrop
  | PromptStatusRefreshStopped
  | DecrementStatusRefreshAttempts
  | OpenCreateFileDropModal
  | CloseCreateFileDropModal
  | UpdateFileDropFormData
  | OpenDeleteFileDropModal
  | CloseDeleteFileDropModal
  | OpenDeleteFileDropConfirmationModal
  | CloseDeleteFileDropConfirmationModal
  | EditFileDrop
  | CancelFileDropEdit
  ;

/** Actions that schedule another action */
export type FileDropScheduleActions =
  | ScheduleSessionCheck
  | ScheduleStatusRefresh
  ;

/** Actions that makes Ajax requests */
export type FileDropRequestActions =
  | FetchGlobalData
  | FetchClients
  | FetchFileDrops
  | CreateFileDrop
  | DeleteFileDrop
  | UpdateFileDrop
  | FetchStatusRefresh
  | FetchSessionCheck
  ;

/** Actions that marks the succesful response of an Ajax request */
export type FileDropSuccessResponseActions =
  | FetchGlobalDataSucceeded
  | FetchClientsSucceeded
  | FetchFileDropsSucceeded
  | CreateFileDropSucceeded
  | DeleteFileDropSucceeded
  | UpdateFileDropSucceeded
  | FetchStatusRefreshSucceeded
  | FetchSessionCheckSucceeded
  ;

/** Actions that marks the errored response of an Ajax request */
export type FileDropErrorActions =
  | FetchGlobalDataFailed
  | FetchClientsFailed
  | FetchFileDropsFailed
  | CreateFileDropFailed
  | DeleteFileDropFailed
  | UpdateFileDropFailed
  | FetchStatusRefreshFailed
  | FetchSessionCheckFailed
  ;

/** Actions that set filter text */
export type FilterActions =
  | SetFilterTextClient
  | SetFilterTextFileDrop
  ;

/** All available File Drop Actions */
export type FileDropActions =
  | FileDropPageActions
  | FileDropScheduleActions
  | FileDropRequestActions
  | FileDropSuccessResponseActions
  | FileDropErrorActions
  | FilterActions
  ;

/** An action that opens a modal */
export type OpenModalAction =
  | OpenCreateFileDropModal
  | OpenDeleteFileDropModal
  | OpenDeleteFileDropConfirmationModal
  ;
