import * as _ from 'lodash';

import { reducer as toastrReducer } from 'react-redux-toastr';
import { combineReducers } from 'redux';

import { AccessAction, FilterAccessAction, OpenModalAction } from './actions';
import * as AccessActions from './actions';

import {
  AccessStateBaseFormData, AccessStateData, AccessStateEdit,
  AccessStateSelected, AccessStateValid, PendingCreateClientUserState, PendingDataState,
  PendingDeleteClientState, PendingDiscardEditAfterSelectModal, PendingDiscardEditUserRoles,
  PendingHitrustReason, PendingRemoveClientUserState, PendingUserRoleAssignments,
} from './store';

import { CardAttributes } from '../../shared-components/card/card';
import { createReducerCreator, Handlers } from '../../shared-components/redux/reducers';
import { Dict, FilterState, ModalState } from '../../shared-components/redux/store';
import { ClientDetail } from '../../system-admin/interfaces';

const _initialPendingData: PendingDataState = {
  clients: false,
  details: false,
  clientUsers: false,
};
const _initialPendingUserRoleAssignements: PendingUserRoleAssignments = {
  roleAssignments: [],
};
const _initialHitrustReason: PendingHitrustReason = {
  reason: 0,
};
const _initialPendingDeleteClient: PendingDeleteClientState = {
  id: null,
  name: null,
};
const _initialPendingCreateClientUser: PendingCreateClientUserState = {
  memberOfClientId: null,
  userName: null,
  email: '',
  displayEmailError: false,
};
const _initialPendingRemoveClientUser: PendingRemoveClientUserState = {
  clientId: null,
  userId: null,
  name: null,
};
const _initialPendingDiscardEditModal: PendingDiscardEditAfterSelectModal = {
  newlySelectedClientId: null,
  editAfterSelect: false,
  newSubClientParentId: null,
  canManageNewlySelectedClient: false,
};
const _initialPendingDiscardEditUserRoles: PendingDiscardEditUserRoles = {
  callback: null,
};

const _initialDetails: ClientDetail = {
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
  newUserWelcomeText: '',
};

const _initialData: AccessStateData = {
  clients: {},
  parentClients: {},
  profitCenters: [],
  details: _initialDetails,
  assignedUsers: [],
};

const _initialFormData: AccessStateBaseFormData = {
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
  useNewUserWelcomeText: false,
  initialUseNewUserWelcomeText: false,
};

const _initialValidation: AccessStateValid = {
  name: true,
  profitCenterId: true,
  contactEmail: true,
  consultantEmail: true,
};

const _initialSelected: AccessStateSelected = {
  client: null,
  parent: null,
  user: null,
  readonly: false,
};

const _initialEditStatus: AccessStateEdit = {
  disabled: true,
  userEnabled: false,
};

/**
 * Create reducers for a subtree of the redux store
 * @param initialState Subtree of state these handlers can influence
 * @param handlers Actions and their state transformations
 */
const createReducer = createReducerCreator<AccessAction>();

/**
 * Create a reducer for a modal
 * @param openActions Actions that cause the modal to open
 * @param closeActions Actions that cause the modal to close
 */
const createModalReducer = (
  openActions: Array<OpenModalAction['type']>,
  closeActions: Array<AccessAction['type']>,
) => {
  const handlers: Handlers<ModalState, any> = {};
  openActions.forEach((action) => {
    handlers[action] = (state) => ({
      ...state,
      isOpen: true,
    });
  });
  closeActions.forEach((action) => {
    handlers[action] = (state) => ({
      ...state,
      isOpen: false,
    });
  });
  return createReducer<ModalState>({ isOpen: false }, handlers);
};

const pendingData = createReducer<PendingDataState>(_initialPendingData, {
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
  DELETE_CLIENT: (state) => ({
    ...state,
    clients: true,
  }),
  DELETE_CLIENT_SUCCEEDED: (state) => ({
    ...state,
    clients: false,
  }),
  DELETE_CLIENT_FAILED: (state) => ({
    ...state,
    clients: false,
  }),
});

