import {
    ClientWithEligibleUsers, ClientWithStats, ContentPublicationRequest, ContentReductionTask,
    ContentType, Guid, PublicationQueueDetails, ReductionField, ReductionFieldValue,
    ReductionQueueDetails, RootContentItem, RootContentItemWithStats, SelectionGroup,
    SelectionGroupWithAssignedUsers, User,
} from '../../models';
import { Dict } from './store';

/**
 * Error type alias.
 * Aliased as any by necessity due to the nature of try/catch in TypeScript.
 */
export type TSError = any;

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
 * Exclusively select the selection group card specified by id.
 * If id refers to the currently selected card, deselect it.
 */
export interface SelectGroup {
  type: 'SELECT_GROUP';
  id: Guid;
}

/**
 * Expand the selection group card specified by id.
 */
export interface SetExpandedGroup {
  type: 'SET_EXPANDED_GROUP';
  id: Guid;
}

/**
 * Collapse the selection group card specified by id.
 */
export interface SetCollapsedGroup {
  type: 'SET_COLLAPSED_GROUP';
  id: Guid;
}

/**
 * Expand all selection group cards.
 */
export interface SetAllExpandedGroup {
  type: 'SET_ALL_EXPANDED_GROUP';
}

/**
 * Collapse all selection group cards.
 */
export interface SetAllCollapsedGroup {
  type: 'SET_ALL_COLLAPSED_GROUP';
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
 * Set filter text for the selection group card filter.
 */
export interface SetFilterTextGroup {
  type: 'SET_FILTER_TEXT_GROUP';
  text: string;
}

/**
 * Set filter text for the selections filter.
 */
export interface SetFilterTextSelections {
  type: 'SET_FILTER_TEXT_SELECTIONS';
  text: string;
}

/**
 * Set master status for the current selection group.
 * This change is not made permanent until pending selection changes are submitted to the server.
 */
export interface SetPendingIsMaster {
  type: 'SET_PENDING_IS_MASTER';
  isMaster: boolean;
}

/**
 * Select a hierarchy field value for the current selection group.
 * This change is not made permanent until pending selection changes are submitted to the server.
 */
export interface SetPendingSelectionOn {
  type: 'SET_PENDING_SELECTION_ON';
  id: Guid;
}

/**
 * Deselect a hierarchy field value for the current selection group.
 * This change is not made permanent until pending selection changes are submitted to the server.
 */
export interface SetPendingSelectionOff {
  type: 'SET_PENDING_SELECTION_OFF';
  id: Guid;
}

/**
 * Select all hierarchy fields for the current selection group.
 * This change is not made permanent until pending selection changes are submitted to the server.
 */
export interface SetPendingAllSelectionsOn {
  type: 'SET_PENDING_ALL_SELECTIONS_ON';
}

/**
 * Deselect all hierarchy fields for the current selection group.
 * This change is not made permanent until pending selection changes are submitted to the server.
 */
export interface SetPendingAllSelectionsOff {
  type: 'SET_PENDING_ALL_SELECTIONS_OFF';
}

/**
 * Open the modal used to add new selection groups.
 */
export interface OpenAddGroupModal {
  type: 'OPEN_ADD_GROUP_MODAL';
}

/**
 * Close the modal used to add new selection groups.
 */
export interface CloseAddGroupModal {
  type: 'CLOSE_ADD_GROUP_MODAL';
}

/**
 * Open the modal used to confirm selection group deletion.
 */
export interface OpenDeleteGroupModal {
  type: 'OPEN_DELETE_GROUP_MODAL';
  id: Guid;
}

/**
 * Close the modal used to confirm selection group deletion.
 */
export interface CloseDeleteGroupModal {
  type: 'CLOSE_DELETE_GROUP_MODAL';
}

/**
 * Open the modal used to warn that the pending selections will cause the selection group to become inactive.
 */
export interface OpenInactiveModal {
  type: 'OPEN_INACTIVE_MODAL';
}

/**
 * Open the modal used to warn that the pending selections will cause the selection group to become inactive.
 */
export interface CloseInactiveModal {
  type: 'CLOSE_INACTIVE_MODAL';
}

/**
 * Set the name for a new selection group.
 */
export interface SetPendingNewGroupName {
  type: 'SET_PENDING_NEW_GROUP_NAME';
  name: string;
}

/**
 * Start exclusively editing a selection group.
 */
export interface SetGroupEditingOn {
  type: 'SET_GROUP_EDITING_ON';
  id: Guid;
}

/**
 * Stop editing a selection group.
 */
export interface SetGroupEditingOff {
  type: 'SET_GROUP_EDITING_OFF';
  id: Guid;
}

/**
 * Set a selection group's name.
 * This change is not made permanent until selection group changes are submitted.
 */
export interface SetPendingGroupName {
  type: 'SET_PENDING_GROUP_NAME';
  name: string;
}

/**
 * Set selection group user filter text.
 */
export interface SetPendingGroupUserQuery {
  type: 'SET_PENDING_GROUP_USER_QUERY';
  query: string;
}

/**
 * Set a user as assigned to the selection group being edited.
 * This change is not made permanent until selection group changes are submitted.
 */
export interface SetPendingGroupUserAssigned {
  type: 'SET_PENDING_GROUP_USER_ASSIGNED';
  id: Guid;
}

/**
 * Set a user as removed from the selection group being edited.
 * This change is not made permanent until selection group changes are submitted.
 */
export interface SetPendingGroupUserRemoved {
  type: 'SET_PENDING_GROUP_USER_REMOVED';
  id: Guid;
}

/**
 * Display a toast indicating that an action could not be performed while a selection group is being edited.
 */
export interface PromptGroupEditing {
  type: 'PROMPT_GROUP_EDITING';
}

/**
 * Display a toast indicating that an action could not be performed because a selection group name was empty.
 */
export interface PromptGroupNameEmpty {
  type: 'PROMPT_GROUP_NAME_EMPTY';
}

// ~~ Server actions ~~

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
    users: Dict<User>;
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
    contentItems: Dict<RootContentItemWithStats>;
    contentTypes: Dict<ContentType>;
    publications: Dict<ContentPublicationRequest>;
    publicationQueue: Dict<PublicationQueueDetails>;
    clientStats: ClientWithStats;
  };
}
export interface FetchItemsFailed {
  type: 'FETCH_ITEMS_FAILED';
  error: TSError;
}

