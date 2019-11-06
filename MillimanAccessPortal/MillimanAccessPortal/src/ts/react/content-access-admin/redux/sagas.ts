import { select, takeLatest } from 'redux-saga/effects';

import { ClientWithEligibleUsers, RootContentItemWithStats } from '../../models';
import {
    createTakeEveryToast, createTakeLatestRequest, createTakeLatestSchedule,
} from '../../shared-components/redux/sagas';
import * as AccessActionCreators from './action-creators';
import * as AccessActions from './actions';
import {
    AccessAction, ErrorAccessAction, RequestAccessAction, ResponseAccessAction,
} from './actions';
import * as api from './api';
import { remainingStatusRefreshAttempts, selectedClient, selectedItem } from './selectors';

/**
 * Custom effect for handling request actions.
 * @param type Action type
 * @param apiCall API method to invoke
 */
const takeLatestRequest = createTakeLatestRequest<RequestAccessAction, ResponseAccessAction>();

/**
 * Custom effect for handling schedule actions.
 * @param type action type
 * @param nextActionCreator action creator to invoke after the scheduled duration
 */
const takeLatestSchedule = createTakeLatestSchedule<AccessAction>();

/**
 * Custom effect for handling actions that result in toasts.
 * @param type action type
 * @param message message to display, or a function that builds the message from a response
 * @param level message severity
 */
const takeEveryToast = createTakeEveryToast<AccessAction, ResponseAccessAction>();

/**
 * Register all sagas for the page.
 */
export default function* rootSaga() {
  // API requests
  yield takeLatestRequest('FETCH_GLOBAL_DATA', api.fetchGlobalData);
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
  yield takeEveryToast('CREATE_GROUP_SUCCEEDED', 'Selection group created.');
  yield takeEveryToast('DELETE_GROUP_SUCCEEDED', 'Selection group deleted.');
  yield takeEveryToast('UPDATE_GROUP_SUCCEEDED', 'Selection group updated.');
  yield takeEveryToast<AccessActions.SuspendGroupSucceeded>
    ('SUSPEND_GROUP_SUCCEEDED', ({ isSuspended }) =>
      `Selection group ${isSuspended ? '' : 'un'}suspended.`);
  yield takeEveryToast<AccessActions.UpdateSelectionsSucceeded>
    ('UPDATE_SELECTIONS_SUCCEEDED', ({ reduction, group }) =>
      reduction && reduction.taskStatus === 9
        ? 'Reduction submitted.'
        : group && group.isMaster
          ? 'Unrestricted access granted.'
          : 'Group inactivated.');
  yield takeEveryToast('CANCEL_REDUCTION_SUCCEEDED', 'Reduction canceled.');
  yield takeEveryToast('PROMPT_GROUP_EDITING',
    'Please finish editing the current selection group before performing this action.', 'warning');
  yield takeEveryToast('PROMPT_GROUP_NAME_EMPTY',
    'Please name the selection group before saving changes.', 'warning');
  yield takeEveryToast('PROMPT_STATUS_REFRESH_STOPPED',
    'Please refresh the page to update reduction status.', 'warning');
  yield takeEveryToast<ErrorAccessAction>([
    'FETCH_GLOBAL_DATA_FAILED',
    'FETCH_CLIENTS_FAILED',
    'FETCH_ITEMS_FAILED',
    'FETCH_GROUPS_FAILED',
    'FETCH_SELECTIONS_FAILED',
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
        : 'An unexpected error has occurred.',
    'error');
}
