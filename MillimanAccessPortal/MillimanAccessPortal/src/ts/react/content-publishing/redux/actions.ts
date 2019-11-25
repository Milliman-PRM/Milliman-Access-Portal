import { PageUploadAction } from '../../../upload/Redux/actions';
import {
  PreLiveContentValidationSummary, PublishRequest, RootContentItemSummaryAndDetail,
} from '../../../view-models/content-publishing';
import {
  ClientWithStats, ContentAssociatedFileType, ContentItemDetail,
  ContentPublicationRequest, ContentType, GoLiveViewModel, Guid,
  PublicationQueueDetails, RootContentItem, RootContentItemWithStats,
} from '../../models';
import { TSError } from '../../shared-components/redux/actions';
import { Dict } from '../../shared-components/redux/store';
import { AfterFormModal } from './store';

// ~~ Page actions ~~

/**
 * Exclusively select the client card specified by id.
 * If id refers to the currently selected card, deselect it.
 */
export interface SelectClient {
  type: 'SELECT_CLIENT';
  id: Guid;
}

/**
 * Exclusively select the content item card specified by id.
 * If id refers to the currently selected card, deselect it.
 */
export interface SelectItem {
  type: 'SELECT_ITEM';
  id: Guid;
}

/**
 * Set the Content Item form read/write state
 * 'read' = read-only | 'write' = write
 */
export interface SetContentItemFormState {
  type: 'SET_CONTENT_ITEM_FORM_STATE';
  formState: 'read' | 'write';
}

/**
 * Setup an empty content item form for creating
 * a new Content Item
 */
export interface SetFormForNewContentItem {
  type: 'SET_FORM_FOR_NEW_CONTENT_ITEM';
  clientId: Guid;
}

/**
 * Set filter text for the client card filter.
 */
export interface SetFilterTextClient {
  type: 'SET_FILTER_TEXT_CLIENT';
  text: string;
}

/**
 * Set filter text for the content item card filter.
 */
export interface SetFilterTextItem {
  type: 'SET_FILTER_TEXT_ITEM';
  text: string;
}

/*
 * Toggle confirmation checkboxes in Go-Live Summary
 */
export interface ToggleGoLiveConfirmationCheckbox {
  type: 'TOGGLE_GO_LIVE_CONFIRMATION_CHECKBOX';
  target: 'masterContent'
  | 'thumbnail'
  | 'releaseNotes'
  | 'userguide'
  | 'hierarchyChanges'
  | 'selectionGroups';
  status: boolean;
}

/**
 * Set filter text for the content item card filter.
 */
export interface ToggleShowOnlyChanges {
  type: 'TOGGLE_SHOW_ONLY_CHANGES';
}

/**
 * Display a toast indicating that the status refresh polling has stopped
 */
export interface PromptStatusRefreshStopped {
  type: 'PROMPT_STATUS_REFRESH_STOPPED';
}

// ~~ Server actions ~~

/**
 * GET:
 *   Information on Content Types and Associated Content Types.
 */
export interface FetchGlobalData {
  type: 'FETCH_GLOBAL_DATA';
  request: {};
}
export interface FetchGlobalDataSucceeded {
  type: 'FETCH_GLOBAL_DATA_SUCCEEDED';
  response: {
    contentTypes: Dict<ContentType>;
    contentAssociatedFileTypes: Dict<ContentAssociatedFileType>;
  };
}
export interface FetchGlobalDataFailed {
  type: 'FETCH_GLOBAL_DATA_FAILED';
  error: TSError;
}

/**
 * GET:
 *   clients the current user has access to publish for;
 *   users who are content eligible in any of those clients.
 */
export interface FetchClients {
  type: 'FETCH_CLIENTS';
  request: {};
}
export interface FetchClientsSucceeded {
  type: 'FETCH_CLIENTS_SUCCEEDED';
  response: {
    clients: Dict<ClientWithStats>;
  };
}
export interface FetchClientsFailed {
  type: 'FETCH_CLIENTS_FAILED';
  error: TSError;
}

/**
 * GET:
 *   content items in the selected client;
 *   content types belonging to those content items;
 *   publications for those content items;
 *   publication queue information for those publications;
 *   updated card stats for the selected client.
 */
export interface FetchItems {
  type: 'FETCH_ITEMS';
  request: {
    clientId: Guid;
  };
}
export interface FetchItemsSucceeded {
  type: 'FETCH_ITEMS_SUCCEEDED';
  response: {
    clientStats: ClientWithStats;
    contentItems: Dict<RootContentItemWithStats>;
    publications: Dict<ContentPublicationRequest>;
    publicationQueue: Dict<PublicationQueueDetails>;
  };
}
export interface FetchItemsFailed {
  type: 'FETCH_ITEMS_FAILED';
  error: TSError;
}

