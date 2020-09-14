import * as _ from 'lodash';

import { combineReducers } from 'redux';

import { AccessAction, FilterAccessAction } from './actions';
import * as AccessActions from './actions';
import { AccessStateData, AccessStateEdit, AccessStateFormData, AccessStateSelected, PendingDataState } from './store';

import { CardAttributes } from '../../shared-components/card/card';
import { createReducerCreator } from '../../shared-components/redux/reducers';
import { Dict, FilterState } from '../../shared-components/redux/store';

const _initialPendingData: PendingDataState = {
  clients: false,
  details: false,
};

const _initialData: AccessStateData = {
  clients: {},
  profitCenters: [],
  details: {
    id: null,
    name: '',
    clientCode: '',
    clientContactName: '',
    clientContactTitle: '',
    clientContactEmail: '',
    clientContactPhone: '',
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
    consultantEmail: '',
  },
  assignedUsers: [],
};

const _initialFormData: AccessStateFormData = {
  id: '',
  name: '',
  clientCode: '',
  contactName: '',
  contactTitle: '',
  contactEmail: '',
  contactPhone: '',
  domainListCountLimit: 0,
  acceptedEmailDomainList: [],
  acceptedEmailAddressExceptionList: [],
  profitCenterId: '',
  consultantOffice: '',
  consultantName: '',
  consultantEmail: '',
  newUserWelcomeText: '',
  parentClientId: '',
};

const _initialSelected: AccessStateSelected = {
  client: null,
  user: null,
};

const _initialEditStatus: AccessStateEdit = {
  status: false,
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
});

const edit = createReducer<AccessStateEdit>(_initialEditStatus, {
  SET_EDIT_STATUS: (state, action: AccessActions.SetEditStatus) => ({
    ...state,
    status: action.status,
  }),
});

const formData = createReducer<AccessStateFormData>(_initialFormData, {
  CLEAR_FORM_DATA: () => _initialFormData,
  FETCH_CLIENT_DETAILS_SUCCEEDED: (state, action: AccessActions.FetchClientDetailsSucceeded) => ({
    ...state,
    details: action.response.clientDetail,
    id: action.response.clientDetail.id,
    name: action.response.clientDetail.name || action.response.clientDetail.clientName,
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
    office: action.response.clientDetail.office,
    consultantName: action.response.clientDetail.consultantName,
    consultantEmail: action.response.clientDetail.consultantEmail ?
                     action.response.clientDetail.consultantEmail : null,
  }),
  SET_FORM_DATA: (state, action: AccessActions.SetFormData) => ({
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
    office: action.details.office,
    consultantName: action.details.consultantName,
    consultantEmail: action.details.consultantEmail ? action.details.consultantEmail : null,
  }),
  SET_CLIENT_NAME: (state, action: AccessActions.SetClientName) => ({
    ...state,
    name: action.name,
  }),
  SET_CLIENT_CODE: (state, action: AccessActions.SetClientCode) => ({
    ...state,
    clientCode: action.clientCode,
  }),
  SET_CLIENT_CONTACT_NAME: (state, action: AccessActions.SetClientContactName) => ({
    ...state,
    contactName: action.contactName,
  }),
  SET_CLIENT_CONTACT_TITLE: (state, action: AccessActions.SetClientContactTitle) => ({
    ...state,
    contactTitle: action.clientContactTitle,
  }),
  SET_CLIENT_CONTACT_EMAIL: (state, action: AccessActions.SetClientContactEmail) => ({
    ...state,
    contactEmail: action.clientContactEmail ? action.clientContactEmail : null,
  }),
  SET_CLIENT_CONTACT_PHONE: (state, action: AccessActions.SetClientContactPhone) => ({
    ...state,
    contactPhone: action.clientContactPhone ? action.clientContactPhone : null,
  }),
  SET_DOMAIN_LIST_COUNT_LIMIT: (state, action: AccessActions.SetDomainListCountLimit) => ({
    ...state,
    domainListCountLimit: action.domainListCountLimit,
  }),
  SET_ACCEPTED_EMAIL_DOMAIN_LIST: (state, action: AccessActions.SetAcceptedEmailDomainList) => ({
    ...state,
    acceptedEmailDomainList: action.acceptedEmailDomainList,
  }),
  SET_ACCEPTED_EMAIL_ADDRESS_EXCEPTION_LIST: (state, action: AccessActions.SetAcceptedEmailAddressExceptionList) => ({
    ...state,
    acceptedEmailAddressExceptionList: action.acceptedEmailAddressAcceptionList,
  }),
  SET_PROFIT_CENTER: (state, action: AccessActions.SetProfitCenter) => ({
    ...state,
    profitCenterId: action.profitCenterId,
  }),
  SET_OFFICE: (state, action: AccessActions.SetOffice) => ({
    ...state,
    consultantOffice: action.consultantOffice,
  }),
  SET_CONSULTANT_NAME: (state, action: AccessActions.SetConsultantName) => ({
    ...state,
    consultantName: action.consultantName,
  }),
  SET_CONSULTANT_EMAIL: (state, action: AccessActions.SetConsultantEmail) => ({
    ...state,
    consultantEmail: action.consultantEmail ? action.consultantEmail : null,
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
  filters,
  pending,
});
