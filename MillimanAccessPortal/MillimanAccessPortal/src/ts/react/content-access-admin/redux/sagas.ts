import { call, put, takeLatest } from 'redux-saga/effects';

import { AccessAction, fetchClientsFailed, fetchClientsSucceeded } from './actions';
import * as api from './api';

function* fetchClients() {
  try {
    const clients = yield call(api.fetchClients);
    yield put(fetchClientsSucceeded(clients));
  } catch (error) {
    yield put(fetchClientsFailed(error));
  }
}

export default function* rootSaga() {
  yield takeLatest(AccessAction.FetchClientsRequested, fetchClients);
}
