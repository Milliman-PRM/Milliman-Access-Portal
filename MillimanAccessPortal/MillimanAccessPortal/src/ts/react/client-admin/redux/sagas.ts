import { select } from 'redux-saga/effects';

import * as AccessActionCreators from './action-creators';
import {
  AccessAction, ErrorAccessAction, RequestAccessAction, ResponseAccessAction,
} from './actions';
import * as api from './api';
import { selectedClientId, userIsRemovingOwnClientAdminRole } from './selectors';

import {
  createTakeEveryToast, createTakeLatestRequest, createTakeLatestSchedule,
} from '../../shared-components/redux/sagas';

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
  yield takeLatestRequest('FETCH_CLIENTS', api.fetchClients);
  yield takeLatestRequest('FETCH_GLOBAL_DATA', api.fetchGlobalData);
  yield takeLatestRequest('FETCH_CLIENT_DETAILS', api.fetchClientDetails);
  yield takeLatestRequest('UPDATE_ALL_USER_ROLES_IN_CLIENT', api.updateAllUserRolesInClient);
  yield takeLatestRequest('SAVE_NEW_CLIENT', api.saveNewClient);
  yield takeLatestRequest('EDIT_CLIENT', api.editClient);
  yield takeLatestRequest('DELETE_CLIENT', api.deleteClient);
  yield takeLatestRequest('SAVE_NEW_CLIENT_USER', api.saveNewClientUser);
  yield takeLatestRequest('REMOVE_CLIENT_USER', api.removeClientUser);
  yield takeLatestRequest('REQUEST_REENABLE_USER_ACCOUNT', api.requestReenableUserAccount);

  // Scheduled actions
  yield takeLatestSchedule('SAVE_NEW_CLIENT_USER_SUCCEEDED', () => AccessActionCreators.fetchClients({}));
  yield takeLatestSchedule('REMOVE_CLIENT_USER_SUCCEEDED', () => AccessActionCreators.fetchClients({}));
  yield takeLatestSchedule('EDIT_CLIENT_SUCCEEDED', function*() {
    const selectedClient = yield select(selectedClientId);
    return AccessActionCreators.fetchClientDetails({ clientId: selectedClient });
  });
  yield takeLatestSchedule('UPDATE_ALL_USER_ROLES_IN_CLIENT_SUCCEEDED', function*() {
    if (userIsRemovingOwnClientAdminRole) {
      return AccessActionCreators.fetchClients({});
    }
  });

  // Toasts
  yield takeEveryToast('SAVE_NEW_CLIENT_SUCCEEDED', 'Created new client');
  yield takeEveryToast('EDIT_CLIENT_SUCCEEDED', 'Updated client');
  yield takeEveryToast('DELETE_CLIENT_SUCCEEDED', 'Deleted client');
  yield takeEveryToast('UPDATE_ALL_USER_ROLES_IN_CLIENT_SUCCEEDED', 'Roles updated');
  yield takeEveryToast('SAVE_NEW_CLIENT_USER_SUCCEEDED', 'User successfully added');
  yield takeEveryToast('REMOVE_CLIENT_USER_SUCCEEDED', 'User successfully removed');
  yield takeEveryToast('REQUEST_REENABLE_USER_ACCOUNT_SUCCEEDED', 'Request sent to support');

  yield takeEveryToast<ErrorAccessAction>([
    'FETCH_CLIENTS_FAILED',
    'FETCH_GLOBAL_DATA_FAILED',
    'FETCH_CLIENT_DETAILS_FAILED',
    'UPDATE_ALL_USER_ROLES_IN_CLIENT_FAILED',
    'SAVE_NEW_CLIENT_FAILED',
    'EDIT_CLIENT_FAILED',
    'DELETE_CLIENT_FAILED',
    'SAVE_NEW_CLIENT_USER_FAILED',
    'REMOVE_CLIENT_USER_FAILED',
    'REQUEST_REENABLE_USER_ACCOUNT_FAILED',
  ], ({ message }) => message === 'sessionExpired'
    ? 'Your session has expired. Please refresh the page.'
    : isNaN(message)
      ? message
      : 'An unexpected error has occurred.',
    'error');
}
