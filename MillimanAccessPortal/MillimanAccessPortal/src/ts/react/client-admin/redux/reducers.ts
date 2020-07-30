import * as _ from 'lodash';

import { combineReducers } from 'redux';

import { AccessAction, FilterAccessAction } from './actions';
import * as AccessActions from './actions';
import { AccessStateData, AccessStateFormData, AccessStateSelected } from './store';

import { CardAttributes } from '../../shared-components/card/card';
import { createReducerCreator } from '../../shared-components/redux/reducers';
import { Dict, FilterState } from '../../shared-components/redux/store';

const _initialData: AccessStateData = {
  clients: {},
  details: {
    id: null,
    clientName: '',
    clientCode: '',
    clientContactName: '',
    clientContactEmail: '',
    clientContactPhone: '',
    domainListCountLimit: 0,
    acceptedEmailDomainList: [],
    acceptedEmailAddressExceptionList: [],
    profitCenter: '',
    office: '',
    consultantName: '',
    consultantEmail: '',
  },
  assignedUsers: [],
};

const _initialFormData: AccessStateFormData = {
  name: '',
  clientCode: '',
  contactName: '',
  contactEmail: null,
  contactPhone: null,
  domainListCountLimit: 0,
  acceptedEmailDomainList: [],
  acceptedEmailAddressExceptionList: [],
  profitCenterId: '',
  consultantOffice: '',
  consultantName: '',
  consultantEmail: null,
};

const _initialSelected: AccessStateSelected = {
  client: null,
  user: null,
};

/**
 * Create reducers for a subtree of the redux store
 * @param initialState Subtree of state these handlers can influence
 * @param handlers Actions and their state transformations
 */
const createReducer = createReducerCreator<AccessAction>();

const data = createReducer<AccessStateData>(_initialData, {
  FETCH_CLIENTS_SUCCEEDED: (state, action: AccessActions.FetchClientsSucceeded) => ({
    ...state,
    clients: {
      ...action.response.clients,
    },
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

const formData = createReducer<AccessStateFormData>(_initialFormData, {
  CLEAR_FORM_DATA: () => _initialFormData,
  SET_FORM_DATA: (state, action: AccessActions.SetFormData) => ({
    ...state,
    clientName: action.details.clientName,
    clientCode: action.details.clientCode,
    clientContactName: action.details.clientContactName,
    clientContactEmail: action.details.clientContactEmail,
    clientContactPhone: action.details.clientContactPhone,
    domainListCountLimit: action.details.domainListCountLimit,
    acceptedEmailDomainList: action.details.acceptedEmailDomainList,
    acceptedEmailAddressExceptionList: action.details.acceptedEmailAddressExceptionList,
    profitCenter: action.details.profitCenter,
    office: action.details.office,
    consultantName: action.details.consultantName,
    consultantEmail: action.details.consultantEmail,
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
    contactName: action.clientContactName,
  }),
  SET_CLIENT_CONTACT_EMAIL: (state, action: AccessActions.SetClientContactEmail) => ({
    ...state,
    contactEmail: action.clientContactEmail,
  }),
  SET_CLIENT_CONTACT_PHONE: (state, action: AccessActions.SetClientContactPhone) => ({
    ...state,
    contactPhone: action.clientContactPhone,
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
    profitCenter: action.profitCenter,
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
    consultantEmail: action.consultantEmail,
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
  formData,
  filters,
});
