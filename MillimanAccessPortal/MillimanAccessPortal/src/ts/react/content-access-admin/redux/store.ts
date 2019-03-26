import * as toastr from 'react-redux-toastr';
import { applyMiddleware, createStore } from 'redux';
import createSagaMiddleware from 'redux-saga';

import {
    ClientWithEligibleUsers, ClientWithStats, ContentPublicationRequest, ContentReductionTask,
    ContentType, Guid, PublicationQueueDetails, ReductionField, ReductionFieldValue,
    ReductionQueueDetails, RootContentItemWithStats, SelectionGroupWithAssignedUsers, User,
} from '../../models';
import { CardAttributes } from '../../shared-components/card/card';
import { contentAccessAdmin } from './reducers';
import sagas from './sagas';

/**
 * Alias for a string indexed object.
 */
export interface Dict<T> {
  [key: string]: T;
}

/**
 * State attached to card column filters.
 */
export interface FilterState {
  text: string;
}
/**
 * State attached to modals.
 */
export interface ModalState {
  isOpen: boolean;
}
/**
 * State attached to a user pending assignment/removal from a selection group.
 */
export interface PendingGroupUserState {
  assigned: boolean;
}
/**
 * Flags indicating whether the page is waiting on new data for an entity type.
 */
export interface PendingDataState {
  clients: boolean;
  items: boolean;
  groups: boolean;
  selections: boolean;
  createGroup: boolean;
  updateGroup: boolean;
  deleteGroup: boolean;
  suspendGroup: boolean;
  updateSelections: boolean;
  cancelReduction: boolean;
}
/**
 * Changes to a selection group pending submission.
 */
export interface PendingGroupState {
  id: Guid;
  name: string;
  userQuery: string;
  users: Dict<PendingGroupUserState>;
}

/**
 * Entity data returned from the server.
 */
export interface AccessStateData {
  clients: Dict<ClientWithEligibleUsers | ClientWithStats>;
  items: Dict<RootContentItemWithStats>;
  groups: Dict<SelectionGroupWithAssignedUsers>;
  users: Dict<User>;
  fields: Dict<ReductionField>;
  values: Dict<ReductionFieldValue>;
  contentTypes: Dict<ContentType>;
  publications: Dict<ContentPublicationRequest>;
  publicationQueue: Dict<PublicationQueueDetails>;
  reductions: Dict<ContentReductionTask>;
  reductionQueue: Dict<ReductionQueueDetails>;
}
/**
 * Selected cards.
 */
export interface AccessStateSelected {
  client: Guid;
  item: Guid;
  group: Guid;
}
/**
 * Card attribute collections.
 */
export interface AccessStateCardAttributes {
  client: Dict<CardAttributes>;
  group: Dict<CardAttributes>;
}
/**
 * All state that represents a change pending submission.
 */
export interface AccessStatePending {
  data: PendingDataState;
  statusTries: number;
  isMaster: boolean;
  selections: Dict<{ selected: boolean }>;
  newGroupName: string;
  group: PendingGroupState;
  deleteGroup: Guid;
}
/**
 * All filter state.
 */
export interface AccessStateFilters {
  client: FilterState;
  item: FilterState;
  group: FilterState;
  selections: FilterState;
}
/**
 * All modal state.
 */
export interface AccessStateModals {
  addGroup: ModalState;
  deleteGroup: ModalState;
  invalidate: ModalState;
}

/**
 * All content access admin state.
 */
export interface AccessState {
  data: AccessStateData;
  selected: AccessStateSelected;
  cardAttributes: AccessStateCardAttributes;
  pending: AccessStatePending;
  filters: AccessStateFilters;
  modals: AccessStateModals;
  toastr: toastr.ToastrState;
}

// Create the store and apply saga middleware
const sagaMiddleware = createSagaMiddleware();
export const store = createStore(
  contentAccessAdmin,
  applyMiddleware(sagaMiddleware),
  );
sagaMiddleware.run(sagas);
