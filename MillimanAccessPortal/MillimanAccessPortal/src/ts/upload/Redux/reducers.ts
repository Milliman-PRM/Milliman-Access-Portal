import * as _ from 'lodash';
import { reducer as toastrReducer } from 'react-redux-toastr';

import { createReducerCreator } from '../../react/shared-components/redux/reducers';
import { Dict, FilterState } from '../../react/shared-components/redux/store';
import { ProgressSummary } from '../progress-monitor';
import * as UploadActions from './actions';
import { UploadState } from './store';

const _initialUpload: UploadState = {
  cancelable: false,
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
        cancelable,
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
        errorMsg,
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
    CREATE_NEW_CONTENT_ITEM_SUCCEEDED: () => {
      return {};
    },
    UPDATE_CONTENT_ITEM_SUCCEEDED: () => {
      return {};
    },
    PUBLISH_CONTENT_FILES_SUCCEEDED: () => {
      return {};
    },
  },
);
