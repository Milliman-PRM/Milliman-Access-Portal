import { createStore } from 'redux';

import {
  Client, Guid, ReductionField, ReductionFieldValue, RootContentItem, SelectionGroup, User,
} from '../../models';
import { contentAccessAdmin } from './reducers';

export interface ContentAccessAdminState {
  data: {
    clients: Client[];
    items: RootContentItem[];
    groups: SelectionGroup[];
    users: User[];
    fields: ReductionField[];
    values: ReductionFieldValue[];
  };
  clientPanel: {
    cards: {
      [id: string]: {
        expanded: boolean;
        profitCenterModalOpen: boolean;
      };
    };
    selectedCard: Guid;
  };
  itemPanel: {
    cards: {
      [id: string]: {
        expanded: boolean;
        profitCenterModalOpen: boolean;
      };
    };
    selectedCard: Guid;
  };
  groupPanel: {
    cards: {
      [id: string]: {
        expanded: boolean;
        profitCenterModalOpen: boolean;
      };
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
