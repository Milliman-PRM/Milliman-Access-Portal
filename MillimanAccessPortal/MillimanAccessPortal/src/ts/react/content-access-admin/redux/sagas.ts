import { toastr } from 'react-redux-toastr';
import { all, call, Effect, put, select, takeEvery, takeLatest } from 'redux-saga/effects';

import { ClientWithEligibleUsers, RootContentItemWithStats } from '../../models';
import * as AccessActionCreators from './action-creators';
import { createErrorActionCreator, createResponseActionCreator } from './action-creators';
import * as AccessActions from './actions';
import {
    AccessAction, ErrorAction, isScheduleAction, RequestAction, ResponseAction,
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
function* takeLatestRequest<TRequest extends RequestAction>(
  type: TRequest['type'],
  apiCall: (request: TRequest['request']) => Promise<ResponseAction['response']>,
) {
  yield takeLatest(type, requestSaga, apiCall);
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
  yield put(yield nextActionCreator());
}
function* takeLatestSchedule<TAction extends AccessAction, TNext extends AccessAction>(
  type: TAction['type'] | ((type: TAction) => boolean),
  nextActionCreator: () => (TNext | IterableIterator<Effect | AccessAction>),
) {
  yield takeLatest(type, scheduleSaga, nextActionCreator);
}

// Toast triggers
function* toastSaga(
  message: string | ((response: ResponseAction['response']) => string),
  level: 'error' | 'info' | 'message' | 'success' | 'warning',
  action: ResponseAction,
) {
  yield toastr[level]('', typeof message === 'string' ? message : message(action.response));
}
function* takeEveryToast<TAction extends AccessAction>(
  type: TAction['type'] | Array<TAction['type']> | ((type: TAction) => boolean),
  message: string | (TAction extends ResponseAction
    ? (response: TAction['response']) => string
    : TAction extends ErrorAction
      ? (error: TAction['error']) => string
      : never),
  level: 'error' | 'info' | 'message' | 'success' | 'warning' = 'success',
) {
  yield takeEvery(type, toastSaga, message, level);
}

export default function* rootSaga() {
  // API requests
  yield all([
    takeLatestRequest('FETCH_CLIENTS', api.fetchClients),
    takeLatestRequest('FETCH_ITEMS', api.fetchItems),
    takeLatestRequest('FETCH_GROUPS', api.fetchGroups),
    takeLatestRequest('FETCH_SELECTIONS', api.fetchSelections),
    takeLatestRequest('FETCH_STATUS_REFRESH', api.fetchStatusRefresh),
    takeLatestRequest('FETCH_SESSION_CHECK', api.fetchSessionCheck),
    takeLatestRequest('CREATE_GROUP', api.createGroup),
    takeLatestRequest('UPDATE_GROUP', api.updateGroup),
    takeLatestRequest('DELETE_GROUP', api.deleteGroup),
    takeLatestRequest('SUSPEND_GROUP', api.suspendGroup),
    takeLatestRequest('UPDATE_SELECTIONS', api.updateSelections),
    takeLatestRequest('CANCEL_REDUCTION', api.cancelReduction),
  ]);
  yield all([
    takeLatestSchedule('SCHEDULE_STATUS_REFRESH', function*() {
      const client: ClientWithEligibleUsers = yield select(selectedClient);
      const item: RootContentItemWithStats = yield select(selectedItem);
      yield client
        ? AccessActionCreators.fetchStatusRefresh({
          clientId: client.id,
          contentItemId: item && item.id,
        })
        : AccessActionCreators.scheduleStatusRefresh({ delay: 45000 });
    }),
    takeLatestSchedule(
      (action) => action.type.match(/^FETCH_STATUS_REFRESH_/).length > 0,
      () => AccessActionCreators.scheduleStatusRefresh({ delay: 45000 })),
    takeLatestSchedule('SCHEDULE_SESSION_CHECK', () => AccessActionCreators.fetchSessionCheck({})),
    takeLatestSchedule('FETCH_SESSION_CHECK_SUCCEEDED',
      () => AccessActionCreators.scheduleSessionCheck({ delay: 60000 })),
    takeLatest('FETCH_SESSION_CHECK_FAILED', function*() { yield window.location.reload(); }),
  ]);

  yield all([
    takeEveryToast('CREATE_GROUP_SUCCEEDED', 'Selection group created.'),
    takeEveryToast('DELETE_GROUP_SUCCEEDED', 'Selection group deleted.'),
    takeEveryToast('UPDATE_GROUP_SUCCEEDED', 'Selection group updated.'),
    takeEveryToast<AccessActions.SuspendGroupSucceeded>
      ('SUSPEND_GROUP_SUCCEEDED', ({ isSuspended }) =>
        `Selection group ${isSuspended ? '' : 'un'}suspended.`),
    takeEveryToast<AccessActions.UpdateSelectionsSucceeded>
      ('UPDATE_SELECTIONS_SUCCEEDED', ({ reduction, group }) =>
        reduction && reduction.taskStatus === 10
          ? 'Reduction queued.'
          : group && group.isMaster
            ? 'Unrestricted access granted.'
            : 'Group inactivated.'),
    takeEveryToast('CANCEL_REDUCTION_SUCCEEDED', 'Reduction canceled.'),
  ]);

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
  ], ({ message }) => isNaN(message)
    ? message
    : message === '401'
      ? 'Your session has expired. Please refresh the page.'
      : 'An unexpected error has occured.',
    'error');
}
