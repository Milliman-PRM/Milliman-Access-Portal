import { takeLatest } from 'redux-saga/effects';

import {
    createTakeEveryToast, createTakeLatestRequest, createTakeLatestSchedule,
    createTakeLatestValidation,
} from '../../shared-components/redux/sagas';
import * as AccountActionCreators from './action-creators';
import {
  AccountAction, ErrorAccountAction, RequestAccountAction, RequestPasswordResetFailed, RequestPasswordResetSucceeded,
  ResponseAccountAction, ValidationAccountAction, ValidationResultAccountAction,
} from './actions';
import * as api from './api';

/**
 * Custom effect for handling request actions.
 * @param type Action type
 * @param apiCall API method to invoke
 */
const takeLatestRequest = createTakeLatestRequest<RequestAccountAction, ResponseAccountAction>();

/**
 * Custom effect for handling schedule actions.
 * @param type action type
 * @param nextActionCreator action creator to invoke after the scheduled duration
 */
const takeLatestSchedule = createTakeLatestSchedule<AccountAction>();

/**
 * Custom effect for handling actions that result in toasts.
 * @param type action type
 * @param message message to display, or a function that builds the message from a response
 * @param level message severity
 */
const takeEveryToast = createTakeEveryToast<AccountAction, ResponseAccountAction>();

const takeLatestValidation = createTakeLatestValidation<ValidationAccountAction, ValidationResultAccountAction>();

export default function* rootSaga() {
  // API requests
  yield takeLatestRequest('FETCH_USER', api.fetchUser);
  yield takeLatestRequest('FETCH_SESSION_CHECK', api.fetchSessionCheck);
  yield takeLatestRequest('UPDATE_ACCOUNT', api.updateAccount);
  yield takeLatestRequest('REQUEST_PASSWORD_RESET', api.requestPasswordReset);

  // Scheduled actions
  yield takeLatestSchedule('SCHEDULE_SESSION_CHECK', () => AccountActionCreators.fetchSessionCheck({}));
  yield takeLatestSchedule('FETCH_SESSION_CHECK_SUCCEEDED',
    () => AccountActionCreators.scheduleSessionCheck({ delay: 60000 }));
  yield takeLatest('FETCH_SESSION_CHECK_FAILED', function*() { yield window.location.reload(); });

  // Toasts
  yield takeEveryToast('UPDATE_ACCOUNT_SUCCEEDED', 'Your account has been updated.');
  yield takeEveryToast<RequestPasswordResetSucceeded>('REQUEST_PASSWORD_RESET_SUCCEEDED',
    ({ successMessage }) => successMessage);
  yield takeEveryToast<ErrorAccountAction>([
    'FETCH_USER_FAILED',
    'FETCH_SESSION_CHECK_FAILED',
    'UPDATE_ACCOUNT_FAILED',
    'REQUEST_PASSWORD_RESET_FAILED',
  ], ({ message }) => message === 'sessionExpired'
      ? 'Your session has expired. Please refresh the page.'
      : isNaN(message)
        ? message
        : 'An unexpected error has occurred.',
    'error');

  // Validation
  yield takeLatestValidation('VALIDATE_INPUT_USER', api.validateUserInput);
  yield takeLatestValidation('VALIDATE_INPUT_PASSWORD', api.validatePasswordInput);
}
