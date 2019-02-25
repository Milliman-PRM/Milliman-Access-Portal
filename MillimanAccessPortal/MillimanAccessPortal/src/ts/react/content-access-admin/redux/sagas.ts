import { toastr } from 'react-redux-toastr';
import { all, call, Effect, put, select, takeEvery, takeLatest } from 'redux-saga/effects';

import { ClientWithEligibleUsers, RootContentItemWithStats } from '../../models';
import * as AccessActionCreators from './action-creators';
import { createErrorActionCreator, createResponseActionCreator } from './action-creators';
import * as AccessActions from './actions';
import {
    AccessAction, ErrorAction, isErrorAction, isScheduleAction, RequestAction, ResponseAction,
} from './actions';
import * as api from './api';
import { selectedClient, selectedItem } from './selectors';

// API requests
function* requestSaga(
  apiCall: (request: RequestAction['request']) => ResponseAction['response'], action: RequestAction) {
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
  nextActionCreator: () => (AccessAction | IterableIterator<Effect | AccessAction>),
  action: AccessAction,
) {
  if (isScheduleAction(action)) {
    yield call(sleep, action.delay);
  }
  const nextAction = yield nextActionCreator();
  if (nextAction) {
    yield put(nextAction);
  }
}
function takeLatestSchedule<TAction extends AccessAction, TNext extends AccessAction>(
  type: TAction['type'] | ((type: TAction) => boolean),
  nextActionCreator: () => (TNext | IterableIterator<Effect | AccessAction>),
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
function takeEveryToast<TAction extends AccessAction>(
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
  yield takeLatestRequest('FETCH_CLIENTS', api.fetchClients);
  yield takeLatestRequest('FETCH_ITEMS', api.fetchItems);
  yield takeLatestRequest('FETCH_GROUPS', api.fetchGroups);
  yield takeLatestRequest('FETCH_SELECTIONS', api.fetchSelections);
  yield takeLatestRequest('FETCH_STATUS_REFRESH', api.fetchStatusRefresh);
  yield takeLatestRequest('FETCH_SESSION_CHECK', api.fetchSessionCheck);
  yield takeLatestRequest('CREATE_GROUP', api.createGroup);
  yield takeLatestRequest('UPDATE_GROUP', api.updateGroup);
  yield takeLatestRequest('DELETE_GROUP', api.deleteGroup);
  yield takeLatestRequest('SUSPEND_GROUP', api.suspendGroup);
  yield takeLatestRequest('UPDATE_SELECTIONS', api.updateSelections);
  yield takeLatestRequest('CANCEL_REDUCTION', api.cancelReduction);

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
  yield takeLatestSchedule('SCHEDULE_SESSION_CHECK', () => AccessActionCreators.fetchSessionCheck({}));
  yield takeLatestSchedule('FETCH_SESSION_CHECK_SUCCEEDED',
    () => AccessActionCreators.scheduleSessionCheck({ delay: 60000 }));
  yield takeLatest('FETCH_SESSION_CHECK_FAILED', function*() { yield window.location.reload(); });

  // Toasts
  yield takeEveryToast('CREATE_GROUP_SUCCEEDED', 'Selection group created.');
  yield takeEveryToast('DELETE_GROUP_SUCCEEDED', 'Selection group deleted.');
  yield takeEveryToast('UPDATE_GROUP_SUCCEEDED', 'Selection group updated.');
  yield takeEveryToast<AccessActions.SuspendGroupSucceeded>
    ('SUSPEND_GROUP_SUCCEEDED', ({ isSuspended }) =>
      `Selection group ${isSuspended ? '' : 'un'}suspended.`);
  yield takeEveryToast<AccessActions.UpdateSelectionsSucceeded>
    ('UPDATE_SELECTIONS_SUCCEEDED', ({ reduction, group }) =>
      reduction && reduction.taskStatus === 10
        ? 'Reduction queued.'
        : group && group.isMaster
          ? 'Unrestricted access granted.'
          : 'Group inactivated.');
  yield takeEveryToast('CANCEL_REDUCTION_SUCCEEDED', 'Reduction canceled.');
  yield takeEveryToast('PROMPT_GROUP_EDITING',
    'Please finish editing the current selection group before performing this action.', 'warning');
  yield takeEveryToast<ErrorAction>([
    'FETCH_CLIENTS_FAILED',
    'FETCH_ITEMS_FAILED',
    'FETCH_GROUPS_FAILED',
    'FETCH_SELECTIONS_FAILED',
    'FETCH_STATUS_REFRESH_FAILED',
    'FETCH_SESSION_CHECK_FAILED',
    'CREATE_GROUP_FAILED',
    'UPDATE_GROUP_FAILED',
    'DELETE_GROUP_FAILED',
    'SUSPEND_GROUP_FAILED',
    'UPDATE_SELECTIONS_FAILED',
    'CANCEL_REDUCTION_FAILED',
  ], ({ message }) => message === 'sessionExpired'
      ? 'Your session has expired. Please refresh the page.'
      : isNaN(message)
        ? message
        : 'An unexpected error has occured.',
    'error');
}
