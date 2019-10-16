import * as _ from 'lodash';
import { reducer as toastrReducer } from 'react-redux-toastr';
import { combineReducers } from 'redux';

import { generateUniqueId } from '../../../upload/generate-unique-identifier';
import { ProgressSummary } from '../../../upload/progress-monitor';
import * as UploadActions from '../../../upload/Redux/actions';
import { uploadStatus } from '../../../upload/Redux/reducers';
import { UploadState } from '../../../upload/Redux/store';
import {
  AssociatedContentItemUpload, ContentItemDetail, ContentItemFormErrors, RelatedFiles,
} from '../../models';
import { CardAttributes } from '../../shared-components/card/card';
import { createReducerCreator } from '../../shared-components/redux/reducers';
import { Dict, FilterState } from '../../shared-components/redux/store';
import * as PublishingActions from './actions';
import { FilterPublishingAction, PublishingAction } from './actions';
import {
  PendingDataState, PublishingFormData, PublishingState, PublishingStateData,
  PublishingStateSelected,
} from './store';

const _initialData: PublishingStateData = {
  clients: {},
  items: {},
  contentTypes: {},
  contentAssociatedFileTypes: {},
  publications: {},
  publicationQueue: {},
};

const emptyContentItemDetail: ContentItemDetail = {
  clientId: '',
  contentDisclaimer: '',
  contentName: '',
  contentTypeId: '',
  contentDescription: '',
  doesReduce: false,
  id: '',
  isSuspended: false,
  contentNotes: '',
  relatedFiles: {
    MasterContent: {
      filePurpose: '',
      fileOriginalName: '',
      uniqueUploadId: '',
    },
    Thumbnail: {
      filePurpose: '',
      fileOriginalName: '',
      uniqueUploadId: '',
    },
    UserGuide: {
      filePurpose: '',
      fileOriginalName: '',
      uniqueUploadId: '',
    },
    ReleaseNotes: {
      filePurpose: '',
      fileOriginalName: '',
      uniqueUploadId: '',
    },
  },
  associatedFiles: {},
  typeSpecificDetailObject: {},
};

const emptyContentItemErrors: ContentItemFormErrors = {
  clientId: '',
  contentDisclaimer: '',
  contentName: '',
  contentTypeId: '',
  contentDescription: '',
  doesReduce: '',
  id: '',
  isSuspended: '',
  contentNotes: '',
  relatedFiles: {
    MasterContent: '',
    Thumbnail: '',
    UserGuide: '',
    ReleaseNotes: '',
  },
  associatedFiles: {},
  typeSpecificDetailObject: {},
};

const _initialFormData: PublishingFormData = {
  originalData: emptyContentItemDetail,
  formData: emptyContentItemDetail,
  formErrors: emptyContentItemErrors,
  uploads: {},
  formState: 'read',
};

const _initialPendingData: PendingDataState = {
  globalData: false,
  clients: false,
  items: false,
  contentItemDetail: false,
  formSubmit: false,
};

/**
 * Create reducers for a subtree of the redux store
 * @param initialState Subtree of state these handlers can influence
 * @param handlers Actions and their state transformations
 */
const createReducer = createReducerCreator<PublishingAction>();

/**
 * Create a reducer for a filter
 * @param actionType Single filter action
 */
const createFilterReducer = (actionType: FilterPublishingAction['type']) =>
  createReducer({ text: '' }, {
    [actionType]: (state: FilterState, action: FilterPublishingAction) => ({
      ...state,
      text: action.text,
    }),
  });

const clientCardAttributes = createReducer<Dict<CardAttributes>>({},
  {
    FETCH_CLIENTS_SUCCEEDED: (__, { response }: PublishingActions.FetchClientsSucceeded) => ({
      ..._.mapValues(response.clients, (client) => ({ disabled: !client.canManage })),
    }),
  },
);

const pendingData = createReducer<PendingDataState>(_initialPendingData, {
  FETCH_GLOBAL_DATA: (state) => ({
    ...state,
    globalData: true,
  }),
  FETCH_GLOBAL_DATA_SUCCEEDED: (state) => ({
    ...state,
    globalData: false,
  }),
  FETCH_GLOBAL_DATA_FAILED: (state) => ({
    ...state,
    globalData: false,
  }),
  FETCH_CLIENTS: (state) => ({
    ...state,
    clients: true,
  }),
  FETCH_CLIENTS_SUCCEEDED: (state) => ({
    ...state,
    clients: false,
  }),
  FETCH_CLIENTS_FAILED: (state) => ({
    ...state,
    clients: false,
  }),
  FETCH_ITEMS: (state) => ({
    ...state,
    items: true,
  }),
  FETCH_ITEMS_SUCCEEDED: (state) => ({
    ...state,
    items: false,
  }),
  FETCH_ITEMS_FAILED: (state) => ({
    ...state,
    items: false,
  }),
});

