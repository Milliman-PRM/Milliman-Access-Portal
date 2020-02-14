import { FileDrop, FileDropClientWithStats, FileDropWithStats, Guid } from '../../models';
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
  | PromptStatusRefreshStopped
  | DecrementStatusRefreshAttempts
  | OpenCreateFileDropModal
  | CloseCreateFileDropModal
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
  | FetchStatusRefresh
  | FetchSessionCheck
  ;

/** Actions that marks the succesful response of an Ajax request */
export type FileDropSuccessResponseActions =
  | FetchGlobalDataSucceeded
  | FetchClientsSucceeded
  | FetchStatusRefreshSucceeded
  | FetchSessionCheckSucceeded
  ;

/** Actions that marks the errored response of an Ajax request */
export type FileDropErrorActions =
  | FetchGlobalDataFailed
  | FetchClientsFailed
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
  ;
