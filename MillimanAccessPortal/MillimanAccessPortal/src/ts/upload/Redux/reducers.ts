import * as _ from 'lodash';
import { reducer as toastrReducer } from 'react-redux-toastr';

import { createReducerCreator } from '../../react/shared-components/redux/reducers';
import { Dict, FilterState } from '../../react/shared-components/redux/store';
import * as UploadActions from './actions';
import { UploadPending, UploadState } from './store';
import { ProgressSummary } from '../progress-monitor';

const _initialUpload: UploadState = {
  cancelable: false,
  checksum: null,
  errorMsg: null,
  checksumProgress: ProgressSummary.empty(),
  uploadProgress: ProgressSummary.empty(),
};

const createReducer = createReducerCreator<UploadActions.PageUploadAction>();

export const uploadStatus = createReducer<Dict<UploadState>>({},
  {
    BEGIN_FILE_UPLOAD: (state, { uploadId }: UploadActions.BeginFileUpload) => ({
      ...state,
      [uploadId]: _initialUpload,
    }),
    SET_UPLOAD_CANCELABLE: (state, { uploadId, cancelable }: UploadActions.SetUploadCancelable) => ({
      ...state,
      [uploadId]: {
        ...state[uploadId],
        cancelable: cancelable,
      },
    }),
    SET_CHECKSUM_VALUE: (state, { uploadId, checksum }: UploadActions.SetChecksumValue) => ({
      ...state,
      [uploadId]: {
        ...state[uploadId],
        checksum: checksum,
      },
    }),
    UPDATE_CHECKSUM_PROGRESS: (state, { uploadId, progress }: UploadActions.UpdateChecksumProgress) => ({
      ...state,
      [uploadId]: {
        ...state[uploadId],
        checksumProgress: progress,
      },
    }),
    UPDATE_UPLOAD_PROGRESS: (state, { uploadId, progress }: UploadActions.UpdateUploadProgress) => ({
      ...state,
      [uploadId]: {
        ...state[uploadId],
        uploadProgress: progress,
      },
    }),
    SET_UPLOAD_ERROR: (state, { uploadId, errorMsg }: UploadActions.SetUploadError) => ({
      ...state,
      [uploadId]: {
        cancelable: true,
        checksum: null,
        errorMsg: errorMsg,
        checksumProgress: ProgressSummary.empty(),
        uploadProgress: ProgressSummary.empty(),
      },
    }),
    CANCEL_FILE_UPLOAD: (state, { uploadId }: UploadActions.CancelFileUpload) => {
      const Uploads = { ...state };
      delete Uploads[uploadId];
      return Uploads;
    },
    FINALIZE_UPLOAD: (state, { uploadId }: UploadActions.FinalizeUpload) => {
      const Uploads = { ...state };
      delete Uploads[uploadId];
      return Uploads;
    },
  },
);
