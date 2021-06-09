import { select, takeLatest } from 'redux-saga/effects';

import { ClientWithReviewDate } from '../../models';
import {
    createTakeEveryToast, createTakeLatestRequest, createTakeLatestSchedule,
} from '../../shared-components/redux/sagas';
import * as AccessReviewActionCreators from './action-creators';
import {
    AccessReviewAction, ErrorAccessReviewAction, RequestAccessReviewAction, ResponseAccessReviewAction,
} from './actions';
import * as api from './api';
import { selectedClient } from './selectors';

/**
 * Custom effect for handling request actions.
 */
const takeLatestRequest = createTakeLatestRequest<RequestAccessReviewAction, ResponseAccessReviewAction>();

/**
 * Custom effect for handling schedule actions.
 */
const takeLatestSchedule = createTakeLatestSchedule<AccessReviewAction>();

/**
 * Custom effect for handling actions that result in toasts.
 */
const takeEveryToast = createTakeEveryToast<AccessReviewAction, ResponseAccessReviewAction>();

/**
 * Register all sagas for the page.
 */
export default function* rootSaga() {
  // API requests
  yield takeLatestRequest('FETCH_GLOBAL_DATA', api.fetchGlobalData);
  yield takeLatestRequest('FETCH_CLIENTS', api.fetchClients);
  yield takeLatestRequest('FETCH_CLIENT_SUMMARY', api.fetchClientSummary);
  yield takeLatestRequest('FETCH_CLIENT_REVIEW', api.fetchClientReview);
  yield takeLatestRequest('APPROVE_CLIENT_ACCESS_REVIEW', api.approvedClientAccessReview);
  yield takeLatestSchedule('APPROVE_CLIENT_ACCESS_REVIEW_SUCCEEDED', function*() {
    const client: ClientWithReviewDate = yield select(selectedClient);
    return AccessReviewActionCreators.fetchClientSummary({ clientId: client.id });
  });
  yield takeLatestSchedule('APPROVE_CLIENT_ACCESS_REVIEW_SUCCEEDED',
    () => AccessReviewActionCreators.updateNavBar({}),
  );
  yield takeLatestRequest('DOWNLOAD_CLIENT_ACCESS_REVIEW_SUMMARY', api.downloadClientAccessReviewSummary);

  // Scheduled actions
  yield takeLatestSchedule('SCHEDULE_SESSION_CHECK', () => AccessReviewActionCreators.fetchSessionCheck({}));
  yield takeLatestSchedule('FETCH_SESSION_CHECK_SUCCEEDED',
    () => AccessReviewActionCreators.scheduleSessionCheck({ delay: 60000 }));
  yield takeLatest('FETCH_SESSION_CHECK_FAILED', function*() { yield window.location.reload(); });

  // Toasts
  yield takeEveryToast('APPROVE_CLIENT_ACCESS_REVIEW_SUCCEEDED', 'Client Access Review completed.');

  yield takeEveryToast<ErrorAccessReviewAction>([
    'FETCH_GLOBAL_DATA_FAILED',
    'FETCH_CLIENTS_FAILED',
    'FETCH_CLIENT_SUMMARY_FAILED',
    'FETCH_CLIENT_REVIEW_FAILED',
    'APPROVE_CLIENT_ACCESS_REVIEW_FAILED',
    'DOWNLOAD_CLIENT_ACCESS_REVIEW_SUMMARY_FAILED',
  ], ({ message }) => message === 'sessionExpired'
      ? 'Your session has expired. Please refresh the page.'
      : isNaN(message)
        ? message
        : 'An unexpected error has occurred.',
    'error');
}