const pendingStatusTries = createReducer<number>(5, {
  DECREMENT_STATUS_REFRESH_ATTEMPTS: (state) => state ? state - 1 : 0,
  FETCH_STATUS_REFRESH_SUCCEEDED: () => 5,
});

const data = createReducer<PublishingStateData>(_initialData, {
  FETCH_GLOBAL_DATA_SUCCEEDED: (state, action: PublishingActions.FetchGlobalDataSucceeded) => ({
    ...state,
    contentTypes: {
      ...action.response.contentTypes,
    },
    contentAssociatedFileTypes: {
      ...action.response.contentAssociatedFileTypes,
    },
  }),
  FETCH_CLIENTS_SUCCEEDED: (state, action: PublishingActions.FetchClientsSucceeded) => ({
    ...state,
    clients: {
      ...action.response.clients,
    },
  }),
  FETCH_ITEMS_SUCCEEDED: (state, action: PublishingActions.FetchItemsSucceeded) => {
    const { contentItems, publications, publicationQueue, clientStats } = action.response;
    return {
      ...state,
      items: contentItems,
      publications,
      publicationQueue,
      clients: {
        ...state.clients,
        [clientStats.id]: {
          ...state.clients[clientStats.id],
          ...clientStats,
        },
      },
    };
  },
  FETCH_STATUS_REFRESH_SUCCEEDED: (state, action: PublishingActions.FetchStatusRefreshSucceeded) => {
    const items = { ...state.items };
    _.forEach(items, (item, itemId) => {
      if (action.response.contentItems[itemId]) {
        items[itemId] = {
          ...item,
          ...action.response.contentItems[itemId],
        };
      }
    });

    return {
      ...state,
      items,
      publications: action.response.publications,
      publicationQueue: action.response.publicationQueue,
    };
  },
});

