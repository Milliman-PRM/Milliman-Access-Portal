import * as _ from 'lodash';
import { reducer as toastrReducer } from 'react-redux-toastr';
import { combineReducers } from 'redux';

import { CardAttributes } from '../../shared-components/card/card';
import { createReducerCreator } from '../../shared-components/redux/reducers';
import { Dict, FilterState } from '../../shared-components/redux/store';
import * as AccessReviewActions from './actions';
import { AccessReviewAction, FilterAccessReviewAction } from './actions';
import {
  AccessReviewStateData, AccessReviewStateSelected, ClientAccessReviewProgress,
  ClientAccessReviewProgressEnum, PendingDataState,
} from './store';

const _initialData: AccessReviewStateData = {
  globalData: {
    clientReviewEarlyWarningDays: null,
    clientReviewGracePeriodDays: null,
  },
  clients: {},
  selectedClientSummary: null,
  clientAccessReview: null,
};

const _initialPendingData: PendingDataState = {
  clients: false,
  clientSummary: false,
  clientAccessReview: false,
  approveAccessReview: false,
};

/**
 * Create reducers for a subtree of the redux store
 */
const createReducer = createReducerCreator<AccessReviewAction>();

/**
 * Create a reducer for a filter
 */
const createFilterReducer = (actionType: FilterAccessReviewAction['type']) =>
  createReducer({ text: '' }, {
    [actionType]: (state: FilterState, action: FilterAccessReviewAction) => ({
      ...state,
      text: action.text,
    }),
  });

/**
 * Create a reducer for a modal
 */
// const createModalReducer = (
//   openActions: Array<OpenAccessReviewAction['type']>,
//   closeActions: Array<AccessReviewAction['type']>,
// ) => {
//   const handlers: Handlers<ModalState, any> = {};
//   openActions.forEach((action) => {
//     handlers[action] = (state) => ({
//       ...state,
//       isOpen: true,
//     });
//   });
//   closeActions.forEach((action) => {
//     handlers[action] = (state) => ({
//       ...state,
//       isOpen: false,
//     });
//   });
//   return createReducer<ModalState>({ isOpen: false }, handlers);
// };

const clientCardAttributes = createReducer<Dict<CardAttributes>>({},
  {
    FETCH_CLIENTS_SUCCEEDED: (__, action: AccessReviewActions.FetchClientsSucceeded) => ({
      ..._.mapValues(action.response.clients, () => ({ disabled: false })),
      ..._.mapValues(action.response.parentClients, () => ({ disabled: true })),
    }),
    APPROVE_CLIENT_ACCESS_REVIEW_SUCCEEDED: (__, action: AccessReviewActions.ApproveClientAccessReviewSucceeded) => ({
      ..._.mapValues(action.response.clients, () => ({ disabled: false })),
      ..._.mapValues(action.response.parentClients, () => ({ disabled: true })),
    }),
  },
);

const pendingData = createReducer<PendingDataState>(_initialPendingData, {
  FETCH_CLIENTS: (state) => ({
    ...state,
    clients: true,
  }),
  FETCH_CLIENTS_SUCCEEDED: (state) => ({
    ...state,
    clients: false,
  }),
  FETCH_CLIENTS_FAILED: (state) => ({
    ...state,
    clients: false,
  }),
  FETCH_CLIENT_SUMMARY: (state) => ({
    ...state,
    clientSummary: true,
  }),
  FETCH_CLIENT_SUMMARY_SUCCEEDED: (state) => ({
    ...state,
    clientSummary: false,
  }),
  FETCH_CLIENT_SUMMARY_FAILED: (state) => ({
    ...state,
    clientSummary: false,
  }),
  FETCH_CLIENT_REVIEW: (state) => ({
    ...state,
    clientAccessReview: true,
  }),
  FETCH_CLIENT_REVIEW_SUCCEEDED: (state) => ({
    ...state,
    clientAccessReview: false,
  }),
  FETCH_CLIENT_REVIEW_FAILED: (state) => ({
    ...state,
    clientAccessReview: false,
  }),
  APPROVE_CLIENT_ACCESS_REVIEW: (state) => ({
    ...state,
    approveAccessReview: true,
  }),
  APPROVE_CLIENT_ACCESS_REVIEW_SUCCEEDED: (state) => ({
    ...state,
    approveAccessReview: false,
  }),
  APPROVE_CLIENT_ACCESS_REVIEW_FAILED: (state) => ({
    ...state,
    approveAccessReview: false,
  }),
});

