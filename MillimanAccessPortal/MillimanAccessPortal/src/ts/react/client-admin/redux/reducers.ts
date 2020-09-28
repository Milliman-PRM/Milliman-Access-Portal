import * as _ from 'lodash';
import * as Yup from 'yup';

import { combineReducers } from 'redux';

import { AccessAction, FilterAccessAction } from './actions';
import * as AccessActions from './actions';
import {
  AccessStateBaseFormData, AccessStateData, AccessStateEdit, AccessStateFormData,
  AccessStateSelected, AccessStateValid, PendingDataState,
} from './store';

import { CardAttributes } from '../../shared-components/card/card';
import { createReducerCreator } from '../../shared-components/redux/reducers';
import { Dict, FilterState } from '../../shared-components/redux/store';
import { ClientDetail } from '../../system-admin/interfaces';

const emailRegex = /\S+@\S+\.\S+/;

const _initialPendingData: PendingDataState = {
  clients: false,
  details: false,
};

const initialDetails: ClientDetail = {
  id: null,
  name: '',
  clientCode: '',
  clientContactName: '',
  clientContactTitle: '',
  clientContactEmail: null,
  clientContactPhone: null,
  domainListCountLimit: 0,
  acceptedEmailDomainList: [],
  acceptedEmailAddressExceptionList: [],
  profitCenter: {
    id: '',
    name: '',
    code: '',
    office: '',
  },
  office: '',
  consultantName: '',
  consultantEmail: null,
};

const _initialData: AccessStateData = {
  clients: {},
  profitCenters: [],
  details: initialDetails,
  assignedUsers: [],
};

const _initialFormData: AccessStateBaseFormData = {
  name: '',
  clientCode: '',
  contactName: '',
  contactTitle: '',
  contactEmail: null,
  contactPhone: null,
  domainListCountLimit: 0,
  acceptedEmailDomainList: [],
  acceptedEmailAddressExceptionList: [],
  profitCenterId: '',
  consultantOffice: '',
  consultantName: '',
  consultantEmail: null,
  newUserWelcomeText: '',
  parentClientId: '',
};

const _initialValidation: AccessStateValid = {
  name: { valid: true },
  profitCenter: { valid: true },
  clientContactEmail: { valid: true },
  consultantEmail: { valid: true },
};

const _initialSelected: AccessStateSelected = {
  client: null,
  user: null,
};

const _initialEditStatus: AccessStateEdit = {
  disabled: true,
};

/**
 * Create reducers for a subtree of the redux store
 * @param initialState Subtree of state these handlers can influence
 * @param handlers Actions and their state transformations
 */
const createReducer = createReducerCreator<AccessAction>();

const pending = createReducer<PendingDataState>(_initialPendingData, {
  FETCH_CLIENTS: (state) => ({
    ...state,
    clients: true,
  }),
  FETCH_CLIENT_DETAILS: (state) => ({
    ...state,
    details: true,
  }),
  FETCH_CLIENTS_SUCCEEDED: (state) => ({
    ...state,
    clients: false,
  }),
  FETCH_CLIENT_DETAILS_SUCCEEDED: (state) => ({
    ...state,
    details: false,
  }),
  FETCH_CLIENTS_FAILED: (state) => ({
    ...state,
    clients: false,
  }),
  FETCH_CLIENT_DETAILS_FAILED: (state) => ({
    ...state,
    details: false,
  }),
});

const data = createReducer<AccessStateData>(_initialData, {
  FETCH_CLIENTS_SUCCEEDED: (state, action: AccessActions.FetchClientsSucceeded) => ({
    ...state,
    clients: {
      ...action.response.clients,
    },
  }),
  FETCH_PROFIT_CENTERS_SUCCEEDED: (state, action: AccessActions.FetchProfitCentersSucceeded) => ({
    ...state,
    profitCenters: action.response,
  }),
  RESET_CLIENT_DETAILS: (state) => ({
    ...state,
    details: initialDetails,
  }),
  FETCH_CLIENT_DETAILS_SUCCEEDED: (state, action: AccessActions.FetchClientDetailsSucceeded) => ({
    ...state,
    details: action.response.clientDetail,
    assignedUsers: action.response.assignedUsers,
  }),
  SET_USER_ROLE_IN_CLIENT_SUCCEEDED: (state, action: AccessActions.SetUserRoleInClientSucceeded) => ({
    ...state,
    ...state.assignedUsers.find((u) => u.id === action.response.userId).userRoles = action.response.roles,
  }),
  SAVE_NEW_CLIENT_SUCCEEDED: (state, action: AccessActions.SaveNewClientSucceeded) => ({
    ...state,
    clients: {
      ...action.response.clients,
    },
    details: action.response.newClient,
  }),
  EDIT_CLIENT_SUCCEEDED: (state, action: AccessActions.EditClientSucceeded) => ({
    ...state,
    clients: {
      ...action.response.clients,
    },
  }),
  DELETE_CLIENT_SUCCEEDED: (state, action: AccessActions.DeleteClientSucceeded) => ({
    ...state,
    clients: {
      ...action.response.clients,
    },
  }),
});

