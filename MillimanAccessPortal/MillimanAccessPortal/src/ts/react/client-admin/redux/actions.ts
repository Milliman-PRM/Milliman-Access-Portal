import { Dict } from "../../shared-components/redux/store";
import { Client, ClientWithEligibleUsers, ClientWithStats, Guid, ProfitCenter, User, UserRole } from "../../models";
import { TSError } from "../../shared-components/redux/actions";
import { ClientDetail } from "../../system-admin/interfaces";
import { RoleEnum } from "../../shared-components/interfaces";
import { AccessStateFormData, PendingDeleteClientState } from "./store";

// ~ Page Actions ~

/**
 * Exclusively select the client card specified by id.
 * If id refers to the currently selected card, deselect it.
 */
export interface SelectClient {
  type: 'SELECT_CLIENT';
  id: Guid;
  readonly?: boolean;
}
export interface SelectNewSubClient {
  type: 'SELECT_NEW_SUB_CLIENT';
  parentId: Guid;
}
export interface SelectUser {
  type: 'SELECT_USER';
  id: Guid;
}

/**
 * Set whether or not the current form is being edited.
 */
export interface SetEditStatus {
  type: 'SET_EDIT_STATUS';
  disabled: boolean;
}

/**
 * Expand the user card specified by id.
 */
export interface SetExpandedUser {
  type: 'SET_EXPANDED_USER';
  id: Guid;
}

/**
 * Collapse the user card specified by id.
 */
export interface SetCollapsedUser {
  type: 'SET_COLLAPSED_USER';
  id: Guid;
}

/**
 * Expand all user cards.
 */
export interface SetAllExpandedUser {
  type: 'SET_ALL_EXPANDED_USER';
}

/**
 * Collapse all user cards.
 */
export interface SetAllCollapsedUser {
  type: 'SET_ALL_COLLAPSED_USER';
}

// ~ Filter Actions ~
export interface SetFilterTextClient {
  type: 'SET_FILTER_TEXT_CLIENT';
  text: string;
}
export interface SetFilterTextUser {
  type: 'SET_FILTER_TEXT_USER';
  text: string;
}

// ~ Form Actions ~
export interface ClearFormData {
  type: 'CLEAR_FORM_DATA';
}
export interface ResetFormData {
  type: 'RESET_FORM_DATA';
  details: ClientDetail;
}
export interface SetFormFieldValue {
  type: 'SET_FORM_FIELD_VALUE';
  field: string;
  value: string | string[] | Guid;
}

// ~ Checking validity of form items ~
export interface ResetValidity {
  type: 'RESET_VALIDITY';
}
export interface SetValidityForField {
  type: 'SET_VALIDITY_FOR_FIELD';
  field: string;
  valid: boolean;
}

// ~ GETs ~

/**
 * GET:
 *   clients the current user has access to administrate;
 */
export interface FetchClients {
  type: 'FETCH_CLIENTS';
  request: {};
}
export interface FetchClientsSucceeded {
  type: 'FETCH_CLIENTS_SUCCEEDED';
  response: {
    clients: Dict<ClientWithEligibleUsers>;
    parentClients: Dict<ClientWithStats>;
  };
}
export interface FetchClientsFailed {
  type: 'FETCH_CLIENTS_FAILED';
  error: TSError;
}

/**
 * GET:
 *  profit center the current user has access to; 
 */
export interface FetchProfitCenters {
  type: 'FETCH_PROFIT_CENTERS';
  request: {};
}
export interface FetchProfitCentersSucceeded {
  type: 'FETCH_PROFIT_CENTERS_SUCCEEDED';
  response: ProfitCenter[];
}
export interface FetchProfitCentersFailed {
  type: 'FETCH_PROFIT_CENTERS_FAILED';
  error: TSError;
}

/**
 * GET:
 *   details for a client that can be viewed or changed by a client admin;
 */
