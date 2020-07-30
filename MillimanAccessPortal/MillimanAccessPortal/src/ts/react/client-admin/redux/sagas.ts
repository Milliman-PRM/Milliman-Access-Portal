import { RequestAccessAction, ResponseAccessAction } from './actions';
import * as api from './api';

import { createTakeLatestRequest } from '../../shared-components/redux/sagas';

/**
 * Custom effect for handling request actions.
 * @param type Action type
 * @param apiCall API method to invoke
 */
const takeLatestRequest = createTakeLatestRequest<RequestAccessAction, ResponseAccessAction>();

/**
 * Register all sagas for the page.
 */
export default function* rootSaga() {
  // API requests
  yield takeLatestRequest('FETCH_CLIENTS', api.fetchClients);
  yield takeLatestRequest('FETCH_CLIENT_DETAILS', api.fetchClientDetails);
  yield takeLatestRequest('SET_USER_ROLE_IN_CLIENT', api.setUserRoleInClient);
  yield takeLatestRequest('SAVE_NEW_CLIENT', api.saveNewClient);
}
