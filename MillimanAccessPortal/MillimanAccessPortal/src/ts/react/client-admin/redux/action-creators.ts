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

// Form Actions
export const clearFormData =
  createActionCreator<AccessActions.ClearFormData>('CLEAR_FORM_DATA');
export const setFormData =
  createActionCreator<AccessActions.SetFormData>('SET_FORM_DATA');
export const setClientName =
  createActionCreator<AccessActions.SetClientName>('SET_CLIENT_NAME');
export const setClientCode =
  createActionCreator<AccessActions.SetClientCode>('SET_CLIENT_CODE');
export const setClientContactName =
  createActionCreator<AccessActions.SetClientContactName>('SET_CLIENT_CONTACT_NAME');
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

// Data fetches/posts
export const fetchClients =
  createRequestActionCreator<AccessActions.FetchClients>('FETCH_CLIENTS');
export const fetchClientDetails =
  createRequestActionCreator<AccessActions.FetchClientDetails>('FETCH_CLIENT_DETAILS');
export const setUserRoleInClient =
  createRequestActionCreator<AccessActions.SetUserRoleInClient>('SET_USER_ROLE_IN_CLIENT');
export const saveNewClient =
  createRequestActionCreator<AccessActions.SaveNewClient>('SAVE_NEW_CLIENT');
