import * as ContentPublishingActions from '../../react/content-publishing/redux/actions';
import { ProgressSummary } from '../progress-monitor';

/**
 * File Upload Actions
 */
export interface BeginFileUpload {
  type: 'BEGIN_FILE_UPLOAD';
  uploadId: string;
  fileName: string;
}

export interface UpdateChecksumProgress {
  type: 'UPDATE_CHECKSUM_PROGRESS';
  uploadId: string;
  progress: ProgressSummary;
}

export interface UpdateUploadProgress {
  type: 'UPDATE_UPLOAD_PROGRESS';
  uploadId: string;
  progress: ProgressSummary;
}

export interface SetUploadCancelable {
  type: 'SET_UPLOAD_CANCELABLE';
  uploadId: string;
  cancelable: boolean;
}

export interface SetUploadError {
  type: 'SET_UPLOAD_ERROR';
  uploadId: string;
  errorMsg: string;
}

export interface CancelFileUpload {
  type: 'CANCEL_FILE_UPLOAD';
  uploadId: string;
}

export interface FinalizeUpload {
  type: 'FINALIZE_UPLOAD';
  uploadId: string;
  fileName: string;
  Guid: string;
}

// ~~ Action unions ~~

/**
 * An action that changes the state of the page.
 */
export type PageUploadAction =
  | BeginFileUpload
  | UpdateChecksumProgress
  | UpdateUploadProgress
  | SetUploadCancelable
  | SetUploadError
  | CancelFileUpload
  | FinalizeUpload
  | ContentPublishingActions.CreateNewContentItemSucceeded
  | ContentPublishingActions.UpdateContentItemSucceeded
  | ContentPublishingActions.PublishContentFilesSucceeded
  ;