const selected = createReducer<AccessStateSelected>(_initialSelected, {
  SELECT_CLIENT: (state, action: AccessActions.SelectClient) => ({
    ...state,
    client: action.id === state.client ? null : action.id,
    user: null,
  }),
  SELECT_USER: (state, action: AccessActions.SelectUser) => ({
    ...state,
    user: action.id === state.user ? null : action.id,
  }),
  SAVE_NEW_CLIENT_SUCCEEDED: (state, action: AccessActions.SaveNewClientSucceeded) => ({
    ...state,
    client: action.response.newClient.id,
    user: null,
  }),
});

const edit = createReducer<AccessStateEdit>(_initialEditStatus, {
  SET_EDIT_STATUS: (state, action: AccessActions.SetEditStatus) => ({
    ...state,
    disabled: action.disabled,
  }),
});

const formData = createReducer<AccessStateBaseFormData>(_initialFormData, {
  CLEAR_FORM_DATA: () => _initialFormData,
  FETCH_CLIENT_DETAILS_SUCCEEDED: (state, action: AccessActions.FetchClientDetailsSucceeded) => ({
    ...state,
    details: action.response.clientDetail,
    id: action.response.clientDetail.id,
    name: action.response.clientDetail.name,
    clientCode: action.response.clientDetail.clientCode,
    contactName: action.response.clientDetail.clientContactName,
    contactTitle: action.response.clientDetail.clientContactTitle,
    contactEmail: action.response.clientDetail.clientContactEmail ?
                  action.response.clientDetail.clientContactEmail : null,
    contactPhone: action.response.clientDetail.clientContactPhone ?
                  action.response.clientDetail.clientContactPhone : null,
    domainListCountLimit: action.response.clientDetail.domainListCountLimit,
    acceptedEmailDomainList: action.response.clientDetail.acceptedEmailDomainList,
    acceptedEmailAddressExceptionList: action.response.clientDetail.acceptedEmailAddressExceptionList,
    profitCenterId: action.response.clientDetail.profitCenter.id,
    consultantOffice: action.response.clientDetail.office,
    consultantName: action.response.clientDetail.consultantName,
    consultantEmail: action.response.clientDetail.consultantEmail ?
                     action.response.clientDetail.consultantEmail : null,
  }),
  SAVE_NEW_CLIENT_SUCCEEDED: (state, action: AccessActions.SaveNewClientSucceeded) => ({
    ...state,
    id: action.response.newClient.id,
    name: action.response.newClient.name,
    clientCode: action.response.newClient.clientCode,
    contactName: action.response.newClient.clientContactName,
    contactTitle: action.response.newClient.clientContactTitle,
    contactEmail: action.response.newClient.clientContactEmail ?
      action.response.newClient.clientContactEmail : null,
    contactPhone: action.response.newClient.clientContactPhone ?
      action.response.newClient.clientContactPhone : null,
    domainListCountLimit: action.response.newClient.domainListCountLimit,
    acceptedEmailDomainList: action.response.newClient.acceptedEmailDomainList,
    acceptedEmailAddressExceptionList: action.response.newClient.acceptedEmailAddressExceptionList,
    profitCenterId: action.response.newClient.profitCenter.id,
    consultantOffice: action.response.newClient.office,
    consultantName: action.response.newClient.consultantName,
    consultantEmail: action.response.newClient.consultantEmail ?
      action.response.newClient.consultantEmail : null,
  }),
  RESET_FORM_DATA: (state, action: AccessActions.ResetFormData) => ({
    ...state,
    id: action.details.id,
    name: action.details.name,
    clientCode: action.details.clientCode,
    contactName: action.details.clientContactName,
    contactTitle: action.details.clientContactTitle,
    contactEmail: action.details.clientContactEmail ? action.details.clientContactEmail : null,
    contactPhone: action.details.clientContactPhone ? action.details.clientContactPhone : null,
    domainListCountLimit: action.details.domainListCountLimit,
    acceptedEmailDomainList: action.details.acceptedEmailDomainList,
    acceptedEmailAddressExceptionList: action.details.acceptedEmailAddressExceptionList,
    profitCenterId: action.details.profitCenter.id,
    consultantOffice: action.details.office,
    consultantName: action.details.consultantName,
    consultantEmail: action.details.consultantEmail ? action.details.consultantEmail : null,
  }),
  SET_FORM_FIELD_VALUE: (state, action: AccessActions.SetFormFieldValue) => ({
    ...state,
    [action.field]: action.value,
  }),
});

