import * as AccessActions from './actions';

import {
    createActionCreator, createRequestActionCreator,
} from '../../shared-components/redux/action-creators';

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
export const setClientName =
  createActionCreator<AccessActions.SetClientName>('SET_CLIENT_NAME');
export const setClientCode =
  createActionCreator<AccessActions.SetClientCode>('SET_CLIENT_CODE');
export const setClientContactName =
  createActionCreator<AccessActions.SetClientContactName>('SET_CLIENT_CONTACT_NAME');
export const setClientContactTitle =
  createActionCreator<AccessActions.SetClientContactTitle>('SET_CLIENT_CONTACT_TITLE');
export const setClientContactEmail =
  createActionCreator<AccessActions.SetClientContactEmail>('SET_CLIENT_CONTACT_EMAIL');
export const setClientContactPhone =
  createActionCreator<AccessActions.SetClientContactPhone>('SET_CLIENT_CONTACT_PHONE');
export const setDomainListCountLimit =
  createActionCreator<AccessActions.SetDomainListCountLimit>('SET_DOMAIN_LIST_COUNT_LIMIT');
export const setAcceptedEmailDomainList =
  createActionCreator<AccessActions.SetAcceptedEmailDomainList>('SET_ACCEPTED_EMAIL_DOMAIN_LIST');
export const setAcceptedEmailAddressExceptionList =
  createActionCreator<AccessActions.SetAcceptedEmailAddressExceptionList>('SET_ACCEPTED_EMAIL_ADDRESS_EXCEPTION_LIST');
export const setProfitCenter =
  createActionCreator<AccessActions.SetProfitCenter>('SET_PROFIT_CENTER');
export const setOffice =
  createActionCreator<AccessActions.SetOffice>('SET_OFFICE');
export const setConsultantName =
  createActionCreator<AccessActions.SetConsultantName>('SET_CONSULTANT_NAME');
export const setConsultantEmail =
  createActionCreator<AccessActions.SetConsultantEmail>('SET_CONSULTANT_EMAIL');

// Modal Actions
export const openDeleteClientModal =
  createActionCreator<AccessActions.OpenDeleteClientModal>('OPEN_DELETE_CLIENT_MODAL');
export const closeDeleteClientModal =
  createActionCreator<AccessActions.CloseDeleteClientModal>('CLOSE_DELETE_CLIENT_MODAL');
export const openDeleteClientConfirmationModal =
  createActionCreator<AccessActions.OpenDeleteClientConfirmationModal>('OPEN_DELETE_CLIENT_CONFIRMATION_MODAL');
export const closeDeleteClientConfirmationModal =
  createActionCreator<AccessActions.CloseDeleteClientConfirmationModal>('CLOSE_DELETE_CLIENT_CONFIRMATION_MODAL');
export const openCreateClientUserModal =
  createActionCreator<AccessActions.OpenCreateClientUserModal>('OPEN_CREATE_CLIENT_USER_MODAL');
export const closeCreateClientUserModal =
  createActionCreator<AccessActions.CloseCreateClientUserModal>('CLOSE_CREATE_CLIENT_USER_MODAL');
export const setCreateClientUserModalEmail =
  createActionCreator<AccessActions.SetCreateClientUserModalEmail>('SET_CREATE_CLIENT_USER_EMAIL');

// Validity Actions
export const resetValidity =
  createActionCreator<AccessActions.ResetValidity>('RESET_VALIDITY');
export const checkClientNameValidity =
  createActionCreator<AccessActions.CheckClientNameValidity>('CHECK_CLIENT_NAME_VALIDITY');
export const checkProfitCenterValidity =
  createActionCreator<AccessActions.CheckProfitCenterValidity>('CHECK_PROFIT_CENTER_VALIDITY');
export const checkContactEmailValidity =
  createActionCreator<AccessActions.CheckClientContactEmailValidity>('CHECK_CLIENT_CONTACT_EMAIL_VALIDITY');
export const checkConsultantEmailValidity =
  createActionCreator<AccessActions.CheckConsultantEmailValidity>('CHECK_CONSULTANT_EMAIL_VALIDITY');

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
export const saveNewClientUser =
  createRequestActionCreator<AccessActions.SaveNewClientUser>('SAVE_NEW_CLIENT_USER');
