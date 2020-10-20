import { applyMiddleware, createStore } from 'redux';
import { composeWithDevTools } from 'redux-devtools-extension';
import createSagaMiddleware from 'redux-saga';

import { clientAdmin } from './reducers';
import sagas from './sagas';

import { ClientWithEligibleUsers, ClientWithStats, Guid, ProfitCenter, User } from '../../models';
import { CardAttributes } from '../../shared-components/card/card';
import { Dict, FilterState, ModalState } from '../../shared-components/redux/store';
import { ClientDetail } from '../../system-admin/interfaces';

/**
 * Flags indicating whether the page is waiting on new data for an entity type.
 */
export interface PendingDataState {
  clients: boolean;
  details: boolean;
  clientUsers: boolean;
}

/**
 * Flags indicating whether the page is waiting on user input/confirmation for particular actions.
 */
export interface PendingDeleteClientState {
  id: Guid;
  name: string;
}
export interface PendingCreateClientUserState {
  memberOfClientId: Guid;
  userName: string;
  email: string;
}
export interface PendingRemoveClientUserState {
  clientId: Guid;
  userId: Guid;
  name: string;
}
export interface PendingDiscardEditAfterSelectModal {
  newlySelectedClientId: Guid;
  editAfterSelect: boolean;
  newSubClientParentId: Guid;
}

/**
 * Entity data returned from the server.
 */
export interface AccessStateData {
  clients: Dict<ClientWithEligibleUsers | ClientWithStats>;
  parentClients: Dict<ClientWithEligibleUsers | ClientWithStats>;
  profitCenters: ProfitCenter[];
  details: ClientDetail;
  assignedUsers: User[];
}

export interface AccessStateSelected {
  client: Guid;
  parent: Guid;
  user: Guid;
  readonly: boolean;
}

export interface AccessStateEdit {
  disabled: boolean;
}

export interface ValidationState {
  valid: boolean;
}

export interface AccessStateBaseFormData {
  name: string;
  clientCode: string;
  contactName: string;
  contactEmail: string;
  contactTitle: string;
  contactPhone: string;
  domainListCountLimit: number;
  acceptedEmailDomainList: string[];
  acceptedEmailAddressExceptionList: string[];
  profitCenterId: Guid;
  consultantOffice: string;
  consultantName: string;
  consultantEmail: string;
  newUserWelcomeText: string;
  parentClientId: Guid;
  useNewUserWelcomeText: boolean;
}

export interface AccessStateFormData extends AccessStateBaseFormData {
  id?: Guid;
}

export interface AccessStateValid {
  name: boolean;
  profitCenterId: boolean;
  contactEmail: boolean;
  consultantEmail: boolean;
}

/**
 * All filter state.
 */
export interface AccessStateFilters {
  client: FilterState;
  user: FilterState;
}

/**
 * Card attribute collections.
 */
export interface AccessStateCardAttributes {
  user: Dict<CardAttributes>;
}

/**
 * All pending state.
 */
export interface AccessStatePending {
  data: PendingDataState;
  deleteClient: PendingDeleteClientState;
  createClientUser: PendingCreateClientUserState;
  removeClientUser: PendingRemoveClientUserState;
  discardEditAfterSelect: PendingDiscardEditAfterSelectModal;
}

/**
 * All modal state.
 */
export interface AccessStateModals {
  deleteClient: ModalState;
  deleteClientConfirmation: ModalState;
  createClientUser: ModalState;
  removeClientUser: ModalState;
  discardEdit: ModalState;
  discardEditAfterSelect: ModalState;
}

export interface AccessState {
  data: AccessStateData;
  selected: AccessStateSelected;
  edit: AccessStateEdit;
  cardAttributes: AccessStateCardAttributes;
  filters: AccessStateFilters;
  formData: AccessStateBaseFormData;
  pending: AccessStatePending;
  valid: AccessStateValid;
  modals: AccessStateModals;
}

// Create the store and apply saga middleware
const sagaMiddleware = createSagaMiddleware();
export const store = createStore(
  clientAdmin,
  composeWithDevTools(
    applyMiddleware(sagaMiddleware),
  ));
sagaMiddleware.run(sagas);
