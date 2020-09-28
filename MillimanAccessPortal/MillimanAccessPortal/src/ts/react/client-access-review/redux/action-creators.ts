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
export const toggleContentItemReviewStatus =
  createActionCreator<AccessReviewActions.ToggleContentItemReviewStatus>('TOGGLE_CONTENT_ITEM_REVIEW_STATUS');
export const toggleFileDropReviewStatus =
  createActionCreator<AccessReviewActions.ToggleFileDropReviewStatus>('TOGGLE_FILE_DROP_REVIEW_STATUS');
export const cancelClientAccessReview =
  createActionCreator<AccessReviewActions.CancelClientAccessReview>('CANCEL_CLIENT_ACCESS_REVIEW');
export const openLeavingActiveReviewModal =
  createActionCreator<AccessReviewActions.OpenLeavingActiveReviewModal>('OPEN_LEAVING_ACTIVE_REVIEW_MODAL');
export const closeLeavingActiveReviewModal =
  createActionCreator<AccessReviewActions.CloseLeavingActiveReviewModal>('CLOSE_LEAVING_ACTIVE_REVIEW_MODAL');
export const updateNavBar =
  createActionCreator<AccessReviewActions.UpdateNavBar>('UPDATE_NAV_BAR');

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

// Data posts
export const approveClientAccessReview =
  createRequestActionCreator<AccessReviewActions.ApproveClientAccessReview>('APPROVE_CLIENT_ACCESS_REVIEW');

export const scheduleSessionCheck =
  createActionCreator<AccessReviewActions.ScheduleSessionCheck>('SCHEDULE_SESSION_CHECK');
