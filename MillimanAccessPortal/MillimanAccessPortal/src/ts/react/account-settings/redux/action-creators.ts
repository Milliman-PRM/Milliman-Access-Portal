import {
    createActionCreator, createRequestActionCreator,
} from '../../shared-components/redux/action-creators';
import * as AccountActions from './actions';

export const setPendingTextInputValue =
  createActionCreator<AccountActions.SetPendingTextInputValue>('SET_PENDING_TEXT_INPUT_VALUE');
export const resetForm =
  createActionCreator<AccountActions.ResetForm>('RESET_FORM');

export const fetchUser =
  createRequestActionCreator<AccountActions.FetchUser>('FETCH_USER');

export const validateInput =
  createActionCreator<AccountActions.ValidateInput>('VALIDATE_INPUT');
