import {
    ClientWithEligibleUsers, ClientWithStats, ContentPublicationRequest, ContentReductionTask,
    ContentType, Guid, PublicationQueueDetails, ReductionField, ReductionFieldValue,
    ReductionQueueDetails, RootContentItem, RootContentItemWithStats, SelectionGroup,
    SelectionGroupWithAssignedUsers, User,
} from '../../models';
import { TSError } from '../../shared-components/redux/actions';
import { Dict } from '../../shared-components/redux/store';
import { AccessReviewGlobalData } from './store';

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
    clients: Dict<ClientWithEligibleUsers>;
    parentClients: Dict<ClientWithStats>;
    users: Dict<User>;
  };
}
export interface FetchClientsFailed {
  type: 'FETCH_CLIENTS_FAILED';
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
  | FetchSessionCheck
  ;

/**
 * An action that marks the succesful response of an Ajax request.
 */
export type ResponseAccessReviewAction =
  | FetchGlobalDataSucceeded
  | FetchClientsSucceeded
  | FetchSessionCheckSucceeded
  ;

/**
 * An action that marks the errored response of an Ajax request.
 */
export type ErrorAccessReviewAction =
  | FetchGlobalDataFailed
  | FetchClientsFailed
  | FetchSessionCheckFailed
  ;

/**
 * An action available to the content access administration page.
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
export type OpenAccessReviewAction = null;

/**
 * An action that closes a modal.
 */
export type CloseAccessReviewAction = null;
