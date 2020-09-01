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
  clientReviewGracePeriodDays: number;
}

/**
 * Flags indicating whether the page is waiting on new data for an entity type.
 */
export interface PendingDataState {
  clients: boolean;
}

/**
 * Entity data returned from the server.
 */
export interface AccessReviewStateData {
  globalData: AccessReviewGlobalData;
  clients: Dict<ClientWithReviewDate>;
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
 * All state that represents a change pending submission.
 */
export interface AccessReviewStatePending {
  data: PendingDataState;
  statusTries: number;
  isMaster: boolean;
  selections: Dict<{ selected: boolean }>;
  newGroupName: string;
  deleteGroup: Guid;
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
