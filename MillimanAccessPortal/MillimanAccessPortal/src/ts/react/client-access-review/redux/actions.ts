import { ClientWithReviewDate } from '../../models';
import { Guid } from '../../shared-components/interfaces';
import { TSError } from '../../shared-components/redux/actions';
import { Dict } from '../../shared-components/redux/store';
import { AccessReviewGlobalData, ClientAccessReviewModel, ClientSummaryModel } from './store';

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
 * Set filter text for the client card filter.
 */
export interface SetFilterTextClient {
  type: 'SET_FILTER_TEXT_CLIENT';
  text: string;
}

/**
 * Go to the next step of the Client Access Review.
 */
export interface GoToNextAccessReviewStep {
  type: 'GO_TO_NEXT_ACCESS_REVIEW_STEP';
}

/**
 * Go to the previous step of the Client Access Review.
 */
export interface GoToPreviousAccessReviewStep {
  type: 'GO_TO_PREVIOUS_ACCESS_REVIEW_STEP';
}

/**
 * Toggle the provided Content Item review status.
 */
export interface ToggleContentItemReviewStatus {
  type: 'TOGGLE_CONTENT_ITEM_REVIEW_STATUS';
  contentItemId: Guid;
}

/**
 * Toggle the provided File Drop review status.
 */
export interface ToggleFileDropReviewStatus {
  type: 'TOGGLE_FILE_DROP_REVIEW_STATUS';
  fileDropId: Guid;
}

/**
 * Cancel the current Client Access Review.
 */
export interface CancelClientAccessReview {
  type: 'CANCEL_CLIENT_ACCESS_REVIEW';
}

/**
 * Open the Leaving Active Review Modal.
 */
export interface OpenLeavingActiveReviewModal {
  type: 'OPEN_LEAVING_ACTIVE_REVIEW_MODAL';
  clientId?: Guid;
}

/**
 * Close the Leaving Active Review Modal.
 */
export interface CloseLeavingActiveReviewModal {
  type: 'CLOSE_LEAVING_ACTIVE_REVIEW_MODAL';
}

/**
 * Re-render the NavBar to update its contents
 */
export interface UpdateNavBar {
  type: 'UPDATE_NAV_BAR';
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
  response: AccessReviewGlobalData;
}
export interface FetchGlobalDataFailed {
  type: 'FETCH_GLOBAL_DATA_FAILED';
  error: TSError;
}

/**
 * GET:
 *   clients the current user has access to manage;
 *   users who are content eligible in any of those clients.
 */
export interface FetchClients {
  type: 'FETCH_CLIENTS';
  request: {};
}
export interface FetchClientsSucceeded {
  type: 'FETCH_CLIENTS_SUCCEEDED';
  response: {
    clients: Dict<ClientWithReviewDate>;
    parentClients: Dict<ClientWithReviewDate>;
  };
}
export interface FetchClientsFailed {
  type: 'FETCH_CLIENTS_FAILED';
  error: TSError;
}

/**
 * GET:
 *   A summary of the selected client for previewing before starting
 *   an access review
 */
export interface FetchClientSummary {
  type: 'FETCH_CLIENT_SUMMARY';
  request: {
    clientId: Guid;
  };
}
export interface FetchClientSummarySucceeded {
  type: 'FETCH_CLIENT_SUMMARY_SUCCEEDED';
  response: ClientSummaryModel;
}
export interface FetchClientSummaryFailed {
  type: 'FETCH_CLIENT_SUMMARY_FAILED';
  error: TSError;
}

/**
 * GET:
 *   All data necessary to perform a client access review
 */
export interface FetchClientReview {
  type: 'FETCH_CLIENT_REVIEW';
  request: {
    clientId: Guid;
  };
}
export interface FetchClientReviewSucceeded {
  type: 'FETCH_CLIENT_REVIEW_SUCCEEDED';
  response: ClientAccessReviewModel;
}
export interface FetchClientReviewFailed {
  type: 'FETCH_CLIENT_REVIEW_FAILED';
  error: TSError;
}

/**
 * POST:
 *   Send approval of a client access review
 */
export interface ApproveClientAccessReview {
  type: 'APPROVE_CLIENT_ACCESS_REVIEW';
  request: {
    clientId: Guid;
    reviewId: Guid;
  };
}
export interface ApproveClientAccessReviewSucceeded {
  type: 'APPROVE_CLIENT_ACCESS_REVIEW_SUCCEEDED';
  response: {
    clients: Dict<ClientWithReviewDate>;
    parentClients: Dict<ClientWithReviewDate>;
  };
}
export interface ApproveClientAccessReviewFailed {
  type: 'APPROVE_CLIENT_ACCESS_REVIEW_FAILED';
  error: TSError;
}

// ~~ Session Checks ~~ //

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
export type PageAccessReviewAction =
  | SelectClient
  | SetFilterTextClient
  | GoToNextAccessReviewStep
  | GoToPreviousAccessReviewStep
  | ToggleContentItemReviewStatus
  | ToggleFileDropReviewStatus
  | CancelClientAccessReview
  | OpenLeavingActiveReviewModal
  | CloseAccessReviewModalAction
  | UpdateNavBar
  ;

/**
 * An action that schedules another action.
 */
export type ScheduleAccessReviewAction =
  | ScheduleSessionCheck
  ;

/**
 * An action that makes an Ajax request.
 */
export type RequestAccessReviewAction =
  | FetchGlobalData
  | FetchClients
  | FetchClientSummary
  | FetchClientReview
  | ApproveClientAccessReview
  | FetchSessionCheck
  ;

/**
 * An action that marks the succesful response of an Ajax request.
 */
export type ResponseAccessReviewAction =
  | FetchGlobalDataSucceeded
  | FetchClientsSucceeded
  | FetchClientSummarySucceeded
  | FetchClientReviewSucceeded
  | ApproveClientAccessReviewSucceeded
  | FetchSessionCheckSucceeded
  ;

/**
 * An action that marks the errored response of an Ajax request.
 */
export type ErrorAccessReviewAction =
  | FetchGlobalDataFailed
  | FetchClientsFailed
  | FetchClientSummaryFailed
  | FetchClientReviewFailed
  | ApproveClientAccessReviewFailed
  | FetchSessionCheckFailed
  ;

/**
 * An action available to the client access review page.
 */
export type AccessReviewAction =
  | PageAccessReviewAction
  | ScheduleAccessReviewAction
  | RequestAccessReviewAction
  | ResponseAccessReviewAction
  | ErrorAccessReviewAction
  ;

/**
 * An action that sets filter text for a card column.
 */
export type FilterAccessReviewAction =
  | SetFilterTextClient
  ;

/**
 * An action that opens a modal.
 */
export type OpenAccessReviewModalAction = 
  | OpenLeavingActiveReviewModal
  ;

/**
 * An action that closes a modal.
 */
export type CloseAccessReviewModalAction =
  | CloseLeavingActiveReviewModal
  ;