/**
 * GET:
 *   selection groups in the selected content item;
 *   reductions for those selection groups;
 *   reduction queue information for those reductions;
 *   updated card stats for the selected content item;
 *   updated card stats for the selected client.
 */
export interface FetchGroups {
  type: 'FETCH_GROUPS';
  request: {
    contentItemId: Guid;
  };
}
export interface FetchGroupsSucceeded {
  type: 'FETCH_GROUPS_SUCCEEDED';
  response: {
    groups: Dict<SelectionGroupWithAssignedUsers>;
    reductions: Dict<ContentReductionTask>;
    reductionQueue: Dict<ReductionQueueDetails>;
    contentItemStats: RootContentItemWithStats;
    clientStats: ClientWithStats;
  };
}
export interface FetchGroupsFailed {
  type: 'FETCH_GROUPS_FAILED';
  error: TSError;
}

/**
 * GET:
 *   ID of the selected selection group;
 *   live selections for the selected selection group;
 *   selections for the active reduction of the selected selection group (if any);
 *   hierarchy fields for the selected selection group;
 *   hierarchy field values for the selected selection group.
 */
export interface FetchSelections {
  type: 'FETCH_SELECTIONS';
  request: {
    groupId: Guid;
  };
}
export interface FetchSelectionsSucceeded {
  type: 'FETCH_SELECTIONS_SUCCEEDED';
  response: {
    id: Guid;
    liveSelections: Guid[];
    reductionSelections: Guid[];
    fields: Dict<ReductionField>;
    values: Dict<ReductionFieldValue>;
  };
}
export interface FetchSelectionsFailed {
  type: 'FETCH_SELECTIONS_FAILED';
  error: TSError;
}