export interface ResetClientDetails {
  type: 'RESET_CLIENT_DETAILS';
}
export interface FetchClientDetails {
  type: 'FETCH_CLIENT_DETAILS';
  request: {
    clientId: Guid;
  };
}
export interface FetchClientDetailsSucceeded {
  type: 'FETCH_CLIENT_DETAILS_SUCCEEDED';
  response: {
    clientDetail: ClientDetail;
    assignedUsers: User[];
  };
}
export interface FetchClientDetailsFailed {
  type: 'FETCH_CLIENT_DETAILS_FAILED';
  error: TSError;
}

// POSTS
export interface SetUserRoleInClient {
  type: 'SET_USER_ROLE_IN_CLIENT';
  request: {
    clientId: Guid;
    isAssigned: boolean;
    roleEnum: RoleEnum;
    userId: Guid;
  };
}
export interface SetUserRoleInClientSucceeded {
  type: 'SET_USER_ROLE_IN_CLIENT_SUCCEEDED';
  response: {
    userId: Guid;
    roles: Dict<UserRole>;
  };
}
export interface SetUserRoleInClientFailed {
  type: 'SET_USER_ROLE_IN_CLIENT_FAILED';
  error: TSError;
}
export interface SaveNewClient {
  type: 'SAVE_NEW_CLIENT';
  request: AccessStateFormData;
}
export interface SaveNewClientSucceeded {
  type: 'SAVE_NEW_CLIENT_SUCCEEDED';
  response: {
    clients: Dict<ClientWithEligibleUsers>;
    newClient: ClientDetail;
  };
}
export interface SaveNewClientFailed {
  type: 'SAVE_NEW_CLIENT_FAILED';
  error: TSError;
}
export interface EditClient {
  type: 'EDIT_CLIENT';
  request: AccessStateFormData;
}
export interface EditClientSucceeded {
  type: 'EDIT_CLIENT_SUCCEEDED';
  response: {
    clients: Dict<ClientWithEligibleUsers>;
  };
}
export interface EditClientFailed {
  type: 'EDIT_CLIENT_FAILED';
  error: TSError;
}
export interface DeleteClient {
  type: 'DELETE_CLIENT';
  request: Guid;
}
export interface DeleteClientSucceeded {
  type: 'DELETE_CLIENT_SUCCEEDED';
  response: {
    clients: Dict<ClientWithEligibleUsers>;
  };
}
export interface DeleteClientFailed {
  type: 'DELETE_CLIENT_FAILED';
  error: TSError;
}
export interface SaveNewClientUser {
  type: 'SAVE_NEW_CLIENT_USER';
  request: {
    memberOfClientId: Guid;
    userName: string;
    email: string;
  };
}
export interface SaveNewClientUserSucceeded {
  type: 'SAVE_NEW_CLIENT_USER_SUCCEEDED';
  response: User;
}
export interface SaveNewClientUserFailed {
  type: 'SAVE_NEW_CLIENT_USER_FAILED';
  error: TSError;
}
export interface RemoveClientUser {
  type: 'REMOVE_CLIENT_USER';
  request: {
    clientId: Guid;
    userId: Guid;
  };
}
export interface RemoveClientUserSucceeded {
  type: 'REMOVE_CLIENT_USER_SUCCEEDED';
  response: {
    clientDetail: ClientDetail;
    assignedUsers: User[];
  };
}
export interface RemoveClientUserFailed {
  type: 'REMOVE_CLIENT_USER_FAILED';
  error: TSError;
}

