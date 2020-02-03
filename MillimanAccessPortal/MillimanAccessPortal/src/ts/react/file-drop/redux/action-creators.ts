import * as Action from './actions';

import { createActionCreator, createRequestActionCreator } from '../../shared-components/redux/action-creators';

// ~~~~~~~~~~~~
// Page Actions
// ~~~~~~~~~~~~

/** Select a give Client by ID */
export const selectClient =
  createActionCreator<Action.SelectClient>('SELECT_CLIENT');

/** Set the Client filter */
export const setFilterTextClient =
  createActionCreator<Action.SetFilterTextClient>('SET_FILTER_TEXT_CLIENT');

// ~~~~~~~~~~~~~~~~~~~~
// Async/Server Actions
// ~~~~~~~~~~~~~~~~~~~~

/** Fetch global page data from the server */
export const fetchGlobalData =
  createRequestActionCreator<Action.FetchGlobalData>('FETCH_GLOBAL_DATA');

/** Fetch all authorized Clients from the server */
export const fetchClients =
  createRequestActionCreator<Action.FetchClients>('FETCH_CLIENTS');

// ~~~~~~~~~~~~~~~~~~~~~~
// Status Refresh Actions
// ~~~~~~~~~~~~~~~~~~~~~~

/** Schedule a status refresh after a given delay */
export const scheduleStatusRefresh =
  createActionCreator<Action.ScheduleStatusRefresh>('SCHEDULE_STATUS_REFRESH');

/** Fetch the refreshed status information from the server */
export const fetchStatusRefresh =
  createRequestActionCreator<Action.FetchStatusRefresh>('FETCH_STATUS_REFRESH');

/** Decrement the number of status refresh attempts to determine when a threshold has been crossed */
export const decrementStatusRefreshAttempts =
  createActionCreator<Action.DecrementStatusRefreshAttempts>('DECREMENT_STATUS_REFRESH_ATTEMPTS');

/** Notify the user that the status refresh has been stopped */
export const promptStatusRefreshStopped =
  createActionCreator<Action.PromptStatusRefreshStopped>('PROMPT_STATUS_REFRESH_STOPPED');

// ~~~~~~~~~~~~~~~~~~~~~
// Session Check Actions
// ~~~~~~~~~~~~~~~~~~~~~

/** Schedule a session check call after a given delay */
export const scheduleSessionCheck =
  createActionCreator<Action.ScheduleSessionCheck>('SCHEDULE_SESSION_CHECK');

/** Fetch a session check from the server */
export const fetchSessionCheck =
  createRequestActionCreator<Action.FetchSessionCheck>('FETCH_SESSION_CHECK');
