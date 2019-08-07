import { ProgressSummary } from '../progress-monitor';

/**
 * Upload Information
 */
export interface UploadState {
  cancelable: boolean;
  checksum: string;
  errorMsg: string;
  checksumProgress: ProgressSummary;
  uploadProgress: ProgressSummary;
}
