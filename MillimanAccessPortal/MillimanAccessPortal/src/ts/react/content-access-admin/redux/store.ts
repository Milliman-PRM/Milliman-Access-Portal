import { createStore } from 'redux';

import {
  Client, ContentPublicationRequest, ContentReductionTask, Guid, PublicationQueueDetails,
  ReductionField, ReductionFieldValue, ReductionQueueDetails, RootContentItem, SelectionGroup, User,
} from '../../models';
import { CardAttributes } from '../../shared-components/card';
import { contentAccessAdmin } from './reducers';

export interface ContentAccessAdminState {
  data: {
    clients: Client[];
    items: RootContentItem[];
    groups: SelectionGroup[];
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
  };
  itemPanel: {
    cards: {
      [id: string]: CardAttributes;
    };
    selectedCard: Guid;
  };
  groupPanel: {
    cards: {
      [id: string]: CardAttributes;
    };
    selectedCard: Guid;
  };
  selectionsPanel: {
    isMaster: boolean;
    values: {
      [id: string]: boolean;
    };
  };
}

export const store = createStore(contentAccessAdmin);
