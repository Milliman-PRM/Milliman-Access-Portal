import {
    createActionCreator, createRequestActionCreator,
} from '../../shared-components/redux/action-creators';
import * as PublishActions from './actions';

export const selectClient =
  createActionCreator<PublishActions.SelectClient>('SELECT_CLIENT');
export const selectItem =
  createActionCreator<PublishActions.SelectItem>('SELECT_ITEM');

export const setFilterTextClient =
  createActionCreator<PublishActions.SetFilterTextClient>('SET_FILTER_TEXT_CLIENT');
export const setFilterTextItem =
  createActionCreator<PublishActions.SetFilterTextItem>('SET_FILTER_TEXT_ITEM');

export const promptStatusRefreshStopped =
  createActionCreator<PublishActions.PromptStatusRefreshStopped>('PROMPT_STATUS_REFRESH_STOPPED');

// Data fetches
export const fetchClients =
  createRequestActionCreator<PublishActions.FetchClients>('FETCH_CLIENTS');
export const fetchItems =
  createRequestActionCreator<PublishActions.FetchItems>('FETCH_ITEMS');
export const fetchStatusRefresh =
  createRequestActionCreator<PublishActions.FetchStatusRefresh>('FETCH_STATUS_REFRESH');
export const fetchSessionCheck =
  createRequestActionCreator<PublishActions.FetchSessionCheck>('FETCH_SESSION_CHECK');

// Updates

// Scheduled actions
export const scheduleStatusRefresh =
  createActionCreator<PublishActions.ScheduleStatusRefresh>('SCHEDULE_STATUS_REFRESH');
export const decrementStatusRefreshAttempts =
  createActionCreator<PublishActions.DecrementStatusRefreshAttempts>('DECREMENT_STATUS_REFRESH_ATTEMPTS');
export const scheduleSessionCheck =
  createActionCreator<PublishActions.ScheduleSessionCheck>('SCHEDULE_SESSION_CHECK');
