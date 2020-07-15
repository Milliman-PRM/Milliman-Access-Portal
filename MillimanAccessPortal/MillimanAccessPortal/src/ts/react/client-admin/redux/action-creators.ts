import {
    createActionCreator, createRequestActionCreator,
} from '../../shared-components/redux/action-creators';
import * as AccessActions from './actions';

// Page actions
export const selectClient =
  createActionCreator<AccessActions.SelectClient>('SELECT_CLIENT');

// Filter actions
export const setFilterTextClient =
  createActionCreator<AccessActions.SetFilterTextClient>('SET_FILTER_TEXT_CLIENT');

// Data fetches
export const fetchClients =
  createRequestActionCreator<AccessActions.FetchClients>('FETCH_CLIENTS');
export const fetchClientDetails =
  createRequestActionCreator<AccessActions.FetchClientDetails>('FETCH_CLIENT_DETAILS');
