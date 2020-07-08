import { AccessStateData } from "./store";
import { combineReducers } from "redux";
import { createReducerCreator } from "../../shared-components/redux/reducers";
import { AccessAction } from './actions';
import * as AccessActions from './actions';

const _initialState: AccessStateData = {
  clients: {},
};

/**
 * Create reducers for a subtree of the redux store
 * @param initialState Subtree of state these handlers can influence
 * @param handlers Actions and their state transformations
 */
const createReducer = createReducerCreator<AccessAction>();

const data = createReducer<AccessStateData>(_initialState, {
  FETCH_CLIENTS_SUCCEEDED: (state, action: AccessActions.FetchClientsSucceeded) => ({
    ...state,
    clients: {
      ...action.response.clients,
    },
  }),
});

export const clientAdmin = combineReducers({
  data
});