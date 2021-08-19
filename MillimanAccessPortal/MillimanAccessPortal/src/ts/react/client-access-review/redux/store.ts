import * as toastr from 'react-redux-toastr';
import { applyMiddleware, createStore } from 'redux';
import { composeWithDevTools } from 'redux-devtools-extension';
import createSagaMiddleware from 'redux-saga';

import { ClientWithReviewDate, Guid } from '../../models';
import { CardAttributes } from '../../shared-components/card/card';
import { Dict, FilterState, ModalState } from '../../shared-components/redux/store';
import { clientAccessReview } from './reducers';
import sagas from './sagas';

export interface AccessReviewGlobalData {
  clientReviewEarlyWarningDays: number;
}

export interface ClientSummaryModel {
  clientName: string;
  clientCode: string;
  reviewDueDate: string;
  lastReviewDate: string;
  lastReviewedBy: ClientActorModel;
  primaryContactName: string;
  primaryContactEmail: string;
  assignedProfitCenter: string;
  clientAdmins: ClientActorModel[];
  profitCenterAdmins: ClientActorModel[];
  usesCustomPowerBICapacity: boolean;
}

export interface ClientActorModel {
  name: string;
  userEmail: string;
  lastLoginDate?: string;
  disableAccountDate?: string;
  isAccountDisabled: boolean;
  isAccountNearDisabled: boolean;
  isSuspended: boolean;
}

export interface ClientAccessReviewModel {
  id: Guid;
  clientName: string;
  clientCode: string;
  clientAdmins: ClientActorModel[];
  assignedProfitCenterName: string;
  profitCenterAdmins: ClientActorModel[];
  approvedEmailDomainList: string[];
  approvedEmailExceptionList: string[];
  memberUsers: ClientActorReviewModel[];
  contentItems: ClientContentItemModel[];
  fileDrops: ClientFileDropModel[];
  attestationLanguage: string;
  clientAccessReviewId: Guid;
}

interface ClientActorReviewModel extends ClientActorModel {
  clientUserRoles: {
    Admin: boolean;
    ContentAccessAdmin: boolean;
    ContentPublisher: boolean;
    ContentUser: boolean;
    FileDropAdmin: boolean;
    FileDropUser: boolean;
    UserCreator: boolean;
  };
}

interface ClientContentItemModel {
  id: Guid;
  contentType: string;
  contentItemName: string;
  isSuspended: boolean;
  lastPublishedDate: string;
  selectionGroups: ClientContentItemSelectionGroupModel[];
}

interface ClientContentItemSelectionGroupModel {
  selectionGroupName: string;
  isSuspended: boolean;
  authorizedUsers: ClientActorModel[];
}

interface ClientFileDropModel {
  id: Guid;
  fileDropName: string;
  permissionGroups: ClientFileDropPermissionGroupModel[];
}

interface ClientFileDropPermissionGroupModel {
  permissionGroupName: string;
  isPersonalGroup: boolean;
  permissions: {
    Read: boolean;
    Write: boolean;
    Delete: boolean;
  };
  authorizedMapUsers: ClientActorModel[];
  authorizedServiceAccounts: ClientActorModel[];
}

export enum ClientAccessReviewProgressEnum {
  clientReview = 0,
  userRoles = 1,
  contentAccess = 2,
  fileDropAccess = 3,
  attestations = 4,
}

export interface ClientAccessReviewProgress {
  step: ClientAccessReviewProgressEnum;
  contentItemConfirmations: Dict<boolean>;
  fileDropConfirmations: Dict<boolean>;
}

/**
 * Flags indicating whether the page is waiting on new data for an entity type.
 */
export interface PendingDataState {
  clients: boolean;
  clientSummary: boolean;
  clientAccessReview: boolean;
  approveAccessReview: boolean;
}

/**
 * Entity data returned from the server.
 */
export interface AccessReviewStateData {
  globalData: AccessReviewGlobalData;
  clients: Dict<ClientWithReviewDate>;
  selectedClientSummary: ClientSummaryModel;
  clientAccessReview: ClientAccessReviewModel;
}

/**
 * Selected cards.
 */
export interface AccessReviewStateSelected {
  client: Guid;
}

/**
 * Card attribute collections.
 */
export interface AccessReviewStateCardAttributes {
  client: Dict<CardAttributes>;
}

/**
 * Client sort order
 */
export interface ClientSort {
  sortBy: 'date' | 'name';
  sortOrder: 'asc' | 'desc';
}

export enum ClientReviewDeadlineStatusEnum {
  Unspecified = 0,
  Current = 1,
  EarlyWarning = 2,
  Expired = 3,
}

/**
 * All state that represents a change pending submission.
 */
export interface AccessReviewStatePending {
  data: PendingDataState;
  clientAccessReviewProgress: ClientAccessReviewProgress;
  pendingClientSelection: Guid;
  clientSort: ClientSort;
  statusTries: number;
  navBarRenderInt: number;
}

/**
 * All filter state.
 */
export interface AccessReviewStateFilters {
  client: FilterState;
}

/**
 * All modal state.
 */
export interface AccessReviewStateModals {
  leaveActiveReview: ModalState;
}

/**
 * All content access admin state.
 */
export interface AccessReviewState {
  data: AccessReviewStateData;
  selected: AccessReviewStateSelected;
  cardAttributes: AccessReviewStateCardAttributes;
  pending: AccessReviewStatePending;
  filters: AccessReviewStateFilters;
  modals: AccessReviewStateModals;
  toastr: toastr.ToastrState;
}

// Create the store and apply saga middleware
const sagaMiddleware = createSagaMiddleware();
export const store = createStore(
  clientAccessReview,
  composeWithDevTools(
    applyMiddleware(sagaMiddleware),
  ));
sagaMiddleware.run(sagas);
