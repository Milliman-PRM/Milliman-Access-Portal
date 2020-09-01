import {
    createActionCreator, createRequestActionCreator,
} from '../../shared-components/redux/action-creators';
import * as AccountActions from './actions';

export const setPendingTextInputValue =
  createActionCreator<AccountActions.SetPendingTextInputValue>('SET_PENDING_TEXT_INPUT_VALUE');
export const resetForm =
  createActionCreator<AccountActions.ResetForm>('RESET_FORM');
export const scheduleSessionCheck =
  createActionCreator<AccountActions.ScheduleSessionCheck>('SCHEDULE_SESSION_CHECK');

export const fetchUser =
  createRequestActionCreator<AccountActions.FetchUser>('FETCH_USER');
export const fetchSessionCheck =
  createRequestActionCreator<AccountActions.FetchSessionCheck>('FETCH_SESSION_CHECK');
export const updateAccount =
  createRequestActionCreator<AccountActions.UpdateAccount>('UPDATE_ACCOUNT');
export const requestPasswordReset =
  createRequestActionCreator<AccountActions.RequestPasswordReset>('REQUEST_PASSWORD_RESET');

export const validateInputUser =
  createActionCreator<AccountActions.ValidateInputUser>('VALIDATE_INPUT_USER');
