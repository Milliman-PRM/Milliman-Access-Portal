import * as UploadActionCreators from '../../../upload/Redux/action-creators';
import {
    createActionCreator, createRequestActionCreator,
} from '../../shared-components/redux/action-creators';
import * as PublishActions from './actions';

export const selectClient =
  createActionCreator<PublishActions.SelectClient>('SELECT_CLIENT');
export const selectItem =
  createActionCreator<PublishActions.SelectItem>('SELECT_ITEM');
export const setContentItemFormState =
  createActionCreator<PublishActions.SetContentItemFormState>('SET_CONTENT_ITEM_FORM_STATE');
export const setFormForNewContentItem =
  createActionCreator<PublishActions.SetFormForNewContentItem>('SET_FORM_FOR_NEW_CONTENT_ITEM');

export const setFilterTextClient =
  createActionCreator<PublishActions.SetFilterTextClient>('SET_FILTER_TEXT_CLIENT');
export const setFilterTextItem =
  createActionCreator<PublishActions.SetFilterTextItem>('SET_FILTER_TEXT_ITEM');
export const toggleShowOnlyChanges =
  createActionCreator<PublishActions.ToggleShowOnlyChanges>('TOGGLE_SHOW_ONLY_CHANGES');
export const toggleGoLiveConfirmationCheckbox =
  createActionCreator<PublishActions.ToggleGoLiveConfirmationCheckbox>(
    'TOGGLE_GO_LIVE_CONFIRMATION_CHECKBOX',
  );

export const setPublishingFormTextInputValue =
  createActionCreator<PublishActions.SetPublishingFormTextInputValue>('SET_PENDING_TEXT_INPUT_VALUE');
export const setPublishingFormBooleanInputValue =
  createActionCreator<PublishActions.SetPublishingFormBooleanInputValue>('SET_PENDING_BOOLEAN_INPUT_VALUE');
export const resetContentItemForm =
  createActionCreator<PublishActions.ResetContentItemForm>('RESET_CONTENT_ITEM_FORM');

export const promptStatusRefreshStopped =
  createActionCreator<PublishActions.PromptStatusRefreshStopped>('PROMPT_STATUS_REFRESH_STOPPED');

// Data fetches
export const fetchGlobalData =
  createRequestActionCreator<PublishActions.FetchGlobalData>('FETCH_GLOBAL_DATA');
export const fetchClients =
  createRequestActionCreator<PublishActions.FetchClients>('FETCH_CLIENTS');
export const fetchItems =
  createRequestActionCreator<PublishActions.FetchItems>('FETCH_ITEMS');
export const fetchContentItemDetail =
  createRequestActionCreator<PublishActions.FetchContentItemDetail>('FETCH_CONTENT_ITEM_DETAIL');
export const fetchGoLiveSummary =
  createRequestActionCreator<PublishActions.FetchGoLiveSummary>('FETCH_GO_LIVE_SUMMARY');
export const fetchStatusRefresh =
  createRequestActionCreator<PublishActions.FetchStatusRefresh>('FETCH_STATUS_REFRESH');
export const fetchSessionCheck =
  createRequestActionCreator<PublishActions.FetchSessionCheck>('FETCH_SESSION_CHECK');

// Updates
export const createNewContentItem =
  createRequestActionCreator<PublishActions.CreateNewContentItem>('CREATE_NEW_CONTENT_ITEM');
export const updateContentItem =
  createRequestActionCreator<PublishActions.UpdateContentItem>('UPDATE_CONTENT_ITEM');
export const publishContentFiles =
  createRequestActionCreator<PublishActions.PublishContentFiles>('PUBLISH_CONTENT_FILES');
export const deleteContentItem =
  createRequestActionCreator<PublishActions.DeleteContentItem>('DELETE_CONTENT_ITEM');
export const cancelPublicationRequest =
  createRequestActionCreator<PublishActions.CancelPublicationRequest>('CANCEL_PUBLICATION_REQUEST');
export const approveGoLiveSummary =
  createRequestActionCreator<PublishActions.ApproveGoLiveSummary>('APPROVE_GO_LIVE_SUMMARY');
export const rejectGoLiveSummary =
  createRequestActionCreator<PublishActions.RejectGoLiveSummary>('REJECT_GO_LIVE_SUMMARY');

// Modal actions
export const openDeleteContentItemModal =
  createActionCreator<PublishActions.OpenDeleteContentItemModal>('OPEN_DELETE_CONTENT_ITEM_MODAL');
export const closeDeleteContentItemModal =
  createActionCreator<PublishActions.CloseDeleteContentItemModal>('CLOSE_DELETE_CONTENT_ITEM_MODAL');

/**
 * Close the modal used to confirm selection group deletion.
 */
export interface CloseDeleteGroupModal {
  type: 'CLOSE_DELETE_GROUP_MODAL';
}

// Scheduled actions
export const scheduleStatusRefresh =
  createActionCreator<PublishActions.ScheduleStatusRefresh>('SCHEDULE_STATUS_REFRESH');
export const decrementStatusRefreshAttempts =
  createActionCreator<PublishActions.DecrementStatusRefreshAttempts>('DECREMENT_STATUS_REFRESH_ATTEMPTS');
export const scheduleSessionCheck =
  createActionCreator<PublishActions.ScheduleSessionCheck>('SCHEDULE_SESSION_CHECK');

// Upload Action Creators
export const beginFileUpload = UploadActionCreators.beginFileUpload;
export const updateChecksumProgress = UploadActionCreators.updateChecksumProgress;
export const updateUploadProgress = UploadActionCreators.updateUploadProgress;
export const setUploadCancelable = UploadActionCreators.setUploadCancelable;
export const setUploadError = UploadActionCreators.setUploadError;
export const cancelFileUpload = UploadActionCreators.cancelFileUpload;
export const finalizeUpload = UploadActionCreators.finalizeUpload;
