import {
    createActionCreator, createRequestActionCreator,
} from '../../shared-components/redux/action-creators';
import * as AccessReviewActions from './actions';

export const selectClient =
  createActionCreator<AccessReviewActions.SelectClient>('SELECT_CLIENT');

export const setFilterTextClient =
  createActionCreator<AccessReviewActions.SetFilterTextClient>('SET_FILTER_TEXT_CLIENT');

// Data fetches
export const fetchGlobalData =
  createRequestActionCreator<AccessReviewActions.FetchGlobalData>('FETCH_GLOBAL_DATA');
export const fetchClients =
  createRequestActionCreator<AccessReviewActions.FetchClients>('FETCH_CLIENTS');
export const fetchSessionCheck =
  createRequestActionCreator<AccessReviewActions.FetchSessionCheck>('FETCH_SESSION_CHECK');

export const scheduleSessionCheck =
  createActionCreator<AccessReviewActions.ScheduleSessionCheck>('SCHEDULE_SESSION_CHECK');
