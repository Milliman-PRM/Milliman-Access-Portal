import { createStore } from 'redux';

import {
  ClientWithEligibleUsers, ContentPublicationRequest, ContentReductionTask, Guid,
  PublicationQueueDetails, ReductionField, ReductionFieldValue, ReductionQueueDetails,
  RootContentItem, SelectionGroupWithAssignedUsers, User,
} from '../../models';
import { CardAttributes } from '../../shared-components/card/card';
import { contentAccessAdmin } from './reducers';

export interface ContentAccessAdminState {
  data: {
    clients: ClientWithEligibleUsers[];
    items: RootContentItem[];
    groups: SelectionGroupWithAssignedUsers[];
    users: User[];
    fields: ReductionField[];
    values: ReductionFieldValue[];
    publications: ContentPublicationRequest[];
    publicationQueue: PublicationQueueDetails[];
    reductions: ContentReductionTask[];
    reductionQueue: ReductionQueueDetails[];
  };
  clientPanel: {
    selectedCard: Guid;
    filterText: string;
  };
  itemPanel: {
    cards: {
      [id: string]: CardAttributes;
    };
    selectedCard: Guid;
    filterText: string;
  };
  groupPanel: {
    cards: {
      [id: string]: CardAttributes;
    };
    selectedCard: Guid;
    filterText: string;
  };
  selectionsPanel: {
    isMaster: boolean;
    values: {
      [id: string]: boolean;
    };
    filterText: string;
  };
}

export const store = createStore(contentAccessAdmin);
