import { applyMiddleware, createStore } from 'redux';
import { composeWithDevTools } from 'redux-devtools-extension';
import createSagaMiddleware from 'redux-saga';

import { clientAdmin } from './reducers';
import sagas from './sagas';

import { ClientWithEligibleUsers, ClientWithStats, Guid } from '../../models';
import { Dict, FilterState } from '../../shared-components/redux/store';
import { ClientDetail } from '../../system-admin/interfaces';

/**
 * Entity data returned from the server.
 */
export interface AccessStateData {
  clients: Dict<ClientWithEligibleUsers | ClientWithStats>;
  details: ClientDetail;
}

export interface AccessStateSelected {
  client: Guid;
}

/**
 * All filter state.
 */
export interface AccessStateFilters {
  client: FilterState;
}

export interface AccessState {
  data: AccessStateData;
  selected: AccessStateSelected;
  filters: AccessStateFilters;
}

// Create the store and apply saga middleware
const sagaMiddleware = createSagaMiddleware();
export const store = createStore(
  clientAdmin,
  composeWithDevTools(
    applyMiddleware(sagaMiddleware),
  ));
sagaMiddleware.run(sagas);
