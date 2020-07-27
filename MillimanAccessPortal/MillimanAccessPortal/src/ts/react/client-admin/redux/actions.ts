import { Dict } from "../../shared-components/redux/store";
import { ClientWithEligibleUsers, User, Guid } from "../../models";
import { TSError } from "../../shared-components/redux/actions";
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

// ~ Form Actions ~
export interface ClearFormData {
  type: 'CLEAR_FORM_DATA';
}
export interface SetFormData {
  type: 'SET_FORM_DATA';
  details: ClientDetail;
}
export interface SetClientName {
  type: 'SET_CLIENT_NAME';
  clientName: string;
}
export interface SetClientCode {
  type: 'SET_CLIENT_CODE';
  clientCode: string;
}
export interface SetClientContactName {
  type: 'SET_CLIENT_CONTACT_NAME';
  clientContactName: string;
}
export interface SetClientContactTitle {
  type: 'SET_CLIENT_CONTACT_TITLE';
  clientContactTitle: string;
}
export interface SetClientContactEmail {
  type: 'SET_CLIENT_CONTACT_EMAIL';
  clientContactEmail: string;
}
export interface SetClientContactPhone {
  type: 'SET_CLIENT_CONTACT_PHONE';
  clientContactPhone: string;
}
export interface SetDomainListCountLimit {
  type: 'SET_DOMAIN_LIST_COUNT_LIMIT';
  domainListCountLimit: number;
}
export interface SetAcceptedEmailDomainList {
  type: 'SET_ACCEPTED_EMAIL_DOMAIN_LIST';
  acceptedEmailDomainList: string[];
}
export interface SetAcceptedEmailAddressExceptionList {
  type: 'SET_ACCEPTED_EMAIL_ADDRESS_EXCEPTION_LIST';
  acceptedEmailAddressAcceptionList: string[];
}
export interface SetProfitCenter {
  type: 'SET_PROFIT_CENTER';
  profitCenter: string;
}
export interface SetOffice {
  type: 'SET_OFFICE';
  office: string;
}
export interface SetConsultantName {
  type: 'SET_CONSULTANT_NAME';
  consultantName: string;
}
export interface SetConsultantEmail {
  type: 'SET_CONSULTANT_EMAIL';
  consultantEmail: string;
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
    roles: [
      {
        isAssigned: boolean;
        roleDisplayValue: string;
        roleEnum: RoleEnum;
      }
    ];
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

export type FormAction =
  | ClearFormData
  | SetFormData
  | SetClientName
  | SetClientCode
  | SetClientContactName
  | SetClientContactEmail
  | SetClientContactPhone
  | SetDomainListCountLimit
  | SetAcceptedEmailDomainList
  | SetAcceptedEmailAddressExceptionList
  | SetProfitCenter
  | SetOffice
  | SetConsultantName
  | SetConsultantEmail;

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
  | FilterAccessAction
  | FormAction;

export type AccessAction =
  | PageAccessAction
  | RequestAccessAction
  | ResponseAccessAction;