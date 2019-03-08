import { call, Effect, put, takeEvery, takeLatest } from 'redux-saga/effects';

import * as AccountActionCreators from './action-creators';
import { createErrorActionCreator, createResponseActionCreator } from './action-creators';
import {
    AccountAction, ErrorAction, isErrorAction, isScheduleAction, RequestAction, ResponseAction,
} from './actions';
import * as api from './api';

function* requestSaga<TRequest extends RequestAction>(
  apiCall: (request: RequestAction['request']) => ResponseAction['response'], action: TRequest) {
  try {
    const response = yield call(apiCall, action.request);
    yield put(createResponseActionCreator(`${action.type}_SUCCEEDED` as ResponseAction['type'])(response));
  } catch (error) {
    yield put(createErrorActionCreator(`${action.type}_FAILED` as ErrorAction['type'])(error));
  }
}
function takeLatestRequest<TRequest extends RequestAction>(
  type: TRequest['type'],
  apiCall: (request: TRequest['request']) => Promise<ResponseAction['response']>,
) {
  return takeLatest(type, requestSaga, apiCall);
}

// Scheduled actions
function sleep(duration: number) {
  return new Promise((resolve) => {
    setTimeout(resolve, duration);
  });
}
function* scheduleSaga(
  nextActionCreator: () => (AccountAction | IterableIterator<Effect | AccountAction>),
  action: AccountAction,
) {
  if (isScheduleAction(action)) {
    // yield call(sleep, action.delay);
  }
  const nextAction = yield nextActionCreator();
  if (nextAction) {
    yield put(nextAction);
  }
}
function takeLatestSchedule<TAction extends AccountAction, TNext extends AccountAction>(
  type: TAction['type'] | ((type: TAction) => boolean),
  nextActionCreator: () => (TNext | IterableIterator<Effect | AccountAction>),
) {
  return takeLatest(type, scheduleSaga, nextActionCreator);
}

// Toast triggers
function* toastSaga(
  message: string
    | ((response: ResponseAction['response'] | ErrorAction['error']) => string),
  level: 'error' | 'info' | 'message' | 'success' | 'warning',
  action: ResponseAction | ErrorAction,
) {
  yield toastr[level]('', typeof message === 'string'
    ? message
    : isErrorAction(action)
      ? message(action.error)
      : message(action.response));
}
function takeEveryToast<TAction extends AccountAction>(
  type: TAction['type'] | Array<TAction['type']> | ((type: TAction) => boolean),
  message: string | (TAction extends ResponseAction
    ? (response: TAction['response']) => string
    : TAction extends ErrorAction
      ? (error: TAction['error']) => string
      : never),
  level: 'error' | 'info' | 'message' | 'success' | 'warning' = 'success',
) {
  return takeEvery(type, toastSaga, message, level);
}

export default function* rootSaga() {
  // API requests
  yield takeLatestRequest('FETCH_USER', api.fetchUser);

  // Scheduled actions
  // yield takeLatestSchedule('SCHEDULE_SESSION_CHECK', () => AccessActionCreators.fetchSessionCheck({}));
  // yield takeLatestSchedule('FETCH_SESSION_CHECK_SUCCEEDED',
  //   () => AccessActionCreators.scheduleSessionCheck({ delay: 60000 }));
  // yield takeLatest('FETCH_SESSION_CHECK_FAILED', function*() { yield window.location.reload(); });

  // Toasts
  yield takeEveryToast<ErrorAction>([
    'FETCH_USER_FAILED',
  ], ({ message }) => message === 'sessionExpired'
      ? 'Your session has expired. Please refresh the page.'
      : isNaN(message)
        ? message
        : 'An unexpected error has occured.',
    'error');
}
