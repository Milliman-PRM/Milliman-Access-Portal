import { createActionCreator, createRequestActionCreator } from '../../shared-components/redux/action-creators';
import * as Action from './actions';

export const selectClient =
  createActionCreator<Action.SelectClient>('SELECT_CLIENT');

export const setFilterTextClient =
  createActionCreator<Action.SetFilterTextClient>('SET_FILTER_TEXT_CLIENT');

export const promptStatusRefreshStopped =
  createActionCreator<Action.PromptStatusRefreshStopped>('PROMPT_STATUS_REFRESH_STOPPED');

// Data fetches
export const fetchGlobalData =
  createRequestActionCreator<Action.FetchGlobalData>('FETCH_GLOBAL_DATA');
export const fetchClients =
  createRequestActionCreator<Action.FetchClients>('FETCH_CLIENTS');
export const fetchStatusRefresh =
  createRequestActionCreator<Action.FetchStatusRefresh>('FETCH_STATUS_REFRESH');
export const fetchSessionCheck =
  createRequestActionCreator<Action.FetchSessionCheck>('FETCH_SESSION_CHECK');

// Updates

// Modal actions

// Scheduled actions
export const scheduleStatusRefresh =
  createActionCreator<Action.ScheduleStatusRefresh>('SCHEDULE_STATUS_REFRESH');
export const decrementStatusRefreshAttempts =
  createActionCreator<Action.DecrementStatusRefreshAttempts>('DECREMENT_STATUS_REFRESH_ATTEMPTS');
export const scheduleSessionCheck =
  createActionCreator<Action.ScheduleSessionCheck>('SCHEDULE_SESSION_CHECK');
