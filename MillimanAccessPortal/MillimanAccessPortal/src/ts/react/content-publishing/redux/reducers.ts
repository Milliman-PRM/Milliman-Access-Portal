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
  GoLiveSummaryData, PendingDataState, PublishingFormData, PublishingState,
  PublishingStateData, PublishingStateSelected,
} from './store';

const defaultIfUndefined = (purpose: any, value: string, defaultValue = '') => {
  return (purpose !== undefined) && purpose.hasOwnProperty(value) ? purpose[value] : defaultValue;
};

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
      fileOriginalName: '',
      uniqueUploadId: '',
      fileUploadId: '',
    },
    Thumbnail: {
      fileOriginalName: '',
      uniqueUploadId: '',
      fileUploadId: '',
    },
    UserGuide: {
      fileOriginalName: '',
      uniqueUploadId: '',
      fileUploadId: '',
    },
    ReleaseNotes: {
      fileOriginalName: '',
      uniqueUploadId: '',
      fileUploadId: '',
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

const _initialGoLiveData: GoLiveSummaryData = {
  rootContentItemId: null,
  goLiveSummary: null,
};

const _initialPendingData: PendingDataState = {
  globalData: false,
  clients: false,
  items: false,
  contentItemDetail: false,
  goLiveSummary: false,
  contentItemDeletion: false,
  formSubmit: false,
  publishing: false,
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
  CREATE_NEW_CONTENT_ITEM: (state) => ({
    ...state,
    formSubmit: true,
  }),
  CREATE_NEW_CONTENT_ITEM_SUCCEEDED: (state) => ({
    ...state,
    formSubmit: false,
  }),
  CREATE_NEW_CONTENT_ITEM_FAILED: (state) => ({
    ...state,
    formSubmit: false,
  }),
  UPDATE_CONTENT_ITEM: (state) => ({
    ...state,
    formSubmit: true,
  }),
  UPDATE_CONTENT_ITEM_SUCCEEDED: (state) => ({
    ...state,
    formSubmit: false,
  }),
  UPDATE_CONTENT_ITEM_FAILED: (state) => ({
    ...state,
    formSubmit: false,
  }),
  PUBLISH_CONTENT_FILES: (state) => ({
    ...state,
    publishing: true,
  }),
  PUBLISH_CONTENT_FILES_SUCCEEDED: (state) => ({
    ...state,
    publishing: false,
  }),
  PUBLISH_CONTENT_FILES_FAILED: (state) => ({
    ...state,
    publishing: false,
  }),
  DELETE_CONTENT_ITEM: (state) => ({
    ...state,
    contentItemDeletion: true,
  }),
  DELETE_CONTENT_ITEM_SUCCEEDED: (state) => ({
    ...state,
    contentItemDeletion: false,
  }),
  DELETE_CONTENT_ITEM_FAILED: (state) => ({
    ...state,
    contentItemDeletion: false,
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
  CREATE_NEW_CONTENT_ITEM_SUCCEEDED: (state, action: PublishingActions.CreateNewContentItemSucceeded) => {
    const { detail, summary } = action.response;
    return {
      ...state,
      items: {
        ...state.items,
        [detail.id]: {
          id: detail.id,
          clientId: detail.clientId,
          name: detail.contentName,
          contentTypeId: detail.contentTypeId,
          doesReduce: detail.doesReduce,
          isSuspended: detail.isSuspended,
          assignedUserCount: summary.assignedUserCount,
          selectionGroupCount: summary.groupCount,
        },
      },
    };
  },
  UPDATE_CONTENT_ITEM_SUCCEEDED: (state, action: PublishingActions.UpdateContentItemSucceeded) => {
    const { detail, summary } = action.response;
    return {
      ...state,
      items: {
        ...state.items,
        [detail.id]: {
          id: detail.id,
          clientId: detail.clientId,
          name: detail.contentName,
          contentTypeId: detail.contentTypeId,
          doesReduce: detail.doesReduce,
          isSuspended: detail.isSuspended,
          assignedUserCount: summary.assignedUserCount,
          selectionGroupCount: summary.groupCount,
        },
      },
    };
  },
  DELETE_CONTENT_ITEM_SUCCEEDED: (state, action: PublishingActions.DeleteContentItemSucceeded) => {
    const items = { ...state.items };
    delete items[action.response.id];

    return {
      ...state,
      items,
    };
  },
  CANCEL_PUBLICATION_REQUEST_SUCCEEDED: (state, action: PublishingActions.CancelPublicationRequestSucceeded) => {
    const { publications, publicationQueue, contentItems: items } = action.response.statusResponseModel;

    return {
      ...state,
      items,
      publications,
      publicationQueue,
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

    const contentItemDetail = {
      ...action.response,
      relatedFiles: {
        MasterContent: {
          fileOriginalName: defaultIfUndefined(action.response.relatedFiles.MasterContent, 'fileOriginalName'),
          uniqueUploadId: generateUniqueId('MasterContent'),
          fileUploadId: '',
        },
        Thumbnail: {
          fileOriginalName: defaultIfUndefined(action.response.relatedFiles.Thumbnail, 'fileOriginalName'),
          uniqueUploadId: generateUniqueId('Thumbnail'),
          fileUploadId: '',
        },
        UserGuide: {
          fileOriginalName: defaultIfUndefined(action.response.relatedFiles.UserGuide, 'fileOriginalName'),
          uniqueUploadId: generateUniqueId('UserGuide'),
          fileUploadId: '',
        },
        ReleaseNotes: {
          fileOriginalName: defaultIfUndefined(action.response.relatedFiles.ReleaseNotes, 'fileOriginalName'),
          uniqueUploadId: generateUniqueId('ReleaseNotes'),
          fileUploadId: '',
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
  SET_FORM_FOR_NEW_CONTENT_ITEM: (_state, action: PublishingActions.SetFormForNewContentItem) => {
    const contentItemDetail: ContentItemDetail = emptyContentItemDetail;

    contentItemDetail.clientId = action.clientId;
    contentItemDetail.relatedFiles.MasterContent.uniqueUploadId = generateUniqueId('MasterContent');
    contentItemDetail.relatedFiles.Thumbnail.uniqueUploadId = generateUniqueId('Thumbnail');
    contentItemDetail.relatedFiles.UserGuide.uniqueUploadId = generateUniqueId('UserGuide');
    contentItemDetail.relatedFiles.ReleaseNotes.uniqueUploadId = generateUniqueId('ReleaseNotes');

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

    const emptyContentItemFormData: PublishingFormData = {
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
      formState: 'write',
    };

    return emptyContentItemFormData;
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

    const contentItemDetail = {
      ...originalData,
      relatedFiles: {
        MasterContent: {
          fileOriginalName: defaultIfUndefined(originalData.relatedFiles.MasterContent, 'fileOriginalName'),
          uniqueUploadId: generateUniqueId('MasterContent'),
          fileUploadId: '',
        },
        Thumbnail: {
          fileOriginalName: defaultIfUndefined(originalData.relatedFiles.Thumbnail, 'fileOriginalName'),
          uniqueUploadId: generateUniqueId('Thumbnail'),
          fileUploadId: '',
        },
        UserGuide: {
          fileOriginalName: defaultIfUndefined(originalData.relatedFiles.UserGuide, 'fileOriginalName'),
          uniqueUploadId: generateUniqueId('UserGuide'),
          fileUploadId: '',
        },
        ReleaseNotes: {
          fileOriginalName: defaultIfUndefined(originalData.relatedFiles.ReleaseNotes, 'fileOriginalName'),
          uniqueUploadId: generateUniqueId('ReleaseNotes'),
          fileUploadId: '',
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
  FINALIZE_UPLOAD: (state, action: UploadActions.FinalizeUpload) => {
    const fileUploads = { ...state.formData.relatedFiles };
    for (const key in fileUploads) {
      if (fileUploads[key].uniqueUploadId === action.uploadId) {
        fileUploads[key].fileUploadId = action.Guid;
      }
    }

    return {
      ...state,
      formData: {
        ...state.formData,
        relatedFiles: fileUploads,
      },
    };
  },
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
  CREATE_NEW_CONTENT_ITEM_SUCCEEDED: (state, action: PublishingActions.CreateNewContentItemSucceeded) => {
    const { detail } = action.response;
    const newContentItemData: ContentItemDetail = {
      ...state.originalData,
      id: detail.id,
      clientId: detail.clientId,
      contentName: detail.contentName,
      contentTypeId: detail.contentTypeId,
      doesReduce: detail.doesReduce,
      contentDescription: detail.contentDescription,
      contentDisclaimer: detail.contentDisclaimer,
      contentNotes: detail.contentNotes,
      typeSpecificDetailObject: detail.typeSpecificDetailObject,
    };

    return {
      ...state,
      originalData: newContentItemData,
      formData: newContentItemData,
      formState: 'read',
    };
  },
  UPDATE_CONTENT_ITEM_SUCCEEDED: (state, action: PublishingActions.UpdateContentItemSucceeded) => {
    const { detail } = action.response;
    const updatedContentItemData: ContentItemDetail = {
      ...state.originalData,
      id: detail.id,
      clientId: detail.clientId,
      contentName: detail.contentName,
      contentTypeId: detail.contentTypeId,
      doesReduce: detail.doesReduce,
      contentDescription: detail.contentDescription,
      contentDisclaimer: detail.contentDisclaimer,
      contentNotes: detail.contentNotes,
      typeSpecificDetailObject: detail.typeSpecificDetailObject,
    };

    return {
      ...state,
      originalData: updatedContentItemData,
      formData: updatedContentItemData,
      formState: 'read',
    };
  },
  PUBLISH_CONTENT_FILES_SUCCEEDED: (state, action: PublishingActions.PublishContentFilesSucceeded) => {
    const { response: detail } = action;

    const keys = Object.keys({ ...detail.associatedFiles });
    const associatedContentItems: Dict<AssociatedContentItemUpload> = {};

    for (const key of keys) {
      if (detail.associatedFiles.hasOwnProperty(key)) {
        associatedContentItems[key] = {
          ...detail.associatedFiles[key],
          uniqueUploadId: generateUniqueId('associatedContent'),
        };
      }
    }

    const contentItemDetail = {
      ...state.formData,
      relatedFiles: {
        MasterContent: {
          fileOriginalName: defaultIfUndefined(detail.relatedFiles.MasterContent, 'fileOriginalName'),
          uniqueUploadId: generateUniqueId('MasterContent'),
          fileUploadId: '',
        },
        Thumbnail: {
          fileOriginalName: defaultIfUndefined(detail.relatedFiles.Thumbnail, 'fileOriginalName'),
          uniqueUploadId: generateUniqueId('Thumbnail'),
          fileUploadId: '',
        },
        UserGuide: {
          fileOriginalName: defaultIfUndefined(detail.relatedFiles.UserGuide, 'fileOriginalName'),
          uniqueUploadId: generateUniqueId('UserGuide'),
          fileUploadId: '',
        },
        ReleaseNotes: {
          fileOriginalName: defaultIfUndefined(detail.relatedFiles.ReleaseNotes, 'fileOriginalName'),
          uniqueUploadId: generateUniqueId('ReleaseNotes'),
          fileUploadId: '',
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
      if (detail.associatedFiles.hasOwnProperty(key)) {
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
      formState: 'read',
    };
  },
  CANCEL_PUBLICATION_REQUEST_SUCCEEDED: (state, action: PublishingActions.CancelPublicationRequestSucceeded) => {
    const { rootContentItemDetail: detail } = action.response;

    const keys = Object.keys({ ...detail.associatedFiles });
    const associatedContentItems: Dict<AssociatedContentItemUpload> = {};

    for (const key of keys) {
      if (detail.associatedFiles.hasOwnProperty(key)) {
        associatedContentItems[key] = {
          ...detail.associatedFiles[key],
          uniqueUploadId: generateUniqueId('associatedContent'),
        };
      }
    }

    const contentItemDetail = {
      ...state.formData,
      relatedFiles: {
        MasterContent: {
          fileOriginalName: defaultIfUndefined(detail.relatedFiles.MasterContent, 'fileOriginalName'),
          uniqueUploadId: generateUniqueId('MasterContent'),
          fileUploadId: '',
        },
        Thumbnail: {
          fileOriginalName: defaultIfUndefined(detail.relatedFiles.Thumbnail, 'fileOriginalName'),
          uniqueUploadId: generateUniqueId('Thumbnail'),
          fileUploadId: '',
        },
        UserGuide: {
          fileOriginalName: defaultIfUndefined(detail.relatedFiles.UserGuide, 'fileOriginalName'),
          uniqueUploadId: generateUniqueId('UserGuide'),
          fileUploadId: '',
        },
        ReleaseNotes: {
          fileOriginalName: defaultIfUndefined(detail.relatedFiles.ReleaseNotes, 'fileOriginalName'),
          uniqueUploadId: generateUniqueId('ReleaseNotes'),
          fileUploadId: '',
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
      if (detail.associatedFiles.hasOwnProperty(key)) {
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
      formErrors: {},
      uploads: {
        ...uploads,
      },
    };
  },
});

const goLiveSummaryData = createReducer<GoLiveSummaryData>(_initialGoLiveData, {
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
    SET_FORM_FOR_NEW_CONTENT_ITEM: (state) => ({
      ...state,
      item: state.item === 'NEW CONTENT ITEM' ? null : 'NEW CONTENT ITEM',
    }),
    DELETE_CONTENT_ITEM_SUCCEEDED: (state, action: PublishingActions.DeleteContentItemSucceeded) => {
      const item = (state.item === action.response.id) ? null : state.item;
      return {
        ...state,
        item,
      };
    },
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
  goLiveSummaryData,
  selected,
  cardAttributes,
  pending,
  filters,
  toastr: toastrReducer,
});
