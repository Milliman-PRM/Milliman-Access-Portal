import {
    ClientWithEligibleUsers, ClientWithStats, ContentPublicationRequest, ContentReductionTask,
    ContentType, Guid, PublicationQueueDetails, ReductionField, ReductionFieldValue,
    ReductionQueueDetails, RootContentItem, RootContentItemWithStats, SelectionGroup,
    SelectionGroupWithAssignedUsers, User,
} from '../../models';
import { Dict } from './store';

export type TSError = any;  // any by necessity due to the nature of try/catch in TypeScript

export interface SelectClient {
  type: 'SELECT_CLIENT';
  id: Guid;
}
export interface SelectItem {
  type: 'SELECT_ITEM';
  id: Guid;
}
export interface SelectGroup {
  type: 'SELECT_GROUP';
  id: Guid;
}
export interface SetExpandedGroup {
  type: 'SET_EXPANDED_GROUP';
  id: Guid;
}
export interface SetCollapsedGroup {
  type: 'SET_COLLAPSED_GROUP';
  id: Guid;
}
export interface SetAllExpandedGroup {
  type: 'SET_ALL_EXPANDED_GROUP';
}
export interface SetAllCollapsedGroup {
  type: 'SET_ALL_COLLAPSED_GROUP';
}
export interface SetFilterTextClient {
  type: 'SET_FILTER_TEXT_CLIENT';
  text: string;
}
export interface SetFilterTextItem {
  type: 'SET_FILTER_TEXT_ITEM';
  text: string;
}
export interface SetFilterTextGroup {
  type: 'SET_FILTER_TEXT_GROUP';
  text: string;
}
export interface SetFilterTextSelections {
  type: 'SET_FILTER_TEXT_SELECTIONS';
  text: string;
}
export interface SetPendingIsMaster {
  type: 'SET_PENDING_IS_MASTER';
  isMaster: boolean;
}
export interface SetPendingSelectionOn {
  type: 'SET_PENDING_SELECTION_ON';
  id: Guid;
}
export interface SetPendingSelectionOff {
  type: 'SET_PENDING_SELECTION_OFF';
  id: Guid;
}
export interface OpenAddGroupModal {
  type: 'OPEN_ADD_GROUP_MODAL';
}
export interface CloseAddGroupModal {
  type: 'CLOSE_ADD_GROUP_MODAL';
}
export interface OpenDeleteGroupModal {
  type: 'OPEN_DELETE_GROUP_MODAL';
  id: Guid;
}
export interface CloseDeleteGroupModal {
  type: 'CLOSE_DELETE_GROUP_MODAL';
}
export interface OpenInvalidateModal {
  type: 'OPEN_INVALIDATE_MODAL';
}
export interface CloseInvalidateModal {
  type: 'CLOSE_INVALIDATE_MODAL';
}
export interface SetPendingNewGroupName {
  type: 'SET_PENDING_NEW_GROUP_NAME';
  name: string;
}
export interface SetGroupEditingOn {
  type: 'SET_GROUP_EDITING_ON';
  id: Guid;
}
export interface SetGroupEditingOff {
  type: 'SET_GROUP_EDITING_OFF';
  id: Guid;
}
export interface SetPendingGroupName {
  type: 'SET_PENDING_GROUP_NAME';
  name: string;
}
export interface SetPendingGroupUserQuery {
  type: 'SET_PENDING_GROUP_USER_QUERY';
  query: string;
}
export interface SetPendingGroupUserAssigned {
  type: 'SET_PENDING_GROUP_USER_ASSIGNED';
  id: Guid;
}
export interface SetPendingGroupUserRemoved {
  type: 'SET_PENDING_GROUP_USER_REMOVED';
  id: Guid;
}
export interface PromptGroupEditing {
  type: 'PROMPT_GROUP_EDITING';
}
// ~~ Action export interfaces: fetches ~~
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
    publications: Dict<ContentPublicationRequest>;
    publicationQueue: Dict<PublicationQueueDetails>;
    reductions: Dict<ContentReductionTask>;
    reductionQueue: Dict<ReductionQueueDetails>;
    liveSelectionsSet: Dict<Guid[]>;
    contentItems: Dict<RootContentItem>;
    groups: Dict<SelectionGroup>;
  };
}
export interface FetchStatusRefreshFailed {
  type: 'FETCH_STATUS_REFRESH_FAILED';
  error: TSError;
}
export interface ScheduleStatusRefresh {
  type: 'SCHEDULE_STATUS_REFRESH';
  delay: number;
}
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
export interface ScheduleSessionCheck {
  type: 'SCHEDULE_SESSION_CHECK';
  delay: number;
}
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
  | OpenAddGroupModal
  | CloseAddGroupModal
  | OpenDeleteGroupModal
  | CloseDeleteGroupModal
  | OpenInvalidateModal
  | CloseInvalidateModal
  | SetPendingNewGroupName
  | SetGroupEditingOn
  | SetGroupEditingOff
  | SetPendingGroupName
  | SetPendingGroupUserQuery
  | SetPendingGroupUserAssigned
  | SetPendingGroupUserRemoved
  | PromptGroupEditing
  ;
export type ScheduleAction = ScheduleSessionCheck | ScheduleStatusRefresh;
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
// All actions for the content access admin page
export type AccessAction = PageAction | ScheduleAction | RequestAction | ResponseAction | ErrorAction;

// Additional action categories
export type FilterAction = SetFilterTextClient | SetFilterTextItem | SetFilterTextGroup | SetFilterTextSelections;
export type OpenAction = OpenAddGroupModal | OpenDeleteGroupModal | OpenInvalidateModal;
export type CloseAction = CloseAddGroupModal | CloseDeleteGroupModal | CloseInvalidateModal;

export function isScheduleAction(action: AccessAction): action is ScheduleAction {
  return (action as ScheduleAction).delay !== undefined;
}
