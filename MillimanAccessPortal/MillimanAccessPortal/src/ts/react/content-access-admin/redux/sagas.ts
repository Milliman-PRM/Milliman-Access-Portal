import { all, apply, put, takeLatest } from 'redux-saga/effects';

import { AccessAction, DataAction, DataArgs, RequestSuffixes } from './actions';

function* dataSaga<T extends DataArgs, R>(action: DataAction<T, R>) {
  try {
    const payload = yield apply(action, action.callback as any, action.args as any);
    yield put({ type: action.type + RequestSuffixes.Succeeded, payload });
  } catch (error) {
    yield put({ type: action.type + RequestSuffixes.Failed, error });
  }
}

export default function* rootSaga() {
  yield all([
    takeLatest(AccessAction.FetchClients, dataSaga),
    takeLatest(AccessAction.FetchItems, dataSaga),
    takeLatest(AccessAction.FetchGroups, dataSaga),
    takeLatest(AccessAction.FetchSelections, dataSaga),
    takeLatest(AccessAction.FetchStatus, dataSaga),
    takeLatest(AccessAction.CreateGroup, dataSaga),
    takeLatest(AccessAction.UpdateGroup, dataSaga),
    takeLatest(AccessAction.DeleteGroup, dataSaga),
    takeLatest(AccessAction.SuspendGroup, dataSaga),
    takeLatest(AccessAction.UpdateSelections, dataSaga),
    takeLatest(AccessAction.CancelReduction, dataSaga),
  ]);
}
