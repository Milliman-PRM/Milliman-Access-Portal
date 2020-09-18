import {
    createActionCreator, createRequestActionCreator,
} from '../../shared-components/redux/action-creators';
import * as AccessReviewActions from './actions';

export const selectClient =
  createActionCreator<AccessReviewActions.SelectClient>('SELECT_CLIENT');

export const setFilterTextClient =
  createActionCreator<AccessReviewActions.SetFilterTextClient>('SET_FILTER_TEXT_CLIENT');

export const goToNextAccessReviewStep =
  createActionCreator<AccessReviewActions.GoToNextAccessReviewStep>('GO_TO_NEXT_ACCESS_REVIEW_STEP');
export const goToPreviousAccessReviewStep =
  createActionCreator<AccessReviewActions.GoToPreviousAccessReviewStep>('GO_TO_PREVIOUS_ACCESS_REVIEW_STEP');
export const cancelClientAccessReview =
  createActionCreator<AccessReviewAction.CancelClientAccessReview>('CANCEL_CLIENT_ACCESS_REVIEW');

// Data fetches
export const fetchGlobalData =
  createRequestActionCreator<AccessReviewActions.FetchGlobalData>('FETCH_GLOBAL_DATA');
export const fetchClients =
  createRequestActionCreator<AccessReviewActions.FetchClients>('FETCH_CLIENTS');
export const fetchClientSummary =
  createRequestActionCreator<AccessReviewActions.FetchClientSummary>('FETCH_CLIENT_SUMMARY');
export const fetchClientReview =
  createRequestActionCreator<AccessReviewActions.FetchClientReview>('FETCH_CLIENT_REVIEW');
export const fetchSessionCheck =
  createRequestActionCreator<AccessReviewActions.FetchSessionCheck>('FETCH_SESSION_CHECK');

export const scheduleSessionCheck =
  createActionCreator<AccessReviewActions.ScheduleSessionCheck>('SCHEDULE_SESSION_CHECK');
