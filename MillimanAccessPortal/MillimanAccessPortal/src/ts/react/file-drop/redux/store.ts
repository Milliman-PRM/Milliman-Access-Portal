import * as toastr from 'react-redux-toastr';
import { applyMiddleware, createStore } from 'redux';
import { composeWithDevTools } from 'redux-devtools-extension';
import createSagaMiddleware from 'redux-saga';

import { ClientWithStats, Guid } from '../../models';
import { CardAttributes } from '../../shared-components/card/card';
import { Dict, FilterState } from '../../shared-components/redux/store';
import { fileDropReducerState } from './reducers';
import sagas from './sagas';

// ~~~~~~~~~~~~~~~~~~~~
// Define State Objects
// ~~~~~~~~~~~~~~~~~~~~

/** Flags indicating whether the page is waiting on new data for an entity type */
export interface FileDropPendingReturnState {
  globalData: boolean;
  clients: boolean;
}

/** All state that represents the user interactions with the page */
export interface FileDropPendingState {
  async: FileDropPendingReturnState;
  statusTries: number;
}

/** State representing user-selected entities */
export interface FileDropSelectedState {
  client: Guid;
}

/** State representing raw (unaltered) data returned from the server */
export interface FileDropDataState {
  clients: Dict<ClientWithStats>;
}

/** State representing entity Card attribute collections */
export interface FileDropCardAttributesState {
  client: Dict<CardAttributes>;
}

/** State representing filter strings */
export interface FileDropFilterState {
  client: FilterState;
}

/** Top-Level File Drop state */
export interface FileDropState {
  pending: FileDropPendingState;
  selected: FileDropSelectedState;
  cardAttributes: FileDropCardAttributesState;
  filters: FileDropFilterState;
  data: FileDropDataState;
  toastr: toastr.ToastrState;
}

// ~~~~~~~~~~~~~~~~~~~~~~~
// Instantiate Redux Store
// ~~~~~~~~~~~~~~~~~~~~~~~

const sagaMiddleware = createSagaMiddleware();

/** File Drop Redux Store with Saga Middleware */
export const store = createStore(
  fileDropReducerState,
  composeWithDevTools(
    applyMiddleware(sagaMiddleware),
  ));
sagaMiddleware.run(sagas);
