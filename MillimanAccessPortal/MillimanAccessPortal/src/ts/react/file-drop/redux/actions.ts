import { ClientWithStats, Guid } from '../../models';
import { TSError } from '../../shared-components/redux/actions';
import { Dict } from '../../shared-components/redux/store';

// ~~ Page actions ~~

/**
 * Exclusively select the client card specified by id.
 * If id refers to the currently selected card, deselect it.
 */
export interface SelectClient {
  type: 'SELECT_CLIENT';
  id: Guid;
}

/**
 * Display a toast indicating that the status refresh polling has stopped
 */
export interface PromptStatusRefreshStopped {
  type: 'PROMPT_STATUS_REFRESH_STOPPED';
}

// ~~ Server actions ~~

/**
 * GET:
 *   Information on Content Types and Associated Content Types.
 */
export interface FetchGlobalData {
  type: 'FETCH_GLOBAL_DATA';
  request: {};
}
export interface FetchGlobalDataSucceeded {
  type: 'FETCH_GLOBAL_DATA_SUCCEEDED';
  response: {};
}
export interface FetchGlobalDataFailed {
  type: 'FETCH_GLOBAL_DATA_FAILED';
  error: TSError;
}

/**
 * GET:
 *   clients the current user has access to publish for;
 *   users who are content eligible in any of those clients.
 */
export interface FetchClients {
  type: 'FETCH_CLIENTS';
  request: {};
}
export interface FetchClientsSucceeded {
  type: 'FETCH_CLIENTS_SUCCEEDED';
  response: {
    clients: Dict<ClientWithStats>;
  };
}
export interface FetchClientsFailed {
  type: 'FETCH_CLIENTS_FAILED';
  error: TSError;
}

/**
 * GET:
 *   content items for the selected client;
 *   publications for the selected client;
 *   publication queue information for those publications;
 *   reductions for the selected content item;
 *   reduction queue information for those reductions.
 */
export interface FetchStatusRefresh {
  type: 'FETCH_STATUS_REFRESH';
  request: {
    clientId: Guid;
  };
}
export interface FetchStatusRefreshSucceeded {
  type: 'FETCH_STATUS_REFRESH_SUCCEEDED';
  response: {};
}
export interface FetchStatusRefreshFailed {
  type: 'FETCH_STATUS_REFRESH_FAILED';
  error: TSError;
}

/**
 * Fetch status refresh after a delay.
 */
export interface ScheduleStatusRefresh {
  type: 'SCHEDULE_STATUS_REFRESH';
  delay: number;
}

/**
 * Decrement remaining status refresh attempts
 */
export interface DecrementStatusRefreshAttempts {
  type: 'DECREMENT_STATUS_REFRESH_ATTEMPTS';
}

/**
 * GET a bodiless response that serves as a session heartbeat.
 */
export interface FetchSessionCheck {
  type: 'FETCH_SESSION_CHECK';
  request: {};
}
export interface FetchSessionCheckSucceeded {
  type: 'FETCH_SESSION_CHECK_SUCCEEDED';
  response: {};
}
export interface FetchSessionCheckFailed {
  type: 'FETCH_SESSION_CHECK_FAILED';
  error: TSError;
}

/**
 * Fetch session check after a delay.
 */
export interface ScheduleSessionCheck {
  type: 'SCHEDULE_SESSION_CHECK';
  delay: number;
}

/**
 * Set filter text for the client card filter.
 */
export interface SetFilterTextClient {
  type: 'SET_FILTER_TEXT_CLIENT';
  text: string;
}

// ~~ Action unions ~~

/**
 * An action that changes the state of the page.
 */
export type FileDropPageActions =
  | SelectClient
  | PromptStatusRefreshStopped
  | DecrementStatusRefreshAttempts
  ;

/**
 * An action that schedules another action.
 */
export type FileDropScheduleActions =
  | ScheduleSessionCheck
  | ScheduleStatusRefresh
  ;

/**
 * An action that makes an Ajax request.
 */
export type FileDropRequestActions =
  | FetchGlobalData
  | FetchClients
  | FetchStatusRefresh
  | FetchSessionCheck
  ;

/**
 * An action that marks the succesful response of an Ajax request.
 */
export type FileDropSuccessResponseActions =
  | FetchGlobalDataSucceeded
  | FetchClientsSucceeded
  | FetchStatusRefreshSucceeded
  | FetchSessionCheckSucceeded
  ;

/**
 * An action that marks the errored response of an Ajax request.
 */
export type FileDropErrorActions =
  | FetchGlobalDataFailed
  | FetchClientsFailed
  | FetchStatusRefreshFailed
  | FetchSessionCheckFailed
  ;

/**
 * An action that sets filter text for a card column.
 */
export type FilterActions =
  | SetFilterTextClient
  ;

/**
 * An action available to the content publishing page.
 */
export type FileDropActions =
  | FileDropPageActions
  | FileDropScheduleActions
  | FileDropRequestActions
  | FileDropSuccessResponseActions
  | FileDropErrorActions
  | FilterActions
  ;
