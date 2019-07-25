import { select, takeLatest } from 'redux-saga/effects';

import { ClientWithEligibleUsers, RootContentItemWithStats } from '../../models';
import {
  createTakeEveryToast, createTakeLatestRequest, createTakeLatestSchedule,
} from '../../shared-components/redux/sagas';
import * as AccessActionCreators from './action-creators';
import {
  ErrorPublishingAction, PublishingAction, RequestPublishingAction, ResponsePublishingAction,
} from './actions';
import * as api from './api';
import { remainingStatusRefreshAttempts, selectedClient, selectedItem } from './selectors';

/**
 * Custom effect for handling request actions.
 * @param type Action type
 * @param apiCall API method to invoke
 */
const takeLatestRequest = createTakeLatestRequest<RequestPublishingAction, ResponsePublishingAction>();

/**
 * Custom effect for handling schedule actions.
 * @param type action type
 * @param nextActionCreator action creator to invoke after the scheduled duration
 */
const takeLatestSchedule = createTakeLatestSchedule<PublishingAction>();

/**
 * Custom effect for handling actions that result in toasts.
 * @param type action type
 * @param message message to display, or a function that builds the message from a response
 * @param level message severity
 */
const takeEveryToast = createTakeEveryToast<PublishingAction, ResponsePublishingAction>();

/**
 * Register all sagas for the page.
 */
export default function* rootSaga() {
  // API requests
  yield takeLatestRequest('FETCH_GLOBAL_DATA', api.fetchGlobalData);
  yield takeLatestRequest('FETCH_CLIENTS', api.fetchClients);
  yield takeLatestRequest('FETCH_ITEMS', api.fetchItems);
  yield takeLatestRequest('FETCH_STATUS_REFRESH', api.fetchStatusRefresh);
  yield takeLatestRequest('FETCH_SESSION_CHECK', api.fetchSessionCheck);

  // Scheduled actions
  yield takeLatestSchedule('SCHEDULE_STATUS_REFRESH', function*() {
    const client: ClientWithEligibleUsers = yield select(selectedClient);
    const item: RootContentItemWithStats = yield select(selectedItem);
    return client
      ? AccessActionCreators.fetchStatusRefresh({
        clientId: client.id,
        contentItemId: item && item.id,
      })
      : AccessActionCreators.scheduleStatusRefresh({ delay: 5000 });
  });
  yield takeLatestSchedule('FETCH_STATUS_REFRESH_SUCCEEDED',
    () => AccessActionCreators.scheduleStatusRefresh({ delay: 5000 }));
  yield takeLatestSchedule('FETCH_STATUS_REFRESH_FAILED',
    () => AccessActionCreators.decrementStatusRefreshAttempts({}));
  yield takeLatestSchedule('DECREMENT_STATUS_REFRESH_ATTEMPTS', function*() {
    const retriesLeft: number = yield select(remainingStatusRefreshAttempts);
    return retriesLeft
      ? AccessActionCreators.scheduleStatusRefresh({ delay: 5000 })
      : AccessActionCreators.promptStatusRefreshStopped({});
  });
  yield takeLatestSchedule('SCHEDULE_SESSION_CHECK', () => AccessActionCreators.fetchSessionCheck({}));
  yield takeLatestSchedule('FETCH_SESSION_CHECK_SUCCEEDED',
    () => AccessActionCreators.scheduleSessionCheck({ delay: 60000 }));
  yield takeLatest('FETCH_SESSION_CHECK_FAILED', function*() { yield window.location.reload(); });

  // Toasts
  yield takeEveryToast('PROMPT_STATUS_REFRESH_STOPPED',
    'Please refresh the page to update reduction status.', 'warning');
  yield takeEveryToast<ErrorPublishingAction>([
    'FETCH_GLOBAL_DATA_FAILED',
    'FETCH_CLIENTS_FAILED',
    'FETCH_ITEMS_FAILED',
    'FETCH_SESSION_CHECK_FAILED',
  ], ({ message }) => message === 'sessionExpired'
    ? 'Your session has expired. Please refresh the page.'
    : isNaN(message)
      ? message
      : 'An unexpected error has occurred.',
    'error');
}
