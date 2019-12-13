import { createActionCreator } from '../../react/shared-components/redux/action-creators';
import * as UploadActions from './actions';

// File Upload Actions
export const beginFileUpload =
  createActionCreator<UploadActions.BeginFileUpload>('BEGIN_FILE_UPLOAD');
export const updateChecksumProgress =
  createActionCreator<UploadActions.UpdateChecksumProgress>('UPDATE_CHECKSUM_PROGRESS');
export const updateUploadProgress =
  createActionCreator<UploadActions.UpdateUploadProgress>('UPDATE_UPLOAD_PROGRESS');
export const setUploadCancelable =
  createActionCreator<UploadActions.SetUploadCancelable>('SET_UPLOAD_CANCELABLE');
export const setUploadError =
  createActionCreator<UploadActions.SetUploadError>('SET_UPLOAD_ERROR');
export const cancelFileUpload =
  createActionCreator<UploadActions.CancelFileUpload>('CANCEL_FILE_UPLOAD');
export const finalizeUpload =
  createActionCreator<UploadActions.FinalizeUpload>('FINALIZE_UPLOAD');