const valid = createReducer<AccessStateValid>(_initialValidation, {
  RESET_VALIDITY: () => _initialValidation,
  CHECK_CLIENT_NAME_VALIDITY: (state, action: AccessActions.CheckClientNameValidity) => ({
    ...state,
    name: {
      valid: action.name.trim() ? true : false,
      message: action.name.trim() ? null : 'Client Name is a required field.',
    },
  }),
  CHECK_PROFIT_CENTER_VALIDITY: (state, action: AccessActions.CheckProfitCenterValidity) => ({
    ...state,
    profitCenter: {
      valid: action.profitCenterId ? true : false,
      message: action.profitCenterId ? null : 'Profit Center is a required field.',
    },
  }),
  CHECK_CLIENT_CONTACT_EMAIL_VALIDITY: (state, action: AccessActions.CheckClientContactEmailValidity) => ({
    ...state,
    clientContactEmail: {
      valid: (!action.clientContactEmail.trim() || emailRegex.test(action.clientContactEmail))
        ? true : false,
      message: (!action.clientContactEmail.trim() || emailRegex.test(action.clientContactEmail))
        ? null : 'The Client Contact Email field is not a valid e-mail address.',
    },
  }),
  CHECK_CONSULTANT_EMAIL_VALIDITY: (state, action: AccessActions.CheckConsultantEmailValidity) => ({
    ...state,
      consultantEmail: {
      valid: (!action.consultantEmail.trim() || emailRegex.test(action.consultantEmail))
        ? true : false,
      message: (!action.consultantEmail.trim() || emailRegex.test(action.consultantEmail))
        ? null : 'The Consultant Email field is not a valid e-mail address.',
    },
  }),
});

const userCardAttributes = createReducer<Dict<CardAttributes>>({},
  {
    SET_EXPANDED_USER: (state, action: AccessActions.SetExpandedUser) => ({
      ...state,
      [action.id]: {
        expanded: true,
      },
    }),
    SET_COLLAPSED_USER: (state, action: AccessActions.SetCollapsedUser) => ({
      ...state,
      [action.id]: {
        expanded: false,
      },
    }),
    SET_ALL_EXPANDED_USER: (state) =>
      _.mapValues(state, (group) => ({
        ...group,
        expanded: true,
      })),
    SET_ALL_COLLAPSED_USER: (state) =>
      _.mapValues(state, (group) => ({
        ...group,
        expanded: false,
      })),
  },
);

/**
 * Create a reducer for a filter
 * @param actionType Single filter action
 */
const createFilterReducer = (actionType: FilterAccessAction['type']) =>
  createReducer({ text: '' }, {
    [actionType]: (state: FilterState, action: FilterAccessAction) => ({
      ...state,
      text: action.text,
    }),
  });

const cardAttributes = combineReducers({
  user: userCardAttributes,
});
const filters = combineReducers({
  client: createFilterReducer('SET_FILTER_TEXT_CLIENT'),
  user: createFilterReducer('SET_FILTER_TEXT_USER'),
});

export const clientAdmin = combineReducers({
  data,
  cardAttributes,
  selected,
  edit,
  formData,
  valid,
  filters,
  pending,
});