// Modals
export interface OpenDeleteClientModal {
  type: 'OPEN_DELETE_CLIENT_MODAL';
  id: Guid;
  name: string;
}
export interface OpenDeleteClientConfirmationModal {
  type: 'OPEN_DELETE_CLIENT_CONFIRMATION_MODAL';
}
export interface CloseDeleteClientModal {
  type: 'CLOSE_DELETE_CLIENT_MODAL';
}
export interface CloseDeleteClientConfirmationModal {
  type: 'CLOSE_DELETE_CLIENT_CONFIRMATION_MODAL';
}
export interface OpenCreateClientUserModal {
  type: 'OPEN_CREATE_CLIENT_USER_MODAL';
  clientId: Guid;
}
export interface CloseCreateClientUserModal {
  type: 'CLOSE_CREATE_CLIENT_USER_MODAL';
}
export interface SetCreateClientUserModalEmail {
  type: 'SET_CREATE_CLIENT_USER_EMAIL';
  email: string;
}
export interface OpenRemoveClientUserModal {
  type: 'OPEN_REMOVE_CLIENT_USER_MODAL';
  clientId: string;
  userId: string;
  name: string;
}
export interface CloseRemoveClientUserModal {
  type: 'CLOSE_REMOVE_CLIENT_USER_MODAL';
}
export interface OpenDiscardEditModal {
  type: 'OPEN_DISCARD_EDIT_MODAL';
}
export interface CloseDiscardEditModal {
  type: 'CLOSE_DISCARD_EDIT_MODAL';
}
export interface OpenDiscardEditAfterSelectModal {
  type: 'OPEN_DISCARD_EDIT_AFTER_SELECT_MODAL';
  newlySelectedClientId: Guid;
  editAfterSelect: boolean;
}
export interface CloseDiscardEditAfterSelectModal {
  type: 'CLOSE_DISCARD_EDIT_AFTER_SELECT_MODAL';
}

/**
 * An action that sets filter text for a card column.
 */
export type FilterAccessAction =
  | SetFilterTextClient
  | SetFilterTextUser
  ;

export type FormAction =
  | ClearFormData
  | ResetFormData
  | SetFormFieldValue
  ;

export type ValidityAction =
  | ResetValidity
  | SetValidityForField
  ;

/**
 * An action that makes an Ajax request.
 */
export type RequestAccessAction =
  | FetchClients
  | FetchProfitCenters
  | FetchClientDetails
  | SetUserRoleInClient
  | SaveNewClient
  | EditClient
  | DeleteClient
  | SaveNewClientUser
  | RemoveClientUser
  ;

export type ResponseAccessAction =
  | FetchClientsSucceeded
  | FetchProfitCentersSucceeded
  | FetchClientDetailsSucceeded
  | SetUserRoleInClientSucceeded
  | SaveNewClientSucceeded
  | EditClientSucceeded
  | DeleteClientSucceeded
  | SaveNewClientUserSucceeded
  | RemoveClientUserSucceeded
  ;

/**
* An action that marks the errored response of an Ajax request.
*/
export type ErrorAccessAction =
  | FetchClientsFailed
  | FetchProfitCentersFailed
  | FetchClientDetailsFailed
  | SaveNewClientFailed
  | EditClientFailed
  | DeleteClientFailed
  | SaveNewClientFailed
  | SaveNewClientUserFailed
  | RemoveClientUserFailed
  ;

export type PageAccessAction =
  | SelectClient
  | SelectNewSubClient
  | SetEditStatus
  | SelectUser
  | SetCollapsedUser
  | SetExpandedUser
  | SetAllCollapsedUser
  | SetAllExpandedUser
  | FilterAccessAction
  | FormAction
  | ValidityAction
  | ResetClientDetails
  | OpenDeleteClientModal
  | CloseDeleteClientModal
  | OpenDeleteClientConfirmationModal
  | CloseDeleteClientConfirmationModal
  | OpenCreateClientUserModal
  | CloseCreateClientUserModal
  | SetCreateClientUserModalEmail
  | OpenRemoveClientUserModal
  | CloseRemoveClientUserModal
  | OpenDiscardEditModal
  | CloseDiscardEditModal
  | OpenDiscardEditAfterSelectModal
  | CloseDiscardEditAfterSelectModal
  ;

export type AccessAction =
  | PageAccessAction
  | RequestAccessAction
  | ResponseAccessAction
  | ErrorAccessAction
  ;

export type OpenModalAction =
  | OpenDeleteClientModal
  | OpenDeleteClientConfirmationModal
  | OpenCreateClientUserModal
  | OpenRemoveClientUserModal
  | OpenDiscardEditModal
  | OpenDiscardEditAfterSelectModal
  ;