/**
 * GET:
 *   content item detail;
 */
export interface FetchContentItemDetail {
  type: 'FETCH_CONTENT_ITEM_DETAIL';
  request: {
    rootContentItemId: Guid;
  };
}
export interface FetchContentItemDetailSucceeded {
  type: 'FETCH_CONTENT_ITEM_DETAIL_SUCCEEDED';
  response: ContentItemDetail;
}
export interface FetchContentItemDetailFailed {
  type: 'FETCH_CONTENT_ITEM_DETAIL_FAILED';
  error: TSError;
}

/**
 * GET:
 *   Content Item Go Live Summary data;
 */
export interface FetchGoLiveSummary {
  type: 'FETCH_GO_LIVE_SUMMARY';
  request: {
    rootContentItemId: Guid;
  };
}
export interface FetchGoLiveSummarySucceeded {
  type: 'FETCH_GO_LIVE_SUMMARY_SUCCEEDED';
  response: PreLiveContentValidationSummary;
}
export interface FetchGoLiveSummaryFailed {
  type: 'FETCH_GO_LIVE_SUMMARY_FAILED';
  error: TSError;
}

/**
 * POST:
 *   Approve the Go-Live Summary;
 */
export interface ApproveGoLiveSummary {
  type: 'APPROVE_GO_LIVE_SUMMARY';
  request: GoLiveViewModel;
}
export interface ApproveGoLiveSummarySucceeded {
  type: 'APPROVE_GO_LIVE_SUMMARY_SUCCEEDED';
  response: {
    publicationRequestId: Guid;
  };
}
export interface ApproveGoLiveSummaryFailed {
  type: 'APPROVE_GO_LIVE_SUMMARY_FAILED';
  error: TSError;
}

/**
 * POST:
 *   Reject the Go-Live Summary;
 */
export interface RejectGoLiveSummary {
  type: 'REJECT_GO_LIVE_SUMMARY';
  request: GoLiveViewModel;
}
export interface RejectGoLiveSummarySucceeded {
  type: 'REJECT_GO_LIVE_SUMMARY_SUCCEEDED';
  response: {
    publicationRequestId: Guid;
  };
}
export interface RejectGoLiveSummaryFailed {
  type: 'REJECT_GO_LIVE_SUMMARY_FAILED';
  error: TSError;
}

/**
 * POST:
 *   Create a new Content Item;
 */
export interface CreateNewContentItem {
  type: 'CREATE_NEW_CONTENT_ITEM';
  request: {
    ClientId: Guid;
    ContentName: string;
    ContentTypeId: Guid;
    Description: string;
    Notes: string;
    ContentDisclaimer: string;
    DoesReduce: boolean;
    // PowerBi specific:
    FilterPaneEnabled?: boolean;
    NavigationPaneEnabled?: boolean;
    BookmarksPaneEnabled?: boolean;
  };
}
export interface CreateNewContentItemSucceeded {
  type: 'CREATE_NEW_CONTENT_ITEM_SUCCEEDED';
  response: RootContentItemSummaryAndDetail;
}
export interface CreateNewContentItemFailed {
  type: 'CREATE_NEW_CONTENT_ITEM_FAILED';
  error: TSError;
}

/**
 * POST:
 *   Update a Content Item;
 */
export interface UpdateContentItem {
  type: 'UPDATE_CONTENT_ITEM';
  request: {
    Id: Guid;
    ClientId: Guid;
    ContentName: string;
    ContentTypeId: Guid;
    Description: string;
    Notes: string;
    ContentDisclaimer: string;
    DoesReduce: boolean;
    // PowerBi specific:
    FilterPaneEnabled?: boolean;
    NavigationPaneEnabled?: boolean;
    BookmarksPaneEnabled?: boolean;
  };
}
export interface UpdateContentItemSucceeded {
  type: 'UPDATE_CONTENT_ITEM_SUCCEEDED';
  response: RootContentItemSummaryAndDetail;
}
export interface UpdateContentItemFailed {
  type: 'UPDATE_CONTENT_ITEM_FAILED';
  error: TSError;
}

/**
 * POST:
 *   Publish content files to Content Item;
 */
export interface PublishContentFiles {
  type: 'PUBLISH_CONTENT_FILES';
  request: PublishRequest;
}
export interface PublishContentFilesSucceeded {
  type: 'PUBLISH_CONTENT_FILES_SUCCEEDED';
  response: ContentItemDetail;
}
export interface PublishContentFilesFailed {
  type: 'PUBLISH_CONTENT_FILES_FAILED';
  error: TSError;
}

/**
 * DELETE:
 *   Delete a Content Item;
 */