/**
 * GET:
 *   content items for the selected client;
 *   selection groups for the selected content item;
 *   live selections for those selection groups;
 *   publications for the selected client;
 *   publication queue information for those publications;
 *   reductions for the selected content item;
 *   reduction queue information for those reductions.
 */
export interface FetchStatusRefresh {
  type: 'FETCH_STATUS_REFRESH';
  request: {
    clientId: Guid;
    contentItemId: Guid;
  };
}
export interface FetchStatusRefreshSucceeded {
  type: 'FETCH_STATUS_REFRESH_SUCCEEDED';
  response: {
    contentItems: Dict<RootContentItem>;
    groups: Dict<SelectionGroup>;
    liveSelectionsSet: Dict<Guid[]>;
    publications: Dict<ContentPublicationRequest>;
    publicationQueue: Dict<PublicationQueueDetails>;
    reductions: Dict<ContentReductionTask>;
    reductionQueue: Dict<ReductionQueueDetails>;
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

/**
 * POST a new selection group.
 */
export interface CreateGroup {
  type: 'CREATE_GROUP';
  request: {
    contentItemId: Guid;
    name: string;
  };
}
export interface CreateGroupSucceeded {
  type: 'CREATE_GROUP_SUCCEEDED';
  response: {
    group: SelectionGroupWithAssignedUsers;
    contentItemStats: RootContentItemWithStats;
  };
}
export interface CreateGroupFailed {
  type: 'CREATE_GROUP_FAILED';
  error: TSError;
}

/**
 * POST an update to an existing selection group.
 */
export interface UpdateGroup {
  type: 'UPDATE_GROUP';
  request: {
    groupId: Guid;
    name: string;
    users: Guid[];
  };
}
export interface UpdateGroupSucceeded {
  type: 'UPDATE_GROUP_SUCCEEDED';
  response: {
    group: SelectionGroupWithAssignedUsers;
    contentItemStats: RootContentItemWithStats;
  };
}
export interface UpdateGroupFailed {
  type: 'UPDATE_GROUP_FAILED';
  error: TSError;
}

/**
 * POST the deletion of an existing selection group.
 */
export interface DeleteGroup {
  type: 'DELETE_GROUP';
  request: {
    groupId: Guid;
  };
}
export interface DeleteGroupSucceeded {
  type: 'DELETE_GROUP_SUCCEEDED';
  response: {
    groupId: Guid;
    contentItemStats: RootContentItemWithStats;
  };
}
export interface DeleteGroupFailed {
  type: 'DELETE_GROUP_FAILED';
  error: TSError;
}

/**
 * POST the suspension of an existing selection group.
 */
export interface SuspendGroup {
  type: 'SUSPEND_GROUP';
  request: {
    groupId: Guid;
    isSuspended: boolean;
  };
}
export interface SuspendGroupSucceeded {
  type: 'SUSPEND_GROUP_SUCCEEDED';
  response: SelectionGroup;
}
export interface SuspendGroupFailed {
  type: 'SUSPEND_GROUP_FAILED';
  error: TSError;
}

/**
 * POST new selections for an existing selection group.
 */
export interface UpdateSelections {
  type: 'UPDATE_SELECTIONS';
  request: {
    groupId: Guid;
    isMaster: boolean;
    selections: Guid[];
  };
}
export interface UpdateSelectionsSucceeded {
  type: 'UPDATE_SELECTIONS_SUCCEEDED';
  response: {
    group: SelectionGroup;
    reduction: ContentReductionTask;
    reductionQueue: ReductionQueueDetails;
    liveSelections: Guid[];
  };
}
export interface UpdateSelectionsFailed {
  type: 'UPDATE_SELECTIONS_FAILED';
  error: TSError;
}

/**
 * POST the cancelation of a reduction for an existing selection group.
 */
export interface CancelReduction {
  type: 'CANCEL_REDUCTION';
  request: {
    groupId: Guid;
  };
}
export interface CancelReductionSucceeded {
  type: 'CANCEL_REDUCTION_SUCCEEDED';
  response: {
    group: SelectionGroup;
    reduction: ContentReductionTask;
    reductionQueue: ReductionQueueDetails;
    liveSelections: Guid[];
  };
}
export interface CancelReductionFailed {
  type: 'CANCEL_REDUCTION_FAILED';
  error: TSError;
}

// ~~ Action unions ~~

/**
 * An action that changes the state of the page.
 */
export type PageAction = SelectClient
  | SelectItem
  | SelectGroup
  | SetExpandedGroup
  | SetCollapsedGroup
  | SetAllExpandedGroup
  | SetAllCollapsedGroup
  | SetFilterTextClient
  | SetFilterTextItem
  | SetFilterTextGroup
  | SetFilterTextSelections
  | SetPendingIsMaster
  | SetPendingSelectionOn
  | SetPendingSelectionOff
  | SetPendingAllSelectionsOn
  | SetPendingAllSelectionsOff
  | OpenAddGroupModal
  | CloseAddGroupModal
  | OpenDeleteGroupModal
  | CloseDeleteGroupModal
  | OpenInactiveModal
  | CloseInactiveModal
  | SetPendingNewGroupName
  | SetGroupEditingOn
  | SetGroupEditingOff
  | SetPendingGroupName
  | SetPendingGroupUserQuery
  | SetPendingGroupUserAssigned
  | SetPendingGroupUserRemoved
  | PromptGroupEditing
  | PromptGroupNameEmpty
  ;

/**
 * An action that schedules another action.
 */
export type ScheduleAction = ScheduleSessionCheck | ScheduleStatusRefresh;

/**
 * An action that makes an Ajax request.
 */
export type RequestAction = FetchClients
  | FetchItems
  | FetchGroups
  | FetchSelections
  | FetchStatusRefresh
  | FetchSessionCheck
  | CreateGroup
  | UpdateGroup
  | DeleteGroup
  | SuspendGroup
  | UpdateSelections
  | CancelReduction
  ;

/**
 * An action that marks the succesful response of an Ajax request.
 */
export type ResponseAction = FetchClientsSucceeded
  | FetchItemsSucceeded
  | FetchGroupsSucceeded
  | FetchSelectionsSucceeded
  | FetchStatusRefreshSucceeded
  | FetchSessionCheckSucceeded
  | CreateGroupSucceeded
  | UpdateGroupSucceeded
  | DeleteGroupSucceeded
  | SuspendGroupSucceeded
  | UpdateSelectionsSucceeded
  | CancelReductionSucceeded
  ;

/**
 * An action that marks the errored response of an Ajax request.
 */
export type ErrorAction = FetchClientsFailed
  | FetchItemsFailed
  | FetchGroupsFailed
  | FetchSelectionsFailed
  | FetchStatusRefreshFailed
  | FetchSessionCheckFailed
  | CreateGroupFailed
  | UpdateGroupFailed
  | DeleteGroupFailed
  | SuspendGroupFailed
  | UpdateSelectionsFailed
  | CancelReductionFailed
  ;

/**
 * An action available to the content access administration page.
 */
export type AccessAction = PageAction | ScheduleAction | RequestAction | ResponseAction | ErrorAction;

/**
 * An action that sets filter text for a card column.
 */
export type FilterAction = SetFilterTextClient | SetFilterTextItem | SetFilterTextGroup | SetFilterTextSelections;

/**
 * An action that opens a modal.
 */
export type OpenAction = OpenAddGroupModal | OpenDeleteGroupModal | OpenInactiveModal;

/**
 * An action that closes a modal.
 */
export type CloseAction = CloseAddGroupModal | CloseDeleteGroupModal | CloseInactiveModal;

/**
 * Schedule action type guard.
 * @param action An action to inspect.
 */
export function isScheduleAction(action: AccessAction): action is ScheduleAction {
  return (action as ScheduleAction).delay !== undefined;
}

/**
 * Error action type guard.
 * @param action An action to inspect.
 */
export function isErrorAction(action: AccessAction): action is ErrorAction {
  return (action as ErrorAction).error !== undefined;
}