const pendingRoleAssignments = createReducer<PendingUserRoleAssignments>(_initialPendingUserRoleAssignements, {
  OPEN_CREATE_CLIENT_USER_MODAL: () => _initialPendingUserRoleAssignements,
  CHANGE_USER_ROLE_PENDING: (state, action: AccessActions.ChangeUserRolePending) => {
    const currentRoleAssignments = state.roleAssignments;
    if (currentRoleAssignments.findIndex((ra) => ra.roleEnum === action.roleEnum) !== -1) {
      currentRoleAssignments.splice(state.roleAssignments.findIndex((ra) => ra.roleEnum === action.roleEnum), 1);
    }

    return {
      ...state,
      roleAssignments: currentRoleAssignments.concat({
        roleEnum: action.roleEnum,
        isAssigned: action.isAssigned,
      }),
    };
  },
});

const pendingHitrustReason = createReducer<PendingHitrustReason>(_initialHitrustReason, {
  OPEN_CHANGE_USER_ROLE_MODAL: () => _initialHitrustReason,
  OPEN_CREATE_CLIENT_USER_MODAL: () => _initialHitrustReason,
  OPEN_REMOVE_CLIENT_USER_MODAL: () => _initialHitrustReason,
  SET_ROLE_CHANGE_REASON: (state, action: AccessActions.SetRoleChangeReason) => ({
    ...state,
    reason: action.reason,
  }),
});

const pendingDeleteClient = createReducer<PendingDeleteClientState>(_initialPendingDeleteClient, {
  OPEN_DELETE_CLIENT_MODAL: (state, action: AccessActions.OpenDeleteClientModal) => ({
    ...state,
    id: action.id,
    name: action.name,
  }),
});

const pendingCreateClientUser = createReducer<PendingCreateClientUserState>(_initialPendingCreateClientUser, {
  OPEN_CREATE_CLIENT_USER_MODAL: (state, action: AccessActions.OpenCreateClientUserModal) => ({
    ...state,
    memberOfClientId: action.clientId,
    email: '',
    userName: null,
    displayEmailError: false,
  }),
  SET_CREATE_CLIENT_USER_EMAIL: (state, action: AccessActions.SetCreateClientUserModalEmail) => ({
    ...state,
    email: action.email,
    userName: action.email,
    displayEmailError: false,
  }),
  SET_CREATE_CLIENT_USER_EMAIL_ERROR: (state, action: AccessActions.SetCreateClientUserModalEmailError) => ({
    ...state,
    displayEmailError: action.showError,
  }),
});

const pendingRemoveClientUser = createReducer<PendingRemoveClientUserState>(_initialPendingRemoveClientUser, {
  OPEN_REMOVE_CLIENT_USER_MODAL: (state, action: AccessActions.OpenRemoveClientUserModal) => ({
    ...state,
    clientId: action.clientId,
    userId: action.userId,
    name: action.name,
  }),
});

const pendingDiscardEditAfterSelect = createReducer<PendingDiscardEditAfterSelectModal>(
  _initialPendingDiscardEditModal, {
  OPEN_DISCARD_EDIT_AFTER_SELECT_MODAL: (state, action: AccessActions.OpenDiscardEditAfterSelectModal) => ({
    ...state,
    newlySelectedClientId: action.newlySelectedClientId,
    editAfterSelect: action.editAfterSelect,
    newSubClientParentId: action.newSubClientParentId,
    canManageNewlySelectedClient: action.canManageNewlySelectedClient,
  }),
});

const pendingDiscardEditUserRoles = createReducer<PendingDiscardEditUserRoles>(
  _initialPendingDiscardEditUserRoles, {
  OPEN_DISCARD_USER_ROLE_CHANGES_MODAL: (state, action: AccessActions.OpenDiscardUserRoleChangesModal) => ({
    ...state,
    callback: action.callback,
  }),
});

