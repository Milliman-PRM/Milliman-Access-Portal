import { combineReducers } from 'redux';

import { AccessAction, FilterAccessAction } from './actions';
import * as AccessActions from './actions';
import { AccessStateData, AccessStateSelected } from './store';

import { createReducerCreator } from '../../shared-components/redux/reducers';
import { FilterState } from '../../shared-components/redux/store';

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

const filters = combineReducers({
  client: createFilterReducer('SET_FILTER_TEXT_CLIENT'),
  user: createFilterReducer('SET_FILTER_TEXT_USER'),
});

export const clientAdmin = combineReducers({
  data,
  selected,
  filters,
});
