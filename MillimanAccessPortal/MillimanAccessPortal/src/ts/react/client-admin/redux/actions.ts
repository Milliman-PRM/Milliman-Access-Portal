import { Dict } from "../../shared-components/redux/store";
import { ClientWithEligibleUsers, ClientWithStats, User, Guid, UserRole } from "../../models";
import { TSError } from "../../shared-components/redux/actions";
import { fetchClientDetails } from "./action-creators";
import { ClientDetail } from "../../system-admin/interfaces";
import { RoleEnum } from "../../shared-components/interfaces";

// ~ Page Actions ~

/**
 * Exclusively select the client card specified by id.
 * If id refers to the currently selected card, deselect it.
 */
export interface SelectClient {
  type: 'SELECT_CLIENT';
  id: Guid;
}
export interface SelectUser {
  type: 'SELECT_USER';
  id: Guid;
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
  };
}
export interface FetchClientsFailed {
  type: 'FETCH_CLIENTS_FAILED';
  error: TSError;
}

/**
 * GET:
 *   details for a client that can be viewed or changed by a client admin;
 */
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

/**
 * An action that sets filter text for a card column.
 */
export type FilterAccessAction =
  | SetFilterTextClient
  | SetFilterTextUser;

/**
 * An action that makes an Ajax request.
 */
export type RequestAccessAction =
  | FetchClients
  | FetchClientDetails
  | SetUserRoleInClient;

export type ResponseAccessAction =
  | FetchClientsSucceeded
  | FetchClientDetailsSucceeded
  | SetUserRoleInClientSucceeded;

export type PageAccessAction =
  | SelectClient
  | SelectUser
  | SetCollapsedUser
  | SetExpandedUser
  | SetAllCollapsedUser
  | SetAllExpandedUser
  | FilterAccessAction;

export type AccessAction =
  | PageAccessAction
  | RequestAccessAction
  | ResponseAccessAction;