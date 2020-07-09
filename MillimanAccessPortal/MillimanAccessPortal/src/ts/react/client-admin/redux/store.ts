import { applyMiddleware, createStore } from 'redux';
import { composeWithDevTools } from 'redux-devtools-extension';
import createSagaMiddleware from 'redux-saga';

import { clientAdmin } from './reducers';
import sagas from './sagas';

import { ClientWithEligibleUsers, ClientWithStats } from '../../models';
import { Dict } from '../../shared-components/redux/store';

/**
 * Entity data returned from the server.
 */
export interface AccessStateData {
  clients: Dict<ClientWithEligibleUsers | ClientWithStats>;
}

export interface AccessState {
  data: AccessStateData;
}

// Create the store and apply saga middleware
const sagaMiddleware = createSagaMiddleware();
export const store = createStore(
  clientAdmin,
  composeWithDevTools(
    applyMiddleware(sagaMiddleware),
  ));
sagaMiddleware.run(sagas);
