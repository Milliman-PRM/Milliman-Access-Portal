import {
  AccessAction, ErrorAccessAction, PromptDomainLimitExceeded, PromptExistingDomainName,
  PromptExistingEmailAddress, PromptInvalidDomainName, PromptInvalidEmailAddress,
  RequestAccessAction, ResponseAccessAction,
} from './actions';
import * as api from './api';

import { createTakeEveryToast, createTakeLatestRequest } from '../../shared-components/redux/sagas';

/**
 * Custom effect for handling request actions.
 * @param type Action type
 * @param apiCall API method to invoke
 */
const takeLatestRequest = createTakeLatestRequest<RequestAccessAction, ResponseAccessAction>();

/**
 * Custom effect for handling actions that result in toasts.
 * @param type action type
 * @param message message to display, or a function that builds the message from a response
 * @param level message severity
 */
const takeEveryToast = createTakeEveryToast<AccessAction, ResponseAccessAction>();

/**
 * Register all sagas for the page.
 */
export default function* rootSaga() {
  // API requests
  yield takeLatestRequest('FETCH_CLIENTS', api.fetchClients);
  yield takeLatestRequest('FETCH_PROFIT_CENTERS', api.fetchProfitCenters);
  yield takeLatestRequest('FETCH_CLIENT_DETAILS', api.fetchClientDetails);
  yield takeLatestRequest('SET_USER_ROLE_IN_CLIENT', api.setUserRoleInClient);
  yield takeLatestRequest('SAVE_NEW_CLIENT', api.saveNewClient);
  yield takeLatestRequest('EDIT_CLIENT', api.editClient);
  yield takeLatestRequest('DELETE_CLIENT', api.deleteClient);
  yield takeLatestRequest('SAVE_NEW_CLIENT_USER', api.saveNewClientUser);
  yield takeLatestRequest('REMOVE_CLIENT_USER', api.removeClientUser);

  // Toasts
  yield takeEveryToast('SAVE_NEW_CLIENT_SUCCEEDED', 'Created new client');
  yield takeEveryToast('EDIT_CLIENT_SUCCEEDED', 'Updated client');
  yield takeEveryToast('DELETE_CLIENT_SUCCEEDED', 'Deleted client');
  yield takeEveryToast('SAVE_NEW_CLIENT_USER_SUCCEEDED', 'User successfully added');
  yield takeEveryToast('REMOVE_CLIENT_USER_SUCCEEDED', 'User successfully removed');

  // Warning
  yield takeEveryToast<PromptExistingDomainName>('PROMPT_EXISITING_DOMAIN_NAME',
    'That domain already exists.', 'warning');
  yield takeEveryToast<PromptInvalidDomainName>('PROMPT_INVALID_DOMAIN_NAME',
    'Please enter a valid domain name (e.g. domain.com)', 'warning');
  yield takeEveryToast<PromptDomainLimitExceeded>('PROMPT_DOMAIN_LIMIT_EXCEEDED',
    `You have reached the allowed domain limit for this client.
     Contact map.support@milliman.com to request an increase to this limit.`,
    'warning');
  yield takeEveryToast<PromptInvalidEmailAddress>('PROMPT_INVALID_EMAIL_ADDRESS',
    'Please enter a valid email address (e.g. username@domain.com)', 'warning');
  yield takeEveryToast<PromptExistingEmailAddress>('PROMPT_EXISTING_EMAIL_ADDRESS',
    'That email address already exists.', 'warning');

  yield takeEveryToast<ErrorAccessAction>([
    'FETCH_CLIENTS_FAILED',
    'FETCH_PROFIT_CENTERS_FAILED',
    'FETCH_CLIENT_DETAILS_FAILED',
    'SAVE_NEW_CLIENT_FAILED',
    'EDIT_CLIENT_FAILED',
    'DELETE_CLIENT_FAILED',
    'SAVE_NEW_CLIENT_USER_FAILED',
    'REMOVE_CLIENT_USER_FAILED',
  ], ({ message }) => message === 'sessionExpired'
    ? 'Your session has expired. Please refresh the page.'
    : isNaN(message)
      ? message
      : 'An unexpected error has occurred.',
    'error');
}
