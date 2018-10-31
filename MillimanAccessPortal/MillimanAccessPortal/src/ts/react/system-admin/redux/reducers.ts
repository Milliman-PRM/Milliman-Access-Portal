import { SystemAdminColumn, SystemAdminState } from '../system-admin';
import { SystemAdminAction, TextAction } from './actions';

const initialState = {
  data: {
    primaryEntities: null,
    secondaryEntities: null,
    primaryDetail: null,
    secondaryDetail: null,
  },
  primaryPanel: {
    selected: {
      column: SystemAdminColumn.USER,
      card: null,
    },
    cards: null,
    filter: {
      text: '',
    },
    createModal: {
      open: false,
    },
  },
  secondaryPanel: {
    selected: {
      column: null,
      card: null,
    },
    cards: null,
    filter: {
      text: '',
    },
    createModal: {
      open: false,
    },
  },
};

// reducers
export function setPrimaryFilterTextReducer(state: SystemAdminState = initialState, action: TextAction) {
  switch (action.type) {
    case SystemAdminAction.SET_FILTER_TEXT_PRIMARY:
      return {
        ...state,
        primaryPanel: {
          ...state.primaryPanel,
          filter: { text: action.text },
        },
      };
    default:
      return state;
  }
}

// selectors
export function getPrimaryFilterText(state: SystemAdminState = initialState) {
  return state.primaryPanel.filter.text;
}
