import { Dict } from "../../shared-components/redux/store";
import { ClientWithEligibleUsers, ClientWithStats } from "../../models";
import createSagaMiddleware from 'redux-saga';
import { createStore, applyMiddleware } from "redux";
import sagas from './sagas';
import { composeWithDevTools } from "redux-devtools-extension";
import { clientAdmin } from "./reducers";

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
