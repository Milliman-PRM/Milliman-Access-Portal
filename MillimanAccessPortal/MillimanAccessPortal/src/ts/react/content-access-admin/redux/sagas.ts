import { all, apply, call, put, select, takeLatest } from 'redux-saga/effects';

import {
    AccessAction, DataAction, DataArgs, DataSuffixes, fetchSessionCheck, fetchStatusRefresh,
    ScheduleAction,
} from './actions';
import { selectedClient, selectedItem } from './selectors';

function sleep(duration: number) {
  return new Promise((resolve) => {
    setTimeout(resolve, duration);
  });
}

function* dataSaga<T extends DataArgs, R>(action: DataAction<T, R>) {
  try {
    const payload = yield apply(action, action.callback as any, action.args as any);
    yield put({ type: action.type + DataSuffixes.Succeeded, payload });
  } catch (error) {
    yield put({ type: action.type + DataSuffixes.Failed, error });
  }
}

function* scheduleStatusRefresh(action: ScheduleAction) {
  yield call(sleep, action.delay);
  const client = yield select(selectedClient);
  const item = yield select(selectedItem);
  if (client) {
    yield put(fetchStatusRefresh(client.id, item && item.id));
  } else {
    yield put({ type: AccessAction.ScheduleStatusRefresh, delay: 5000 });
  }
}

function* scheduleSessionCheck(action: ScheduleAction) {
  yield call(sleep, action.delay);
  yield put(fetchSessionCheck());
}

export default function* rootSaga() {
  yield all([
    takeLatest(AccessAction.FetchClients, dataSaga),
    takeLatest(AccessAction.FetchItems, dataSaga),
    takeLatest(AccessAction.FetchGroups, dataSaga),
    takeLatest(AccessAction.FetchSelections, dataSaga),
    takeLatest(AccessAction.FetchStatusRefresh, dataSaga),
    takeLatest(AccessAction.FetchSessionCheck, dataSaga),
    takeLatest(AccessAction.CreateGroup, dataSaga),
    takeLatest(AccessAction.UpdateGroup, dataSaga),
    takeLatest(AccessAction.DeleteGroup, dataSaga),
    takeLatest(AccessAction.SuspendGroup, dataSaga),
    takeLatest(AccessAction.UpdateSelections, dataSaga),
    takeLatest(AccessAction.CancelReduction, dataSaga),
  ]);
  yield takeLatest(AccessAction.ScheduleStatusRefresh, scheduleStatusRefresh);
  yield takeLatest(AccessAction.FetchStatusRefresh + DataSuffixes.Succeeded, function*() {
    yield put({ type: AccessAction.ScheduleStatusRefresh, delay: 5000 });
  });
  yield takeLatest(AccessAction.ScheduleSessionCheck, scheduleSessionCheck);
  yield takeLatest(AccessAction.FetchSessionCheck + DataSuffixes.Succeeded, function*() {
    yield put({ type: AccessAction.ScheduleSessionCheck, delay: 60000 });
  });
}
