import { RequestAccessAction, ResponseAccessAction } from "./actions";
import { createTakeLatestRequest } from "../../shared-components/redux/sagas";
import * as api from './api';

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
}