export interface DeleteContentItem {
  type: 'DELETE_CONTENT_ITEM';
  request: Guid;
}
export interface DeleteContentItemSucceeded {
  type: 'DELETE_CONTENT_ITEM_SUCCEEDED';
  response: ContentItemDetail;
}
export interface DeleteContentItemFailed {
  type: 'DELETE_CONTENT_ITEM_FAILED';
  error: TSError;
}

/**
 * POST:
 *   Cancel a Content Publication Request;
 */
export interface CancelPublicationRequest {
  type: 'CANCEL_PUBLICATION_REQUEST';
  request: Guid;
}
export interface CancelPublicationRequestSucceeded {
  type: 'CANCEL_PUBLICATION_REQUEST_SUCCEEDED';
  response: {
    statusResponseModel: {
      contentItems: Dict<RootContentItemWithStats>;
      publications: Dict<ContentPublicationRequest>;
      publicationQueue: Dict<PublicationQueueDetails>;
    },
    rootContentItemDetail: ContentItemDetail,
  };
}
export interface CancelPublicationRequestFailed {
  type: 'CANCEL_PUBLICATION_REQUEST_FAILED';
  error: TSError;
}

/**
 * Set the value of a form inputs
 */

export interface SetPublishingFormTextInputValue {
  type: 'SET_PENDING_TEXT_INPUT_VALUE';
  inputName: 'contentDisclaimer' | 'contentName' | 'contentTypeId' | 'contentDescription' | 'contentNotes';
  value: string;
}

export interface SetPublishingFormBooleanInputValue {
  type: 'SET_PENDING_BOOLEAN_INPUT_VALUE';
  inputName:
  | 'doesReduce'
  | 'isSuspended'
  | 'filterPaneEnabled'
  | 'navigationPaneEnabled'
  | 'bookmarksPaneEnabled';
  value: boolean;
}

export interface ResetContentItemForm {
  type: 'RESET_CONTENT_ITEM_FORM';
}

/**
 * Open the modal used to begin content item deletion.
 */
export interface OpenDeleteContentItemModal {
  type: 'OPEN_DELETE_CONTENT_ITEM_MODAL';
  id: Guid;
}

/**
 * Close the modal used to begin content item deletion.
 */
export interface CloseDeleteContentItemModal {
  type: 'CLOSE_DELETE_CONTENT_ITEM_MODAL';
}

/**
 * Open the modal used to confirm content item deletion.
 */
export interface OpenDeleteConfirmationModal {
  type: 'OPEN_DELETE_CONFIRMATION_MODAL';
}

/**
 * Close the modal used to confirm content item deletion.
 */
export interface CloseDeleteConfirmationModal {
  type: 'CLOSE_DELETE_CONFIRMATION_MODAL';
}

/**
 * Open the modal used to confirm Go-Live rejection
 */
export interface OpenGoLiveRejectionModal {
  type: 'OPEN_GO_LIVE_REJECTION_MODAL';
}

/**
 * Close the modal used to confirm Go-Live rejection
 */
export interface CloseGoLiveRejectionModal {
  type: 'CLOSE_GO_LIVE_REJECTION_MODAL';
}

/**
 * Open the modal used to confirm navigation away from a modified form
 */
export interface OpenModifiedFormModal {
  type: 'OPEN_MODIFIED_FORM_MODAL';
  afterFormModal: AfterFormModal;
}

/**
 * Close the modal used to confirm navigation away from a modified form
 */
export interface CloseModifiedFormModal {
  type: 'CLOSE_MODIFIED_FORM_MODAL';
}

/**
 * Open the modal used to confirm cancelation of a publication
 */
export interface OpenCancelPublicationModal {
  type: 'OPEN_CANCEL_PUBLICATION_MODAL';
  id: Guid;
}

/**
 * Close the modal used to confirm cancelation of a publication
 */
export interface CloseCancelPublicationModal {
  type: 'CLOSE_CANCEL_PUBLICATION_MODAL';
}

/**
 * GET:
 *   content items for the selected client;
 *   publications for the selected client;
 *   publication queue information for those publications;
 *   reductions for the selected content item;
 *   reduction queue information for those reductions.
 */
export interface FetchStatusRefresh {
  type: 'FETCH_STATUS_REFRESH';
  request: {
    clientId: Guid;
  };
}
export interface FetchStatusRefreshSucceeded {
  type: 'FETCH_STATUS_REFRESH_SUCCEEDED';
  response: {
    contentItems: Dict<RootContentItem>;
    publications: Dict<ContentPublicationRequest>;
    publicationQueue: Dict<PublicationQueueDetails>;
  };
}
export interface FetchStatusRefreshFailed {
  type: 'FETCH_STATUS_REFRESH_FAILED';
  error: TSError;
}

/**
 * Fetch status refresh after a delay.
 */