const data = createReducer<AccessStateData>(_initialData, {
  FETCH_CLIENTS_SUCCEEDED: (state, action: AccessActions.FetchClientsSucceeded) => ({
    ...state,
    clients: {
      ...action.response.clients,
    },
    parentClients: {
      ...action.response.parentClients,
    },
  }),
  FETCH_PROFIT_CENTERS_SUCCEEDED: (state, action: AccessActions.FetchProfitCentersSucceeded) => ({
    ...state,
    profitCenters: action.response,
  }),
  RESET_CLIENT_DETAILS: (state) => ({
    ...state,
    details: _initialDetails,
  }),
  FETCH_CLIENT_DETAILS_SUCCEEDED: (state, action: AccessActions.FetchClientDetailsSucceeded) => ({
    ...state,
    details: action.response.clientDetail,
    assignedUsers: action.response.assignedUsers,
  }),
  UPDATE_ALL_USER_ROLES_IN_CLIENT_SUCCEEDED: (state, action: AccessActions.UpdateAllUserRolesInClientSucceeded) => ({
    ...state,
    ...state.assignedUsers.find((u) => u.id === action.response.userId).userRoles = action.response.roles,
  }),
  SAVE_NEW_CLIENT_SUCCEEDED: (state, action: AccessActions.SaveNewClientSucceeded) => ({
    ...state,
    clients: {
      ...action.response.clients,
    },
    details: action.response.newClient,
    assignedUsers: [action.response.assignedUser],
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
    details: _initialDetails,
  }),
  SAVE_NEW_CLIENT_USER_SUCCEEDED: (state, action: AccessActions.SaveNewClientUserSucceeded) => ({
    ...state,
    assignedUsers: [...state.assignedUsers, action.response],
  }),
  REMOVE_CLIENT_USER_SUCCEEDED: (state, action: AccessActions.RemoveClientUserSucceeded) => ({
    ...state,
    details: action.response.clientDetail,
    assignedUsers: action.response.assignedUsers,
  }),
  SELECT_CLIENT: (state) => ({
    ...state,
    clients: _.omit(state.clients, 'child'),
  }),
  SELECT_NEW_SUB_CLIENT: (state, action: AccessActions.SelectNewSubClient) => ({
    ...state,
    clients: {
      ...state.clients,
      ['child']: {
        id: null,
        parentId: action.parentId,
        name: null,
        code: null,
        contentItemCount: 0,
        userCount: 0,
        canManage: true,
      },
    },
    details: _initialDetails,
  }),
});

const selected = createReducer<AccessStateSelected>(_initialSelected, {
  SELECT_CLIENT: (state, action: AccessActions.SelectClient) => ({
    ...state,
    client: action.id === state.client ? null : action.id,
    user: null,
    readonly: action.readonly,
  }),
  SELECT_NEW_SUB_CLIENT: (state, action: AccessActions.SelectNewSubClient) => ({
    ...state,
    client: 'child',
    parent: action.parentId,
    user: null,
    readonly: false,
  }),
  SELECT_USER: (state, action: AccessActions.SelectUser) => ({
    ...state,
    user: action.id === state.user ? null : action.id,
  }),
  SAVE_NEW_CLIENT_SUCCEEDED: (state, action: AccessActions.SaveNewClientSucceeded) => ({
    ...state,
    client: action.response.newClient.id,
    user: null,
    readonly: false,
  }),
  DELETE_CLIENT_SUCCEEDED: () => _initialSelected,
  SET_EDIT_STATUS: (state) => ({
    ...state,
    user: null,
  }),
});

const edit = createReducer<AccessStateEdit>(_initialEditStatus, {
  SET_EDIT_STATUS: (state, action: AccessActions.SetEditStatus) => ({
    ...state,
    disabled: action.disabled,
    userEnabled: false,
  }),
  SELECT_USER: (state, action: AccessActions.SelectClient) => ({
    ...state,
    userEnabled: action.id !== null ? true : false,
  }),
  SELECT_NEW_SUB_CLIENT: () => ({
    disabled: false,
    userEnabled: false,
  }),
});

const formData = createReducer<AccessStateBaseFormData>(_initialFormData, {
  CLEAR_FORM_DATA: () => _initialFormData,
  SELECT_NEW_SUB_CLIENT: () => _initialFormData,
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
    parentClientId: action.response.clientDetail.parentClientId,
    consultantOffice: action.response.clientDetail.office,
    consultantName: action.response.clientDetail.consultantName,
    consultantEmail: action.response.clientDetail.consultantEmail ?
      action.response.clientDetail.consultantEmail : null,
    newUserWelcomeText: action.response.clientDetail.newUserWelcomeText ?
      action.response.clientDetail.newUserWelcomeText : '',
    useNewUserWelcomeText: action.response.clientDetail.newUserWelcomeText ? true : false,
    initialUseNewUserWelcomeText: action.response.clientDetail.newUserWelcomeText ? true : false,
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
    newUserWelcomeText: action.response.newClient.newUserWelcomeText,
    useNewUserWelcomeText: action.response.newClient.newUserWelcomeText ? true : false,
    initialUseNewUserWelcomeText: action.response.newClient.newUserWelcomeText ? true : false,
    parentClientId: action.response.newClient.parentClientId,
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
    newUserWelcomeText: action.details.newUserWelcomeText,
    useNewUserWelcomeText: action.details.newUserWelcomeText ? true : false,
    parentClientId: action.details.parentClientId,
  }),
  SET_FORM_FIELD_VALUE: (state, action: AccessActions.SetFormFieldValue) => ({
    ...state,
    [action.field]: action.value,
  }),
});

