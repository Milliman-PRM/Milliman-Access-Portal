import { createStore } from 'redux';

import {
  ClientWithEligibleUsers, ContentPublicationRequest, ContentReductionTask, ContentType, Guid,
  PublicationQueueDetails, ReductionField, ReductionFieldValue, ReductionQueueDetails,
  RootContentItem, SelectionGroupWithAssignedUsers, User,
} from '../../models';
import { CardAttributes } from '../../shared-components/card/card';
import { contentAccessAdmin } from './reducers';

export interface FilterState {
  text: string;
}
export interface ModalState {
  isOpen: boolean;
}
export interface PendingGroupUserState {
  assigned: boolean;
}
export interface PendingGroupState {
  id: Guid;
  name: string;
  userQuery: string;
  users: Map<Guid, PendingGroupUserState>;
}

export interface AccessStateData {
  clients: ClientWithEligibleUsers[];
  items: RootContentItem[];
  groups: SelectionGroupWithAssignedUsers[];
  users: User[];
  fields: ReductionField[];
  values: ReductionFieldValue[];
  contentTypes: ContentType[];
  publications: ContentPublicationRequest[];
  publicationQueue: PublicationQueueDetails[];
  reductions: ContentReductionTask[];
  reductionQueue: ReductionQueueDetails[];
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
  isMaster: boolean;
  selections: Guid[];
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

export const store = createStore(contentAccessAdmin);
