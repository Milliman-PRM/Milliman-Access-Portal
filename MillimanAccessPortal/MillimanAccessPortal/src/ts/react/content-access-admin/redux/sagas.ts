import { toastr } from 'react-redux-toastr';
import { Action } from 'redux';
import { all, apply, call, put, select, takeEvery, takeLatest } from 'redux-saga/effects';

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
  // API requests
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

  // polling
  yield takeLatest(AccessAction.ScheduleStatusRefresh, scheduleStatusRefresh);
  yield takeLatest(AccessAction.FetchStatusRefresh + DataSuffixes.Succeeded, function*() {
    yield put({ type: AccessAction.ScheduleStatusRefresh, delay: 5000 });
  });
  yield takeLatest(AccessAction.ScheduleSessionCheck, scheduleSessionCheck);
  yield takeLatest(AccessAction.FetchSessionCheck + DataSuffixes.Failed, function*() {
    yield window.location.reload();
  });
  yield takeLatest(AccessAction.FetchSessionCheck + DataSuffixes.Succeeded, function*() {
    yield put({ type: AccessAction.ScheduleSessionCheck, delay: 60000 });
  });

  // toastr
  yield takeEvery(AccessAction.CreateGroup + DataSuffixes.Succeeded, function*() {
    yield toastr.success('', 'Selection group created.');
  });
  yield takeEvery(AccessAction.DeleteGroup + DataSuffixes.Succeeded, function*() {
    yield toastr.success('', 'Selection group deleted.');
  });
  yield takeEvery(AccessAction.UpdateGroup + DataSuffixes.Succeeded, function*() {
    yield toastr.success('', 'Selection group updated.');
  });
  yield takeEvery(AccessAction.SuspendGroup + DataSuffixes.Succeeded, function*(action: any) {
    const { isSuspended } = action.payload;
    yield toastr.success('', `Selection group ${isSuspended ? '' : 'un'}suspended.`);
  });
  yield takeEvery(AccessAction.UpdateSelections + DataSuffixes.Succeeded, function*(action: any) {
    const { group, reduction } = action.payload;
    yield toastr.success('', reduction && reduction.taskStatus === 10
      ? 'Reduction queued.'
      : group && group.isMaster
        ? 'Unrestricted access granted.'
        : 'Group inactivated.');
  });
  yield takeEvery(AccessAction.CancelReduction + DataSuffixes.Succeeded, function*() {
    yield toastr.success('', 'Reduction canceled.');
  });
  yield takeEvery(AccessAction.CancelReduction + DataSuffixes.Failed, function*() {
    yield toastr.info('', 'The reduction has already begun processing.');
  });
  yield takeEvery((action: Action) => (
      action.type.match(`${DataSuffixes.Failed}$`)
      && !action.type.match(`^${AccessAction.CancelReduction}`)
    ), function*() {
      yield toastr.warning('', 'An unexpected error has occured.');
  });
}