const reviewProgress = createReducer<ClientAccessReviewProgress>({
    step: 0,
    contentItemConfirmations: null,
    fileDropConfirmations: null,
}, {
  FETCH_CLIENT_REVIEW_SUCCEEDED: (_state, action: AccessReviewActions.FetchClientReviewSucceeded) => {
    const contentItemConfirmations: Dict<boolean> = {};
    action.response.contentItems.map((ci) => {
      contentItemConfirmations[ci.id] = false;
    });
    const fileDropConfirmations: Dict<boolean> = {};
    action.response.fileDrops.map((fd) => {
      fileDropConfirmations[fd.id] = false;
    });
    return {
      step: 0,
      contentItemConfirmations,
      fileDropConfirmations,
    };
  },
  GO_TO_NEXT_ACCESS_REVIEW_STEP: (state) => ({
    ...state,
    step: (state.step < ClientAccessReviewProgressEnum.attestations) ? state.step + 1 : state.step,
  }),
  GO_TO_PREVIOUS_ACCESS_REVIEW_STEP: (state) => ({
    ...state,
    step: (state.step > ClientAccessReviewProgressEnum.clientReview) ? state.step - 1 : state.step,
  }),
  TOGGLE_CONTENT_ITEM_REVIEW_STATUS: (state, action: AccessReviewActions.ToggleContentItemReviewStatus) => ({
    ...state,
    contentItemConfirmations: {
      ...state.contentItemConfirmations,
      [action.contentItemId]: !state.contentItemConfirmations[action.contentItemId],
    },
  }),
  TOGGLE_FILE_DROP_REVIEW_STATUS: (state, action: AccessReviewActions.ToggleFileDropReviewStatus) => ({
    ...state,
    fileDropConfirmations: {
      ...state.fileDropConfirmations,
      [action.fileDropId]: !state.fileDropConfirmations[action.fileDropId],
    },
  }),
  CANCEL_CLIENT_ACCESS_REVIEW: () => ({
    step: 0,
    contentItemConfirmations: null,
    fileDropConfirmations: null,
  }),
  APPROVE_CLIENT_ACCESS_REVIEW_SUCCEEDED: () => ({
    step: 0,
    contentItemConfirmations: null,
    fileDropConfirmations: null,
  }),
});

const data = createReducer<AccessReviewStateData>(_initialData, {
  FETCH_GLOBAL_DATA_SUCCEEDED: (state, action: AccessReviewActions.FetchGlobalDataSucceeded) => ({
    ...state,
    globalData: action.response,
  }),
  FETCH_CLIENTS_SUCCEEDED: (state, action: AccessReviewActions.FetchClientsSucceeded) => ({
    ...state,
    clients: {
      ...action.response.clients,
      ...action.response.parentClients,
    },
  }),
  FETCH_CLIENT_SUMMARY_SUCCEEDED: (state, action: AccessReviewActions.FetchClientSummarySucceeded) => ({
    ...state,
    selectedClientSummary: action.response,
    clientAccessReview: null,
  }),
  FETCH_CLIENT_REVIEW_SUCCEEDED: (state, action: AccessReviewActions.FetchClientReviewSucceeded) => ({
    ...state,
    clientAccessReview: action.response,
  }),
  CANCEL_CLIENT_ACCESS_REVIEW: (state) => ({
    ...state,
    clientAccessReview: null,
  }),
  APPROVE_CLIENT_ACCESS_REVIEW_SUCCEEDED: (state, action: AccessReviewActions.ApproveClientAccessReviewSucceeded) => ({
    ...state,
    clients: {
      ...action.response.clients,
      ...action.response.parentClients,
    },
  }),
});

const selected = createReducer<AccessReviewStateSelected>(
  {
    client: null,
  },
  {
    SELECT_CLIENT: (state, action: AccessReviewActions.SelectClient) => ({
      client: action.id === state.client ? null : action.id,
    }),
  },
);

const cardAttributes = combineReducers({
  client: clientCardAttributes,
});

const pending = combineReducers({
  data: pendingData,
  clientAccessReviewProgress: reviewProgress,
});

const filters = combineReducers({
  client: createFilterReducer('SET_FILTER_TEXT_CLIENT'),
});

// const modals = combineReducers({
//   leaveActiveReview: createModalReducer([ 'OPEN_ADD_GROUP_MODAL' ], [
//     'CLOSE_ADD_GROUP_MODAL',
//     'CREATE_GROUP_SUCCEEDED',
//     'CREATE_GROUP_FAILED',
//   ]),
// });

export const clientAccessReview = combineReducers({
  data,
  selected,
  cardAttributes,
  pending,
  filters,
  // modals,
  toastr: toastrReducer,
});
