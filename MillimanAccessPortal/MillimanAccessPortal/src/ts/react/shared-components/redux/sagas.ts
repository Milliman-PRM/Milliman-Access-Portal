import { toastr } from 'react-redux-toastr';
import { Action } from 'redux';
import { call, Effect, put, takeEvery, takeLatest } from 'redux-saga/effects';

import { createErrorActionCreator, createResponseActionCreator } from './action-creators';
import {
    ErrorAction, isErrorAction, isScheduleAction, RequestAction, ResponseAction,
} from './actions';

/**
 * Return a custom effect that handles request actions for a set of actions.
 */
export function createTakeLatestRequest<TReq extends RequestAction, TRes extends ResponseAction>() {
  /**
   * Make an asynchronous API request and await the result.
   * @param apiCall API method to invoke
   * @param action the request action that caused this saga to fire
   */
  function* requestSaga(
    apiCall: (request: TReq['request']) => TRes['response'],
    action: TReq,
  ) {
    try {
      const response = yield call(apiCall, action.request);
      yield put(createResponseActionCreator(`${action.type}_SUCCEEDED` as TRes['type'])(response));
    } catch (error) {
      yield put(createErrorActionCreator(`${action.type}_FAILED` as ErrorAction['type'])(error));
    }
  }

  return (
    type: TReq['type'],
    apiCall: (request: TReq['request']) => Promise<TRes['response']>,
  ) => takeLatest(type, requestSaga, apiCall);
}

/**
 * Return a custom effect that handles schedule actions for a set of actions.
 */
export function createTakeLatestSchedule<TAction extends Action>() {
  /**
   * Sleep for the specified duration; awaitable.
   * @param duration time to sleep in milliseconds
   */
  function sleep(duration: number) {
    return new Promise((resolve) => {
      setTimeout(resolve, duration);
    });
  }

  /**
   * Schedule an action to be fired at a later time.
   * @param nextActionCreator action creator to invoke after the scheduled duration
   * @param action the schedule action that caused this saga to fire
   */
  function* scheduleSaga(
    nextActionCreator: () => (TAction | IterableIterator<Effect | TAction>),
    action: TAction,
  ) {
    if (isScheduleAction(action)) {
      yield call(sleep, action.delay);
    }
    const nextAction = yield nextActionCreator();
    if (nextAction) {
      yield put(nextAction);
    }
  }

  return (
    type: TAction['type'] | ((type: TAction) => boolean),
    nextActionCreator: () => (TAction | IterableIterator<Effect | TAction>),
  ) => takeLatest(type, scheduleSaga, nextActionCreator);
}

/**
 * Return a custom effect that handles toast actions for a set of actions.
 */
export function createTakeEveryToast<TAction extends Action, TRes extends ResponseAction>() {
  /**
   * Display a toast.
   * @param message message to display, or a function that builds the message from a response
   * @param level message severity
   * @param action the action that caused this saga to fire
   */
  function* toastSaga(
    message: string
      | ((response: TRes['response'] | ErrorAction['error']) => string),
    level: 'error' | 'info' | 'message' | 'success' | 'warning',
    action: TRes | ErrorAction,
  ) {
    yield toastr[level]('', typeof message === 'string'
      ? message
      : isErrorAction(action)
        ? message(action.error)
        : message(action.response));
  }

  return <TActionInstance extends TAction = TAction>(
    type: TActionInstance['type'] | Array<TActionInstance['type']> | ((type: TActionInstance) => boolean),
    message: string | (TActionInstance extends TRes
      ? (response: TActionInstance['response']) => string
      : TActionInstance extends ErrorAction
        ? (error: TActionInstance['error']) => string
        : never),
    level: 'error' | 'info' | 'message' | 'success' | 'warning' = 'success',
  ) => takeEvery(type, toastSaga, message, level);
}
