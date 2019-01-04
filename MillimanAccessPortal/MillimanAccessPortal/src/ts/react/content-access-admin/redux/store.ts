import { applyMiddleware, createStore } from 'redux';
import createSagaMiddleware from 'redux-saga';

import {
  ClientWithEligibleUsers, ContentPublicationRequest, ContentReductionTask, ContentType, Guid,
  PublicationQueueDetails, ReductionField, ReductionFieldValue, ReductionQueueDetails,
  RootContentItemWithStats, SelectionGroupWithAssignedUsers, User,
} from '../../models';
import { CardAttributes } from '../../shared-components/card/card';
import { contentAccessAdmin } from './reducers';
import sagas from './sagas';

export interface Dict<T> {
  [key: string]: T;
}

export interface FilterState {
  text: string;
}
export interface ModalState {
  isOpen: boolean;
}
export interface PendingGroupUserState {
  assigned: boolean;
}
export interface PendingDataState {
  clients: boolean;
  items: boolean;
  groups: boolean;
  selections: boolean;
  createGroup: boolean;
  deleteGroup: boolean;
  suspendGroup: boolean;
}
export interface PendingGroupState {
  id: Guid;
  name: string;
  userQuery: string;
  users: Map<Guid, PendingGroupUserState>;
}

export interface AccessStateData {
  clients: Dict<ClientWithEligibleUsers>;
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
export interface AccessStateSelected {
  client: Guid;
  item: Guid;
  group: Guid;
}
export interface AccessStateCardAttributes {
  group: Map<Guid, CardAttributes>;
}
export interface AccessStatePending {
  data: PendingDataState;
  isMaster: boolean;
  selections: Map<Guid, { selected: boolean }>;
  newGroupName: string;
  group: PendingGroupState;
}
export interface AccessStateFilters {
  client: FilterState;
  item: FilterState;
  group: FilterState;
  selections: FilterState;
}
export interface AccessStateModals {
  addGroup: ModalState;
}

export interface AccessState {
  data: AccessStateData;
  selected: AccessStateSelected;
  cardAttributes: AccessStateCardAttributes;
  pending: AccessStatePending;
  filters: AccessStateFilters;
  modals: AccessStateModals;
}

const sagaMiddleware = createSagaMiddleware();
export const store = createStore(
  contentAccessAdmin,
  applyMiddleware(sagaMiddleware),
  );
sagaMiddleware.run(sagas);
