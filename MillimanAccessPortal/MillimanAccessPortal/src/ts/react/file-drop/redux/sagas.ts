import { put, select, takeLatest } from 'redux-saga/effects';

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
  yield takeLatestRequest('FETCH_CLIENTS', API.fetchClients);
  yield takeLatestRequest('FETCH_FILE_DROPS', API.fetchFileDrops);
  yield takeLatestRequest('CREATE_FILE_DROP', API.createFileDrop);
  yield takeLatestRequest('DELETE_FILE_DROP', API.deleteFileDrop);
  yield takeLatestRequest('UPDATE_FILE_DROP', API.updateFileDrop);
  yield takeLatestRequest('FETCH_PERMISSION_GROUPS', API.fetchPermissionGroups);
  yield takeLatestRequest('UPDATE_PERMISSION_GROUPS', API.updatePermissionGroups);
  yield takeLatestRequest('FETCH_ACTIVITY_LOG', API.fetchActivityLog);
  yield takeLatestRequest('FETCH_SETTINGS', API.fetchSettings);
  yield takeLatestRequest('GENERATE_NEW_SFTP_PASSWORD', API.generateNewSftpPassword);
  yield takeLatestRequest('SET_FILE_DROP_NOTIFICATION_SETTING', API.setFileDropNotificationSetting);
  yield takeLatestRequest('FETCH_FOLDER_CONTENTS', API.fetchFolderContents);
  yield takeLatestRequest('DELETE_FILE_DROP_FILE', API.deleteFileDropFile);
  yield takeLatestRequest('DELETE_FILE_DROP_FOLDER', API.deleteFileDropFolder);
  yield takeLatestRequest('UPDATE_FILE_DROP_FILE_DESCRIPTION', API.updateFileDropFileDescription);
  yield takeLatestRequest('UPDATE_FILE_DROP_FOLDER_DESCRIPTION', API.updateFileDropFolderDescription);

  // Refresh the File Drop Folder contents if the upload that just finished was in the active File Drop folder
  yield takeLatest('FINALIZE_FILE_DROP_UPLOAD', function*(action: Action.FinalizeFileDropUpload) {
    const activeFileDropFolder = yield select(Selector.activeSelectedFileDropFolder);
    if (action.folderId === activeFileDropFolder) {
      yield put(ActionCreator.fetchFolderContents({
        canonicalPath: action.canonicalPath,
        fileDropId: action.fileDropId,
      }));
    }
  });

  // Session and Status Checks
  // yield takeLatestRequest('FETCH_STATUS_REFRESH', API.fetchStatusRefresh);
  yield takeLatestRequest('FETCH_SESSION_CHECK', API.fetchSessionCheck);
  // yield takeLatestSchedule('SCHEDULE_STATUS_REFRESH', function*() {
  // TO DO: implement status endpoint
  //   const client: ClientWithEligibleUsers = yield select(Selector.selectedClient);
  //   return client
  //     ? ActionCreator.fetchStatusRefresh({
  //       clientId: client.id,
  //     })
  //     : ActionCreator.scheduleStatusRefresh({ delay: 5000 });
  // });
  // yield takeLatestSchedule('FETCH_STATUS_REFRESH_SUCCEEDED',
  //   () => ActionCreator.scheduleStatusRefresh({ delay: 5000 }));
  // yield takeLatestSchedule('FETCH_STATUS_REFRESH_FAILED',
  //   () => ActionCreator.decrementStatusRefreshAttempts({}));
  // yield takeLatestSchedule('DECREMENT_STATUS_REFRESH_ATTEMPTS', function*() {
  //   const retriesLeft: number = yield select(Selector.remainingStatusRefreshAttempts);
  //   return retriesLeft
  //     ? ActionCreator.scheduleStatusRefresh({ delay: 5000 })
  //     : ActionCreator.promptStatusRefreshStopped({});
  // });
  yield takeLatestSchedule('SCHEDULE_SESSION_CHECK', () => ActionCreator.fetchSessionCheck({}));
  yield takeLatestSchedule('FETCH_SESSION_CHECK_SUCCEEDED',
    () => ActionCreator.scheduleSessionCheck({ delay: 60000 }));
  yield takeLatest('FETCH_SESSION_CHECK_FAILED', function*() { yield window.location.reload(); });

  // Toasts (Success)  // Toasts
  yield takeEveryToast('CREATE_FILE_DROP_SUCCEEDED', 'New File Drop created successfully.');
  yield takeEveryToast('UPDATE_FILE_DROP_SUCCEEDED', 'File Drop updated successfully.');
  yield takeEveryToast('DELETE_FILE_DROP_SUCCEEDED', 'File Drop successfully deleted.');
  yield takeEveryToast('DELETE_FILE_DROP_FILE_SUCCEEDED', 'File successfully deleted.');
  yield takeEveryToast('DELETE_FILE_DROP_FOLDER_SUCCEEDED', 'Folder successfully deleted.');
  yield takeEveryToast('UPDATE_FILE_DROP_FILE_DESCRIPTION_SUCCEEDED', 'File updated successfully.');
  yield takeEveryToast('UPDATE_FILE_DROP_FOLDER_DESCRIPTION_SUCCEEDED', 'Folder updated successfully.');

  // Toasts (Errors/Warnings)
  yield takeEveryToast('PROMPT_STATUS_REFRESH_STOPPED',
    'Please refresh the page to update Client status.', 'warning');
  yield takeEveryToast<Action.FileDropErrorActions>([
    'FETCH_CLIENTS_FAILED',
    'FETCH_FILE_DROPS_FAILED',
    'CREATE_FILE_DROP_FAILED',
    'DELETE_FILE_DROP_FAILED',
    'UPDATE_FILE_DROP_FAILED',
    'FETCH_PERMISSION_GROUPS_FAILED',
    'UPDATE_PERMISSION_GROUPS_FAILED',
    'FETCH_SESSION_CHECK_FAILED',
    'FETCH_STATUS_REFRESH_FAILED',
    'FETCH_ACTIVITY_LOG_FAILED',
    'FETCH_SETTINGS_FAILED',
    'GENERATE_NEW_SFTP_PASSWORD_FAILED',
    'SET_FILE_DROP_NOTIFICATION_SETTING_FAILED',
    'FETCH_FOLDER_CONTENTS_FAILED',
    'DELETE_FILE_DROP_FILE_FAILED',
    'DELETE_FILE_DROP_FOLDER_FAILED',
    'UPDATE_FILE_DROP_FILE_DESCRIPTION_FAILED',
    'UPDATE_FILE_DROP_FOLDER_DESCRIPTION_FAILED',
  ], ({ message }) => message === 'sessionExpired'
    ? 'Your session has expired. Please refresh the page.'
    : isNaN(message)
      ? message
      : 'An unexpected error has occurred.',
    'error');
}
