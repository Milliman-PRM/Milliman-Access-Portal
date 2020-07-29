import * as _ from 'lodash';

import { combineReducers } from 'redux';

import { AccessAction, FilterAccessAction } from './actions';
import * as AccessActions from './actions';
import { AccessStateData, AccessStateSelected } from './store';

import { CardAttributes } from '../../shared-components/card/card';
import { RoleEnum } from '../../shared-components/interfaces';
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
  assignedUsers: [
    {
      id: null,
      isActivated: false,
      isSuspended: false,
      firstName: '',
      lastName: '',
      userName: '',
      email: '',
      userRoles: {
        [RoleEnum.Admin]: { roleEnum: 0, roleDisplayValue: '', isAssigned: false },
        [RoleEnum.ContentAccessAdmin]: { roleEnum: 0, roleDisplayValue: '', isAssigned: false },
        [RoleEnum.ContentPublisher]: { roleEnum: 0, roleDisplayValue: '', isAssigned: false },
        [RoleEnum.ContentUser]: { roleEnum: 0, roleDisplayValue: '', isAssigned: false },
        [RoleEnum.FileDropAdmin]: { roleEnum: 0, roleDisplayValue: '', isAssigned: false },
        [RoleEnum.FileDropUser]: { roleEnum: 0, roleDisplayValue: '', isAssigned: false },
      },
  }],
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
  filters,
});
