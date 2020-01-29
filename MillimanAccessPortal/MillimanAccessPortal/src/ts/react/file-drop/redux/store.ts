import * as toastr from 'react-redux-toastr';
import { applyMiddleware, createStore } from 'redux';
import { composeWithDevTools } from 'redux-devtools-extension';
import createSagaMiddleware from 'redux-saga';

import { ClientWithStats, Guid } from '../../models';
import { CardAttributes } from '../../shared-components/card/card';
import { Dict, FilterState, ModalState } from '../../shared-components/redux/store';
import { fileDropReducerState } from './reducers';
import sagas from './sagas';

/**
 * Flags indicating whether the page is waiting on new data for an entity type.
 */
export interface FileDropPendingReturnState {
  globalData: boolean;
  clients: boolean;
}

/*
 * All state that represents the user interactions with the page
 */
export interface FileDropPendingState {
  async: FileDropPendingReturnState;
  statusTries: number;
}

/**
 * Entity data returned from the server.
 */
export interface FileDropDataState {
  clients: Dict<ClientWithStats>;
}

/**
 * Selected cards.
 */
export interface FileDropSelectedState {
  client: Guid;
}

/**
 * Card attribute collections.
 */
export interface FileDropCardAttributesState {
  client: Dict<CardAttributes>;
  item: Dict<CardAttributes>;
}

/**
 * All filter state.
 */
export interface FileDropFilterState {
  client: FilterState;
}

/**
 * Top-Level File Drop state.
 */
export interface FileDropState {
  data: FileDropDataState;
  selected: FileDropSelectedState;
  cardAttributes: FileDropCardAttributesState;
  pending: FileDropPendingState;
  filters: FileDropFilterState;
  toastr: toastr.ToastrState;
}

// Create the store and apply saga middleware
const sagaMiddleware = createSagaMiddleware();
export const store = createStore(
  fileDropReducerState,
  composeWithDevTools(
    applyMiddleware(sagaMiddleware),
  ));
sagaMiddleware.run(sagas);