export interface ScheduleStatusRefresh {
  type: 'SCHEDULE_STATUS_REFRESH';
  delay: number;
}

/**
 * Decrement remaining status refresh attempts
 */
export interface DecrementStatusRefreshAttempts {
  type: 'DECREMENT_STATUS_REFRESH_ATTEMPTS';
}

/**
 * GET a bodiless response that serves as a session heartbeat.
 */
export interface FetchSessionCheck {
  type: 'FETCH_SESSION_CHECK';
  request: {};
}
export interface FetchSessionCheckSucceeded {
  type: 'FETCH_SESSION_CHECK_SUCCEEDED';
  response: {};
}
export interface FetchSessionCheckFailed {
  type: 'FETCH_SESSION_CHECK_FAILED';
  error: TSError;
}

/**
 * Fetch session check after a delay.
 */
export interface ScheduleSessionCheck {
  type: 'SCHEDULE_SESSION_CHECK';
  delay: number;
}

// ~~ Action unions ~~

/**
 * An action that changes the state of the page.
 */
export type PagePublishingAction =
  | SelectClient
  | SelectItem
  | SetContentItemFormState
  | SetFormForNewContentItem
  | SetFilterTextClient
  | SetFilterTextItem
  | ToggleShowOnlyChanges
  | ToggleGoLiveConfirmationCheckbox
  | SetPublishingFormTextInputValue
  | SetPublishingFormBooleanInputValue
  | ResetContentItemForm
  | PromptStatusRefreshStopped
  | DecrementStatusRefreshAttempts
  | PublishContentFilesSucceeded
  | OpenDeleteContentItemModal
  | CloseDeleteContentItemModal
  | OpenDeleteConfirmationModal
  | CloseDeleteConfirmationModal
  | OpenGoLiveRejectionModal
  | CloseGoLiveRejectionModal
  | OpenModifiedFormModal
  | CloseModifiedFormModal
  | OpenCancelPublicationModal
  | CloseCancelPublicationModal
  ;

/**
 * An action that schedules another action.
 */
export type SchedulePublishingAction =
  | ScheduleSessionCheck
  | ScheduleStatusRefresh
  ;

/**
 * An action that makes an Ajax request.
 */
export type RequestPublishingAction =
  | FetchGlobalData
  | FetchClients
  | FetchItems
  | FetchContentItemDetail
  | FetchGoLiveSummary
  | ApproveGoLiveSummary
  | RejectGoLiveSummary
  | FetchStatusRefresh
  | FetchSessionCheck
  | CreateNewContentItem
  | UpdateContentItem
  | PublishContentFiles
  | DeleteContentItem
  | CancelPublicationRequest
  ;

/**
 * An action that marks the succesful response of an Ajax request.
 */
export type ResponsePublishingAction =
  | FetchGlobalDataSucceeded
  | FetchClientsSucceeded
  | FetchItemsSucceeded
  | FetchContentItemDetailSucceeded
  | FetchGoLiveSummarySucceeded
  | ApproveGoLiveSummarySucceeded
  | RejectGoLiveSummarySucceeded
  | FetchStatusRefreshSucceeded
  | FetchSessionCheckSucceeded
  | CreateNewContentItemSucceeded
  | UpdateContentItemSucceeded
  | PublishContentFilesSucceeded
  | DeleteContentItemSucceeded
  | CancelPublicationRequestSucceeded
  ;

/**
 * An action that marks the errored response of an Ajax request.
 */
export type ErrorPublishingAction =
  | FetchGlobalDataFailed
  | FetchClientsFailed
  | FetchItemsFailed
  | FetchContentItemDetailFailed
  | FetchGoLiveSummaryFailed
  | ApproveGoLiveSummaryFailed
  | RejectGoLiveSummaryFailed
  | FetchStatusRefreshFailed
  | FetchSessionCheckFailed
  | CreateNewContentItemFailed
  | UpdateContentItemFailed
  | PublishContentFilesFailed
  | DeleteContentItemFailed
  | CancelPublicationRequestFailed
  ;

/**
 * An action available to the content publishing page.
 */
export type PublishingAction =
  | PagePublishingAction
  | SchedulePublishingAction
  | RequestPublishingAction
  | ResponsePublishingAction
  | ErrorPublishingAction
  | PageUploadAction
  ;

/**
 * An action that sets filter text for a card column.
 */
export type FilterPublishingAction =
  | SetFilterTextClient
  | SetFilterTextItem
  ;

/**
 * An action that opens a modal.
 */
export type OpenModalAction =
  | OpenDeleteContentItemModal
  | OpenDeleteConfirmationModal
  | OpenGoLiveRejectionModal
  | OpenModifiedFormModal
  | OpenCancelPublicationModal
  ;
