import {
    ClientWithEligibleUsers, ClientWithStats, ContentAssociatedFileType, ContentPublicationRequest,
    ContentReductionTask, ContentType, Guid, PublicationQueueDetails, ReductionQueueDetails,
    RootContentItem, RootContentItemWithStats, SelectionGroup, User,
} from '../../models';
import { TSError } from '../../shared-components/redux/actions';
import { Dict } from '../../shared-components/redux/store';

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
  | SetFilterTextClient
  | SetFilterTextItem
  | PromptStatusRefreshStopped
  | DecrementStatusRefreshAttempts
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
  | FetchStatusRefresh
  | FetchSessionCheck
  ;

/**
 * An action that marks the succesful response of an Ajax request.
 */
export type ResponsePublishingAction =
  | FetchGlobalDataSucceeded
  | FetchClientsSucceeded
  | FetchItemsSucceeded
  | FetchStatusRefreshSucceeded
  | FetchSessionCheckSucceeded
  ;

/**
 * An action that marks the errored response of an Ajax request.
 */
export type ErrorPublishingAction =
  | FetchGlobalDataFailed
  | FetchClientsFailed
  | FetchItemsFailed
  | FetchStatusRefreshFailed
  | FetchSessionCheckFailed
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
  ;

/**
 * An action that sets filter text for a card column.
 */
export type FilterPublishingAction =
  | SetFilterTextClient
  | SetFilterTextItem
  ;