const formData = createReducer<PublishingFormData>(_initialFormData, {
  FETCH_CONTENT_ITEM_DETAIL_SUCCEEDED: (state, action: PublishingActions.FetchContentItemDetailSucceeded) => {
    const keys = Object.keys({ ...action.response.associatedFiles });
    const associatedContentItems: Dict<AssociatedContentItemUpload> = {};

    for (const key of keys) {
      if (action.response.associatedFiles.hasOwnProperty(key)) {
        associatedContentItems[key] = {
          ...action.response.associatedFiles[key],
          uniqueUploadId: generateUniqueId('associatedContent'),
        };
      }
    }

    const defaultIfUndefined = (purpose: any, value: string, defaultValue = '') => {
      return (purpose !== undefined) && purpose.hasOwnProperty(value) ? purpose[value] : defaultValue;
    };

    const contentItemDetail = {
      ...action.response,
      relatedFiles: {
        MasterContent: {
          filePurpose: defaultIfUndefined(action.response.relatedFiles.MasterContent, 'filePurpose'),
          fileOriginalName: defaultIfUndefined(action.response.relatedFiles.MasterContent, 'fileOriginalName'),
          uniqueUploadId: generateUniqueId('MasterContent'),
        },
        Thumbnail: {
          filePurpose: defaultIfUndefined(action.response.relatedFiles.Thumbnail, 'filePurpose'),
          fileOriginalName: defaultIfUndefined(action.response.relatedFiles.Thumbnail, 'fileOriginalName'),
          uniqueUploadId: generateUniqueId('Thumbnail'),
        },
        UserGuide: {
          filePurpose: defaultIfUndefined(action.response.relatedFiles.UserGuide, 'filePurpose'),
          fileOriginalName: defaultIfUndefined(action.response.relatedFiles.UserGuide, 'fileOriginalName'),
          uniqueUploadId: generateUniqueId('UserGuide'),
        },
        ReleaseNotes: {
          filePurpose: defaultIfUndefined(action.response.relatedFiles.ReleaseNotes, 'filePurpose'),
          fileOriginalName: defaultIfUndefined(action.response.relatedFiles.ReleaseNotes, 'fileOriginalName'),
          uniqueUploadId: generateUniqueId('ReleaseNotes'),
        },
      },
      associatedFiles: {
        ...associatedContentItems,
      },
    };

    const newUpload: UploadState = {
      cancelable: false,
      errorMsg: null,
      checksumProgress: ProgressSummary.empty(),
      uploadProgress: ProgressSummary.empty(),
    };

    const uploads: Dict<UploadState> = {
      [contentItemDetail.relatedFiles.MasterContent.uniqueUploadId]: newUpload,
      [contentItemDetail.relatedFiles.Thumbnail.uniqueUploadId]: newUpload,
      [contentItemDetail.relatedFiles.UserGuide.uniqueUploadId]: newUpload,
      [contentItemDetail.relatedFiles.ReleaseNotes.uniqueUploadId]: newUpload,
    };

    for (const key of keys) {
      if (action.response.associatedFiles.hasOwnProperty(key)) {
        uploads[contentItemDetail.associatedFiles[key].uniqueUploadId] = newUpload;
      }
    }

    return {
      originalData: {
        ...contentItemDetail,
      },
      formData: {
        ...contentItemDetail,
      },
      formErrors: {},
      uploads: {
        ...uploads,
      },
      formState: state.formState,
    };
  },
  SET_PENDING_TEXT_INPUT_VALUE: (state, action: PublishingActions.SetPublishingFormTextInputValue) => ({
    ...state,
    formData: {
      ...state.formData,
      [action.inputName]: action.value,
    },
  }),
  SET_CONTENT_ITEM_FORM_STATE: (state, action: PublishingActions.SetContentItemFormState) => ({
    ...state,
    formState: action.formState,
  }),
  SET_PENDING_BOOLEAN_INPUT_VALUE: (state, action: PublishingActions.SetPublishingFormBooleanInputValue) => ({
    ...state,
    formData: {
      ...state.formData,
      [action.inputName]: action.value,
    },
  }),
  RESET_CONTENT_ITEM_FORM: (state) => {
    const { originalData } = state;

    const keys = Object.keys({ ...originalData.associatedFiles });
    const associatedContentItems: Dict<AssociatedContentItemUpload> = {};

    for (const key of keys) {
      if (originalData.associatedFiles.hasOwnProperty(key)) {
        associatedContentItems[key] = {
          ...originalData.associatedFiles[key],
          uniqueUploadId: generateUniqueId('associatedContent'),
        };
      }
    }

    const defaultIfUndefined = (purpose: any, value: string, defaultValue = '') => {
      return (purpose !== undefined) && purpose.hasOwnProperty(value) ? purpose[value] : defaultValue;
    };

    const contentItemDetail = {
      ...originalData,
      relatedFiles: {
        MasterContent: {
          filePurpose: defaultIfUndefined(originalData.relatedFiles.MasterContent, 'filePurpose'),
          fileOriginalName: defaultIfUndefined(originalData.relatedFiles.MasterContent, 'fileOriginalName'),
          uniqueUploadId: generateUniqueId('MasterContent'),
        },
        Thumbnail: {
          filePurpose: defaultIfUndefined(originalData.relatedFiles.Thumbnail, 'filePurpose'),
          fileOriginalName: defaultIfUndefined(originalData.relatedFiles.Thumbnail, 'fileOriginalName'),
          uniqueUploadId: generateUniqueId('Thumbnail'),
        },
        UserGuide: {
          filePurpose: defaultIfUndefined(originalData.relatedFiles.UserGuide, 'filePurpose'),
          fileOriginalName: defaultIfUndefined(originalData.relatedFiles.UserGuide, 'fileOriginalName'),
          uniqueUploadId: generateUniqueId('UserGuide'),
        },
        ReleaseNotes: {
          filePurpose: defaultIfUndefined(originalData.relatedFiles.ReleaseNotes, 'filePurpose'),
          fileOriginalName: defaultIfUndefined(originalData.relatedFiles.ReleaseNotes, 'fileOriginalName'),
          uniqueUploadId: generateUniqueId('ReleaseNotes'),
        },
      },
      associatedFiles: {
        ...associatedContentItems,
      },
    };

    const newUpload: UploadState = {
      cancelable: false,
      errorMsg: null,
      checksumProgress: ProgressSummary.empty(),
      uploadProgress: ProgressSummary.empty(),
    };

    const uploads: Dict<UploadState> = {
      [contentItemDetail.relatedFiles.MasterContent.uniqueUploadId]: newUpload,
      [contentItemDetail.relatedFiles.Thumbnail.uniqueUploadId]: newUpload,
      [contentItemDetail.relatedFiles.UserGuide.uniqueUploadId]: newUpload,
      [contentItemDetail.relatedFiles.ReleaseNotes.uniqueUploadId]: newUpload,
    };

    for (const key of keys) {
      if (originalData.associatedFiles.hasOwnProperty(key)) {
        uploads[contentItemDetail.associatedFiles[key].uniqueUploadId] = newUpload;
      }
    }

    return {
      ...state,
      originalData: {
        ...contentItemDetail,
      },
      formData: {
        ...contentItemDetail,
      },
      uploads,
    };
  },
  BEGIN_FILE_UPLOAD: (state, action: UploadActions.BeginFileUpload) => {
    const relatedFiles: RelatedFiles = { ...state.formData.relatedFiles };
    const associatedFiles: Dict<AssociatedContentItemUpload> = { ...state.formData.associatedFiles };

    if (action.fileName.split('-')[0] !== 'associatedContent') {
      const relatedFilesKeys = Object.keys(state.formData.relatedFiles);
      for (const key of relatedFilesKeys) {
        if (relatedFiles.hasOwnProperty(key) &&
          relatedFiles[key].uniqueUploadId === action.uploadId) {
          relatedFiles[key] = {
            ...relatedFiles[key],
            fileOriginalName: action.fileName,
          };
        }
      }
    } else {
      const associatedContentKeys = Object.keys(state.formData.associatedFiles);
      for (const key of associatedContentKeys) {
        if (associatedFiles.hasOwnProperty(key) &&
          associatedFiles[key].uniqueUploadId === action.uploadId) {
          associatedFiles[key] = {
            ...associatedFiles[key],
            fileOriginalName: action.fileName,
          };
        }
      }
    }

    return {
      ...state,
      formData: {
        ...state.formData,
        relatedFiles: {
          ...relatedFiles,
        },
        associatedFiles: {
          ...associatedFiles,
        },
      },
      uploads: {
        ...state.uploads,
        [action.uploadId]: {
          ...state.uploads[action.uploadId],
          errorMsg: '',
          cancelable: true,
        },
      },
    };
  },
  UPDATE_CHECKSUM_PROGRESS: (state, action: UploadActions.UpdateChecksumProgress) => ({
    ...state,
    uploads: {
      ...state.uploads,
     [action.uploadId]: {
        ...state.uploads[action.uploadId],
        checksumProgress: action.progress,
      },
    },
  }),
  UPDATE_UPLOAD_PROGRESS: (state, action: UploadActions.UpdateUploadProgress) => ({
    ...state,
    uploads: {
      ...state.uploads,
      [action.uploadId]: {
        ...state.uploads[action.uploadId],
        uploadProgress: action.progress,
      },
    },
  }),
  SET_UPLOAD_ERROR: (state, action: UploadActions.SetUploadError) => ({
    ...state,
    uploads: {
      ...state.uploads,
      [action.uploadId]: {
        ...state.uploads[action.uploadId],
        errorMsg: action.errorMsg,
      },
    },
  }),
});

const selected = createReducer<PublishingStateSelected>(
  {
    client: null,
    item: null,
  },
  {
    SELECT_CLIENT: (state, action: PublishingActions.SelectClient) => ({
      client: action.id === state.client ? null : action.id,
      item: null,
    }),
    SELECT_ITEM: (state, action: PublishingActions.SelectItem) => ({
      ...state,
      item: action.id === state.item ? null : action.id,
    }),
  },
);

const cardAttributes = combineReducers({
  client: clientCardAttributes,
});

const pending = combineReducers({
  data: pendingData,
  statusTries: pendingStatusTries,
  uploads: uploadStatus,
});

const filters = combineReducers({
  client: createFilterReducer('SET_FILTER_TEXT_CLIENT'),
  item: createFilterReducer('SET_FILTER_TEXT_ITEM'),
});

export const contentPublishing = combineReducers({
  data,
  formData,
  selected,
  cardAttributes,
  pending,
  filters,
  toastr: toastrReducer,
});
