import * as _ from 'lodash';
import { reducer as toastrReducer } from 'react-redux-toastr';
import { combineReducers } from 'redux';

import { CardAttributes } from '../../shared-components/card/card';
import { createReducerCreator } from '../../shared-components/redux/reducers';
import { Dict, FilterState } from '../../shared-components/redux/store';
import * as AccessReviewActions from './actions';
import { AccessReviewAction, FilterAccessReviewAction } from './actions';
import {
  AccessReviewStateData, AccessReviewStateSelected, ClientAccessReviewProgress, PendingDataState,
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
    FETCH_CLIENTS_SUCCEEDED: (__, { response }: AccessReviewActions.FetchClientsSucceeded) => ({
      ..._.mapValues(response.clients, () => ({ disabled: false })),
      ..._.mapValues(response.parentClients, () => ({ disabled: true })),
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
});

const reviewProgress = createReducer<ClientAccessReviewProgress>(0, {
  FETCH_CLIENT_SUMMARY_SUCCEEDED: () => 0,
  GO_TO_NEXT_ACCESS_REVIEW_STEP: (state) => (state < ClientAccessReviewProgress.attestations) ? state + 1 : state,
  GO_TO_PREVIOUS_ACCESS_REVIEW_STEP: (state) => (state > ClientAccessReviewProgress.clientReview) ? state - 1 : state,
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
