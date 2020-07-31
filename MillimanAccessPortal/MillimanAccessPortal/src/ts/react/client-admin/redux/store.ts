import { applyMiddleware, createStore } from 'redux';
import { composeWithDevTools } from 'redux-devtools-extension';
import createSagaMiddleware from 'redux-saga';

import { clientAdmin } from './reducers';
import sagas from './sagas';

import { ClientWithEligibleUsers, ClientWithStats, Guid, ProfitCenter, User } from '../../models';
import { CardAttributes } from '../../shared-components/card/card';
import { Dict, FilterState } from '../../shared-components/redux/store';
import { ClientDetail } from '../../system-admin/interfaces';

/**
 * Entity data returned from the server.
 */
export interface AccessStateData {
  clients: Dict<ClientWithEligibleUsers | ClientWithStats>;
  profitCenters: ProfitCenter[];
  details: ClientDetail;
  assignedUsers: User[];
}

export interface AccessStateSelected {
  client: Guid;
  user: Guid;
}

export interface AccessStateFormData {
  name: string;
  clientCode: string;
  contactName: string;
  contactEmail: string;
  contactPhone: string;
  domainListCountLimit: number;
  acceptedEmailDomainList: string[];
  acceptedEmailAddressExceptionList: string[];
  profitCenterId: Guid;
  consultantOffice: string;
  consultantName: string;
  consultantEmail: string;
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

export interface AccessState {
  data: AccessStateData;
  selected: AccessStateSelected;
  cardAttributes: AccessStateCardAttributes;
  filters: AccessStateFilters;
  formData: AccessStateFormData;
}

// Create the store and apply saga middleware
const sagaMiddleware = createSagaMiddleware();
export const store = createStore(
  clientAdmin,
  composeWithDevTools(
    applyMiddleware(sagaMiddleware),
  ));
sagaMiddleware.run(sagas);
