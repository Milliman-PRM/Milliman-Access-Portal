import { call, put, select, takeLatest } from 'redux-saga/effects';

import { ClientWithEligibleUsers } from '../../models';
import {
  createErrorActionCreator, createRequestActionCreator, createResponseActionCreator,
} from '../../shared-components/redux/action-creators';
import { ErrorAction } from '../../shared-components/redux/actions';
import {
  createTakeEveryToast, createTakeLatestRequest, createTakeLatestSchedule,
} from '../../shared-components/redux/sagas';
import * as ContentPublishingActionCreators from './action-creators';
import * as ContentPublishingActions from './actions';
import {
  ErrorPublishingAction, PublishingAction, RequestPublishingAction, ResponsePublishingAction,
} from './actions';
import * as api from './api';
import { filesForPublishing, remainingStatusRefreshAttempts, selectedClient } from './selectors';

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
  yield takeLatestRequest('FETCH_CONTENT_ITEM_DETAIL', api.fetchContentItemDetail);
  yield takeLatestRequest('FETCH_GO_LIVE_SUMMARY', api.fetchGoLiveSummary);
  yield takeLatestRequest('APPROVE_GO_LIVE_SUMMARY', api.approveGoLiveSummary);
  yield takeLatestRequest('REJECT_GO_LIVE_SUMMARY', api.rejectGoLiveSummary);
  yield takeLatest('CREATE_NEW_CONTENT_ITEM', createNewContentItem);
  yield takeLatestRequest('UPDATE_CONTENT_ITEM', api.updateContentItem);
  yield takeLatestRequest('PUBLISH_CONTENT_FILES', api.publishContentFiles);
  yield takeLatestRequest('DELETE_CONTENT_ITEM', api.deleteContentItem);
  yield takeLatestRequest('CANCEL_PUBLICATION_REQUEST', api.cancelPublicationRequest);

  yield takeLatestRequest('FETCH_STATUS_REFRESH', api.fetchStatusRefresh);
  yield takeLatestRequest('FETCH_SESSION_CHECK', api.fetchSessionCheck);

  // Scheduled actions
  yield takeLatestSchedule('SCHEDULE_STATUS_REFRESH', function*() {
    const client: ClientWithEligibleUsers = yield select(selectedClient);
    return client
      ? ContentPublishingActionCreators.fetchStatusRefresh({
        clientId: client.id,
      })
      : ContentPublishingActionCreators.scheduleStatusRefresh({ delay: 5000 });
  });
  yield takeLatestSchedule('PUBLISH_CONTENT_FILES_SUCCEEDED', function*() {
    const client: ClientWithEligibleUsers = yield select(selectedClient);
    if (client.id) {
      ContentPublishingActionCreators.fetchStatusRefresh({
        clientId: client.id,
      });
    }
  });
  yield takeLatestSchedule('FETCH_STATUS_REFRESH_SUCCEEDED',
    () => ContentPublishingActionCreators.scheduleStatusRefresh({ delay: 5000 }));
  yield takeLatestSchedule('FETCH_STATUS_REFRESH_FAILED',
    () => ContentPublishingActionCreators.decrementStatusRefreshAttempts({}));
  yield takeLatestSchedule('DECREMENT_STATUS_REFRESH_ATTEMPTS', function*() {
    const retriesLeft: number = yield select(remainingStatusRefreshAttempts);
    return retriesLeft
      ? ContentPublishingActionCreators.scheduleStatusRefresh({ delay: 5000 })
      : ContentPublishingActionCreators.promptStatusRefreshStopped({});
  });
  yield takeLatestSchedule('SCHEDULE_SESSION_CHECK', () => ContentPublishingActionCreators.fetchSessionCheck({}));
  yield takeLatestSchedule('FETCH_SESSION_CHECK_SUCCEEDED',
    () => ContentPublishingActionCreators.scheduleSessionCheck({ delay: 60000 }));
  yield takeLatest('FETCH_SESSION_CHECK_FAILED', function*() { yield window.location.reload(); });

  // Toasts
  yield takeEveryToast('CREATE_NEW_CONTENT_ITEM_SUCCEEDED', 'New Content Item created successfully.');
  yield takeEveryToast('UPDATE_CONTENT_ITEM_SUCCEEDED', 'Content Item updated successfully.');
  yield takeEveryToast('PUBLISH_CONTENT_FILES_SUCCEEDED', 'Files successfully uploaded for processing.');
  yield takeEveryToast('DELETE_CONTENT_ITEM_SUCCEEDED', 'Content Item successfully deleted.');
  yield takeEveryToast('CANCEL_PUBLICATION_REQUEST_SUCCEEDED', 'Publication canceled.');
  yield takeEveryToast('REJECT_GO_LIVE_SUMMARY_SUCCEEDED', 'Publication rejected.');
  yield takeEveryToast('APPROVE_GO_LIVE_SUMMARY_SUCCEEDED', 'Publication approved.');
  yield takeEveryToast('PROMPT_STATUS_REFRESH_STOPPED',
    'Please refresh the page to update reduction status.', 'warning');
  yield takeEveryToast<ErrorPublishingAction>([
    'FETCH_GLOBAL_DATA_FAILED',
    'FETCH_CLIENTS_FAILED',
    'FETCH_ITEMS_FAILED',
    'FETCH_CONTENT_ITEM_DETAIL_FAILED',
    'FETCH_SESSION_CHECK_FAILED',
    'FETCH_STATUS_REFRESH_FAILED',
    'FETCH_GO_LIVE_SUMMARY_FAILED',
    'APPROVE_GO_LIVE_SUMMARY_FAILED',
    'REJECT_GO_LIVE_SUMMARY_FAILED',
    'CREATE_NEW_CONTENT_ITEM_FAILED',
    'UPDATE_CONTENT_ITEM_FAILED',
    'PUBLISH_CONTENT_FILES_FAILED',
    'DELETE_CONTENT_ITEM_FAILED',
    'CANCEL_PUBLICATION_REQUEST_FAILED',
  ], ({ message }) => message === 'sessionExpired'
    ? 'Your session has expired. Please refresh the page.'
    : isNaN(message)
      ? message
      : 'An unexpected error has occurred.',
    'error');
}

function* createNewContentItem(action: ContentPublishingActions.CreateNewContentItem) {
  /**
   * Make an asynchronous call to create a new root content item and then publish files
   * @param apiCall API method to invoke
   * @param action the request action that caused this saga to fire
   */
  try {
    const newContentItem = yield call(api.createNewContentItem, action.request);
    yield put(
      createResponseActionCreator(
        'CREATE_NEW_CONTENT_ITEM_SUCCEEDED' as ContentPublishingActions.CreateNewContentItem['type'],
      )(newContentItem),
    );
    try {
      const publishingPayload = yield select(filesForPublishing, newContentItem.detail.id);
      yield put(
        createRequestActionCreator(
          'PUBLISH_CONTENT_FILES' as ContentPublishingActions.PublishContentFiles['type'],
        )(publishingPayload),
      );
    } catch (error) {
      yield put(createErrorActionCreator('PUBLISH_CONTENT_FILES_FAILED' as ErrorAction['type'])(error));
    }
  } catch (error) {
    yield put(createErrorActionCreator('CREATE_NEW_CONTENT_ITEM_FAILED' as ErrorAction['type'])(error));
  }
}
