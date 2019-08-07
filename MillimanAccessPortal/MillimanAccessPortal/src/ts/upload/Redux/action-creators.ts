import { createActionCreator } from '../../react/shared-components/redux/action-creators';
import * as UploadActions from './actions';

// File Upload Actions
export const BeginFileUpload =
  createActionCreator<UploadActions.BeginFileUpload>('BEGIN_FILE_UPLOAD');
export const UpdateChecksumProgress =
  createActionCreator<UploadActions.UpdateChecksumProgress>('UPDATE_CHECKSUM_PROGRESS');
export const UpdateUploadProgress =
  createActionCreator<UploadActions.UpdateUploadProgress>('UPDATE_UPLOAD_PROGRESS');
export const SetChecksumValue =
  createActionCreator<UploadActions.SetChecksumValue>('SET_CHECKSUM_VALUE');
export const SetUploadCancelable =
  createActionCreator<UploadActions.SetUploadCancelable>('SET_UPLOAD_CANCELABLE');
export const SetUploadError =
  createActionCreator<UploadActions.SetUploadError>('SET_UPLOAD_ERROR');
export const CancelFileUpload =
  createActionCreator<UploadActions.CancelFileUpload>('CANCEL_FILE_UPLOAD');
export const FinalizeUpload =
  createActionCreator<UploadActions.FinalizeUpload>('FINALIZE_UPLOAD');
