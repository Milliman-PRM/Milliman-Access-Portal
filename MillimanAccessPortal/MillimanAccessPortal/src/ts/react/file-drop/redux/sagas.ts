import { select, takeLatest } from 'redux-saga/effects';

import * as ActionCreator from './action-creators';
import * as Action from './actions';
import * as API from './api';
import * as Selector from './selectors';

import { ClientWithEligibleUsers } from '../../models';
import {
  createTakeEveryToast, createTakeLatestRequest, createTakeLatestSchedule,
} from '../../shared-components/redux/sagas';

// ~~~~~~~~~~~~~~~~~
// Utility Functions
// ~~~~~~~~~~~~~~~~~

/**
 * Custom effect for handling request actions.
 * @param type Action type
 * @param apiCall API method to invoke
 */
const takeLatestRequest =
  createTakeLatestRequest<Action.FileDropRequestActions, Action.FileDropSuccessResponseActions>();

/**
 * Custom effect for handling schedule actions.
 * @param type action type
 * @param nextActionCreator action creator to invoke after the scheduled duration
 */
const takeLatestSchedule = createTakeLatestSchedule<Action.FileDropActions>();

/**
 * Custom effect for handling actions that result in toasts.
 * @param type action type
 * @param message message to display, or a function that builds the message from a response
 * @param level message severity
 */
const takeEveryToast = createTakeEveryToast<Action.FileDropActions, Action.FileDropSuccessResponseActions>();

// ~~~~~~~~~~~~~~
// Register Sagas
// ~~~~~~~~~~~~~~

export default function* rootSaga() {
  // API requests
  yield takeLatestRequest('FETCH_GLOBAL_DATA', API.fetchGlobalData);
  yield takeLatestRequest('FETCH_CLIENTS', API.fetchClients);

  // Session and Status Checks
  yield takeLatestRequest('FETCH_STATUS_REFRESH', API.fetchStatusRefresh);
  yield takeLatestRequest('FETCH_SESSION_CHECK', API.fetchSessionCheck);
  yield takeLatestSchedule('SCHEDULE_STATUS_REFRESH', function*() {
  // TO DO: implement status endpoint
    const client: ClientWithEligibleUsers = yield select(Selector.selectedClient);
    return client
      ? ActionCreator.fetchStatusRefresh({
        clientId: client.id,
      })
      : ActionCreator.scheduleStatusRefresh({ delay: 5000 });
  });
  yield takeLatestSchedule('FETCH_STATUS_REFRESH_SUCCEEDED',
    () => ActionCreator.scheduleStatusRefresh({ delay: 5000 }));
  yield takeLatestSchedule('FETCH_STATUS_REFRESH_FAILED',
    () => ActionCreator.decrementStatusRefreshAttempts({}));
  yield takeLatestSchedule('DECREMENT_STATUS_REFRESH_ATTEMPTS', function*() {
    const retriesLeft: number = yield select(Selector.remainingStatusRefreshAttempts);
    return retriesLeft
      ? ActionCreator.scheduleStatusRefresh({ delay: 5000 })
      : ActionCreator.promptStatusRefreshStopped({});
  });
  yield takeLatestSchedule('SCHEDULE_SESSION_CHECK', () => ActionCreator.fetchSessionCheck({}));
  yield takeLatestSchedule('FETCH_SESSION_CHECK_SUCCEEDED',
    () => ActionCreator.scheduleSessionCheck({ delay: 60000 }));
  yield takeLatest('FETCH_SESSION_CHECK_FAILED', function*() { yield window.location.reload(); });

  // Toasts
  yield takeEveryToast('PROMPT_STATUS_REFRESH_STOPPED',
    'Please refresh the page to update reduction status.', 'warning');
  yield takeEveryToast<Action.FileDropErrorActions>([
    'FETCH_GLOBAL_DATA_FAILED',
    'FETCH_CLIENTS_FAILED',
    'FETCH_SESSION_CHECK_FAILED',
    'FETCH_STATUS_REFRESH_FAILED',
  ], ({ message }) => message === 'sessionExpired'
    ? 'Your session has expired. Please refresh the page.'
    : isNaN(message)
      ? message
      : 'An unexpected error has occurred.',
    'error');
}
