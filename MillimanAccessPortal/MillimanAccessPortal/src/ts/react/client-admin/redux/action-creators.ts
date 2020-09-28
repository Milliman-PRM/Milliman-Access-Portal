import {
    createActionCreator, createRequestActionCreator,
} from '../../shared-components/redux/action-creators';
import * as AccessActions from './actions';

// Page actions
export const selectClient =
  createActionCreator<AccessActions.SelectClient>('SELECT_CLIENT');
export const selectUser =
  createActionCreator<AccessActions.SelectUser>('SELECT_USER');
export const setEditStatus =
  createActionCreator<AccessActions.SetEditStatus>('SET_EDIT_STATUS');
export const resetClientDetails =
  createActionCreator<AccessActions.ResetClientDetails>('RESET_CLIENT_DETAILS');

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

// Form Actions
export const clearFormData =
  createActionCreator<AccessActions.ClearFormData>('CLEAR_FORM_DATA');
export const resetFormData =
  createActionCreator<AccessActions.ResetFormData>('RESET_FORM_DATA');
export const setFormFieldValue =
  createActionCreator<AccessActions.SetFormFieldValue>('SET_FORM_FIELD_VALUE');

// Validity Actions
export const resetValidity =
  createActionCreator<AccessActions.ResetValidity>('RESET_VALIDITY');
export const setValidityForField =
  createActionCreator<AccessActions.SetValidityForField>('SET_VALIDITY_FOR_FIELD');

// Data fetches/posts
export const fetchClients =
  createRequestActionCreator<AccessActions.FetchClients>('FETCH_CLIENTS');
export const fetchClientDetails =
  createRequestActionCreator<AccessActions.FetchClientDetails>('FETCH_CLIENT_DETAILS');
export const setUserRoleInClient =
  createRequestActionCreator<AccessActions.SetUserRoleInClient>('SET_USER_ROLE_IN_CLIENT');
export const fetchProfitCenters =
  createRequestActionCreator<AccessActions.FetchProfitCenters>('FETCH_PROFIT_CENTERS');
export const saveNewClient =
  createRequestActionCreator<AccessActions.SaveNewClient>('SAVE_NEW_CLIENT');
export const editClient =
  createRequestActionCreator<AccessActions.EditClient>('EDIT_CLIENT');
export const deleteClient =
  createRequestActionCreator<AccessActions.DeleteClient>('DELETE_CLIENT');
