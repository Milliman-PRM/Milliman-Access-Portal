import {
    createActionCreator, createRequestActionCreator,
} from '../../shared-components/redux/action-creators';
import * as AccessActions from './actions';

// Page actions
export const selectClient =
  createActionCreator<AccessActions.SelectClient>('SELECT_CLIENT');
export const selectUser =
  createActionCreator<AccessActions.SelectUser>('SELECT_USER');

// Collapse/expand actions
export const setExpandedUser =
  createActionCreator<AccessActions.SetExpandedUser>('SET_EXPANDED_USER');
export const setCollapsedUser =
  createActionCreator<AccessActions.SetCollapsedUser>('SET_COLLAPSED_USER');
export const setAllExpandedUser =
  createActionCreator<AccessActions.SetAllExpandedUser>('SET_ALL_EXPANDED_USER');
export const setAllCollapsedUser =
  createActionCreator<AccessActions.SetAllCollapsedUser>('SET_ALL_COLLAPSED_USER');

// Filter actions
export const setFilterTextClient =
  createActionCreator<AccessActions.SetFilterTextClient>('SET_FILTER_TEXT_CLIENT');
export const setFilterTextUser =
  createActionCreator<AccessActions.SetFilterTextUser>('SET_FILTER_TEXT_USER');

// Data fetches
export const fetchClients =
  createRequestActionCreator<AccessActions.FetchClients>('FETCH_CLIENTS');
export const fetchClientDetails =
  createRequestActionCreator<AccessActions.FetchClientDetails>('FETCH_CLIENT_DETAILS');
