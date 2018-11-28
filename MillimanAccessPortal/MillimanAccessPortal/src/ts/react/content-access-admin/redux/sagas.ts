import { takeLatest } from 'redux-saga/effects';

function* helloSaga() {
  console.log('Hello!');
}

export default function* rootSaga() {
  yield takeLatest('FETCH_CLIENTS', helloSaga);
}
