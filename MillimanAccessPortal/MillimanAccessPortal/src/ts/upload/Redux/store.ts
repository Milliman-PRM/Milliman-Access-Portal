import { ProgressSummary } from '../progress-monitor';

/**
 * Upload Information
 */
export interface UploadState {
  cancelable: boolean;
  errorMsg: string;
  checksumProgress: ProgressSummary;
  uploadProgress: ProgressSummary;
}