const valid = createReducer<AccessStateValid>(_initialValidation, {
  RESET_VALIDITY: () => _initialValidation,
  SELECT_CLIENT: () => _initialValidation,
  SELECT_NEW_SUB_CLIENT: () => _initialValidation,
  SET_VALIDITY_FOR_FIELD: (state, action: AccessActions.SetValidityForField) => ({
    ...state,
    [action.field]: action.valid,
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
      _.mapValues(state, (user) => ({
        ...user,
        expanded: true,
      })),
    SET_ALL_COLLAPSED_USER: (state) =>
      _.mapValues(state, (user) => ({
        ...user,
        expanded: false,
      })),
    FETCH_CLIENT_DETAILS_SUCCEEDED: (_state, action: AccessActions.FetchClientDetailsSucceeded) => {
      const state: Dict<CardAttributes> = {};
      action.response.assignedUsers.forEach((user) => {
        state[user.id] = {};
      });
      return state;
    },
  },
);

const modals = combineReducers({
  deleteClient: createModalReducer(['OPEN_DELETE_CLIENT_MODAL'], [
    'CLOSE_DELETE_CLIENT_MODAL',
    'OPEN_DELETE_CLIENT_CONFIRMATION_MODAL',
    'CLOSE_DELETE_CLIENT_CONFIRMATION_MODAL',
  ]),
  deleteClientConfirmation: createModalReducer(['OPEN_DELETE_CLIENT_CONFIRMATION_MODAL'], [
    'CLOSE_DELETE_CLIENT_CONFIRMATION_MODAL',
    'DELETE_CLIENT_SUCCEEDED',
    'DELETE_CLIENT_FAILED',
  ]),
  createClientUser: createModalReducer(['OPEN_CREATE_CLIENT_USER_MODAL'], [
    'CLOSE_CREATE_CLIENT_USER_MODAL',
    'SAVE_NEW_CLIENT_USER_SUCCEEDED',
    'SAVE_NEW_CLIENT_USER_FAILED',
  ]),
  removeClientUser: createModalReducer(['OPEN_REMOVE_CLIENT_USER_MODAL'], [
    'CLOSE_REMOVE_CLIENT_USER_MODAL',
    'REMOVE_CLIENT_USER_SUCCEEDED',
    'REMOVE_CLIENT_USER_FAILED',
  ]),
  discardEdit: createModalReducer(['OPEN_DISCARD_EDIT_MODAL'], [
    'CLOSE_DISCARD_EDIT_MODAL',
  ]),
  discardEditAfterSelect: createModalReducer(['OPEN_DISCARD_EDIT_AFTER_SELECT_MODAL'], [
    'CLOSE_DISCARD_EDIT_AFTER_SELECT_MODAL',
  ]),
  changeUserRoles: createModalReducer(['OPEN_CHANGE_USER_ROLE_MODAL'], [
    'CLOSE_CHANGE_USER_ROLES_MODAL',
  ]),
  discardUserRoleChanges: createModalReducer(['OPEN_DISCARD_USER_ROLE_CHANGES_MODAL'], [
    'CLOSE_DISCARD_USER_ROLE_CHANGES_MODAL',
  ]),
});

const pending = combineReducers({
  data: pendingData,
  roles: pendingRoleAssignments,
  hitrustReason: pendingHitrustReason,
  deleteClient: pendingDeleteClient,
  createClientUser: pendingCreateClientUser,
  removeClientUser: pendingRemoveClientUser,
  discardEditAfterSelect: pendingDiscardEditAfterSelect,
  discardEditUserRoles: pendingDiscardEditUserRoles,
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
  modals,
  filters,
  pending,
  toastr: toastrReducer,
});
