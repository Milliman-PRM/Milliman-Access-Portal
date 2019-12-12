import * as _ from 'lodash';
import { reducer as toastrReducer } from 'react-redux-toastr';
import { combineReducers } from 'redux';

import { generateUniqueId } from '../../../upload/generate-unique-identifier';
import { ProgressSummary } from '../../../upload/progress-monitor';
import * as UploadActions from '../../../upload/Redux/actions';
import { uploadStatus } from '../../../upload/Redux/reducers';
import { UploadState } from '../../../upload/Redux/store';
import { PublicationStatus } from '../../../view-models/content-publishing';
import {
  AssociatedContentItemUpload, ContentItemDetail, ContentItemFormErrors, Guid, RelatedFiles,
} from '../../models';
import { CardAttributes } from '../../shared-components/card/card';
import { createReducerCreator, Handlers } from '../../shared-components/redux/reducers';
import { Dict, FilterState, ModalState } from '../../shared-components/redux/store';
import * as PublishingActions from './actions';
import { FilterPublishingAction, OpenModalAction, PublishingAction } from './actions';
import {
  AfterFormModal, ElementsToConfirm, GoLiveSummaryData, PendingDataState,
  PublishingFormData, PublishingStateData, PublishingStateSelected,
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
  thumbnailLink: '',
  typeSpecificDetailObject: {
    bookmarksPaneEnabled: false,
    filterPaneEnabled: false,
    navigationPaneEnabled: false,
  },
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
  originalFormData: emptyContentItemDetail,
  pendingFormData: emptyContentItemDetail,
  formErrors: emptyContentItemErrors,
  uploads: {},
  formState: 'read',
  disclaimerInputState: 'edit',
};

const _initialGoLiveData: GoLiveSummaryData = {
  rootContentItemId: null,
  goLiveSummary: null,
  elementsToConfirm: null,
  onlyChangesShown: false,
};

const _initialPendingData: PendingDataState = {
  globalData: false,
  clients: false,
  items: false,
  contentItemDetail: false,
  goLiveSummary: false,
  goLiveApproval: false,
  goLiveRejection: false,
  contentItemDeletion: false,
  cancelPublication: false,
  formSubmit: false,
  publishing: false,
};

const newUpload: UploadState = {
  cancelable: false,
  errorMsg: null,
  checksumProgress: ProgressSummary.empty(),
  uploadProgress: ProgressSummary.empty(),
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

/**
 * Create a reducer for a modal
 * @param openActions Actions that cause the modal to open
 * @param closeActions Actions that cause the modal to close
 */
const createModalReducer = (
  openActions: Array<OpenModalAction['type']>,
  closeActions: Array<PublishingAction['type']>,
) => {
  const handlers: Handlers<ModalState, any> = {};
  openActions.forEach((action) => {
    handlers[action] = (state) => ({
      ...state,
      isOpen: true,
    });
  });
  closeActions.forEach((action) => {
    handlers[action] = (state) => ({
      ...state,
      isOpen: false,
    });
  });
  return createReducer<ModalState>({ isOpen: false }, handlers);
};

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
  FETCH_GO_LIVE_SUMMARY: (state) => ({
    ...state,
    goLiveSummary: true,
  }),
  FETCH_GO_LIVE_SUMMARY_SUCCEEDED: (state) => ({
    ...state,
    goLiveSummary: false,
  }),
  FETCH_GO_LIVE_SUMMARY_FAILED: (state) => ({
    ...state,
    goLiveSummary: false,
  }),
  APPROVE_GO_LIVE_SUMMARY: (state) => ({
    ...state,
    goLiveApproval: true,
  }),
  APPROVE_GO_LIVE_SUMMARY_SUCCEEDED: (state) => ({
    ...state,
    goLiveApproval: false,
  }),
  APPROVE_GO_LIVE_SUMMARY_FAILED: (state) => ({
    ...state,
    goLiveApproval: false,
  }),
  REJECT_GO_LIVE_SUMMARY: (state) => ({
    ...state,
    goLiveRejection: true,
  }),
  REJECT_GO_LIVE_SUMMARY_SUCCEEDED: (state) => ({
    ...state,
    goLiveRejection: false,
  }),
  REJECT_GO_LIVE_SUMMARY_FAILED: (state) => ({
    ...state,
    goLiveRejection: false,
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
  CANCEL_PUBLICATION_REQUEST: (state) => ({
    ...state,
    cancelPublication: true,
  }),
  CANCEL_PUBLICATION_REQUEST_SUCCEEDED: (state) => ({
    ...state,
    cancelPublication: false,
  }),
  CANCEL_PUBLICATION_REQUEST_FAILED: (state) => ({
    ...state,
    cancelPublication: false,
  }),
});

const pendingStatusTries = createReducer<number>(5, {
  DECREMENT_STATUS_REFRESH_ATTEMPTS: (state) => state ? state - 1 : 0,
  FETCH_STATUS_REFRESH_SUCCEEDED: () => 5,
});

const contentItemToDelete = createReducer<Guid>(null, {
  OPEN_DELETE_CONTENT_ITEM_MODAL: (_state, action: PublishingActions.OpenDeleteContentItemModal) => action.id,
  CLOSE_DELETE_CONTENT_ITEM_MODAL: () => null,
  CLOSE_DELETE_CONFIRMATION_MODAL: () => null,
  DELETE_CONTENT_ITEM_SUCCEEDED: () => null,
});

const afterFormModal = createReducer<AfterFormModal>({ entityToSelect: null, entityType: null }, {
  OPEN_MODIFIED_FORM_MODAL: (_state, action: PublishingActions.OpenModifiedFormModal) => ({
    entityToSelect: action.afterFormModal.entityToSelect,
    entityType: action.afterFormModal.entityType,
  }),
  CLOSE_MODIFIED_FORM_MODAL: () => ({
    entityToSelect: null,
    entityType: null,
  }),
  SELECT_CLIENT: () => ({
    entityToSelect: null,
    entityType: null,
  }),
  SELECT_ITEM: () => ({
    entityToSelect: null,
    entityType: null,
  }),
  DELETE_CONTENT_ITEM_SUCCEEDED: () => ({
    entityToSelect: null,
    entityType: null,
  }),
  DELETE_CONTENT_ITEM_FAILED: () => ({
    entityToSelect: null,
    entityType: null,
  }),
  FETCH_CONTENT_ITEM_DETAIL_SUCCEEDED: () => ({
    entityToSelect: null,
    entityType: null,
  }),
  FETCH_CONTENT_ITEM_DETAIL_FAILED: () => ({
    entityToSelect: null,
    entityType: null,
  }),
  SET_FORM_FOR_NEW_CONTENT_ITEM: () => ({
    entityToSelect: null,
    entityType: null,
  }),
  RESET_CONTENT_ITEM_FORM: () => ({
    entityToSelect: null,
    entityType: null,
  }),
});

const publicationToCancel = createReducer<Guid>(null, {
  OPEN_CANCEL_PUBLICATION_MODAL: (_state, action: PublishingActions.OpenCancelPublicationModal) => action.id,
  CLOSE_CANCEL_PUBLICATION_MODAL: () => null,
  CANCEL_PUBLICATION_REQUEST_SUCCEEDED: () => null,
  CANCEL_PUBLICATION_REQUEST_FAILED: () => null,
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
  APPROVE_GO_LIVE_SUMMARY_SUCCEEDED: (state, action: PublishingActions.ApproveGoLiveSummarySucceeded) => {
    const { publicationRequestId } = action.response;
    return {
      ...state,
      publications: {
        ...state.publications,
        [publicationRequestId]: {
          ...state.publications[publicationRequestId],
          requestStatus: PublicationStatus.Confirming,
        },
      },
    };
  },
  REJECT_GO_LIVE_SUMMARY_SUCCEEDED: (state, action: PublishingActions.RejectGoLiveSummarySucceeded) => {
    const { publicationRequestId } = action.response;
    return {
      ...state,
      publications: {
        ...state.publications,
        [publicationRequestId]: {
          ...state.publications[publicationRequestId],
          requestStatus: PublicationStatus.Rejected,
        },
      },
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
      originalFormData: {
        ...contentItemDetail,
      },
      pendingFormData: {
        ...contentItemDetail,
      },
      formErrors: {},
      uploads: {
        ...uploads,
      },
      formState: state.formState,
      disclaimerInputState: 'edit',
    };
  },
  SET_FORM_FOR_NEW_CONTENT_ITEM: (_state, action: PublishingActions.SetFormForNewContentItem) => {
    const contentItemDetail: ContentItemDetail = emptyContentItemDetail;

    contentItemDetail.clientId = action.clientId;
    contentItemDetail.relatedFiles.MasterContent.uniqueUploadId = generateUniqueId('MasterContent');
    contentItemDetail.relatedFiles.Thumbnail.uniqueUploadId = generateUniqueId('Thumbnail');
    contentItemDetail.relatedFiles.UserGuide.uniqueUploadId = generateUniqueId('UserGuide');
    contentItemDetail.relatedFiles.ReleaseNotes.uniqueUploadId = generateUniqueId('ReleaseNotes');

    const uploads: Dict<UploadState> = {
      [contentItemDetail.relatedFiles.MasterContent.uniqueUploadId]: newUpload,
      [contentItemDetail.relatedFiles.Thumbnail.uniqueUploadId]: newUpload,
      [contentItemDetail.relatedFiles.UserGuide.uniqueUploadId]: newUpload,
      [contentItemDetail.relatedFiles.ReleaseNotes.uniqueUploadId]: newUpload,
    };

    const emptyContentItemFormData: PublishingFormData = {
      originalFormData: {
        ...contentItemDetail,
      },
      pendingFormData: {
        ...contentItemDetail,
      },
      formErrors: {},
      uploads: {
        ...uploads,
      },
      formState: 'write',
      disclaimerInputState: 'edit',
    };

    return emptyContentItemFormData;
  },
  SET_PENDING_TEXT_INPUT_VALUE: (state, action: PublishingActions.SetPublishingFormTextInputValue) => ({
    ...state,
    pendingFormData: {
      ...state.pendingFormData,
      [action.inputName]: action.value,
    },
  }),
  SET_CONTENT_ITEM_FORM_STATE: (state, action: PublishingActions.SetContentItemFormState) => ({
    ...state,
    formState: action.formState,
    disclaimerInputState: 'edit',
  }),
  SET_PENDING_BOOLEAN_INPUT_VALUE: (state, action: PublishingActions.SetPublishingFormBooleanInputValue) => {
    if (action.inputName === 'doesReduce' || action.inputName === 'isSuspended') {
      return {
        ...state,
        pendingFormData: {
          ...state.pendingFormData,
          [action.inputName]: action.value,
        },
      };
    } else {
      return {
        ...state,
        pendingFormData: {
          ...state.pendingFormData,
          typeSpecificDetailObject: {
            ...state.pendingFormData.typeSpecificDetailObject,
            [action.inputName]: action.value,
          },
        },
      };
    }
  },
  RESET_CONTENT_ITEM_FORM: (state) => {
    const { originalFormData } = state;

    const keys = Object.keys({ ...originalFormData.associatedFiles });
    const associatedContentItems: Dict<AssociatedContentItemUpload> = {};

    for (const key of keys) {
      if (originalFormData.associatedFiles.hasOwnProperty(key)) {
        associatedContentItems[key] = {
          ...originalFormData.associatedFiles[key],
          uniqueUploadId: generateUniqueId('associatedContent'),
        };
      }
    }

    const contentItemDetail = {
      ...originalFormData,
      relatedFiles: {
        MasterContent: {
          fileOriginalName: defaultIfUndefined(originalFormData.relatedFiles.MasterContent, 'fileOriginalName'),
          uniqueUploadId: generateUniqueId('MasterContent'),
          fileUploadId: '',
        },
        Thumbnail: {
          fileOriginalName: defaultIfUndefined(originalFormData.relatedFiles.Thumbnail, 'fileOriginalName'),
          uniqueUploadId: generateUniqueId('Thumbnail'),
          fileUploadId: '',
        },
        UserGuide: {
          fileOriginalName: defaultIfUndefined(originalFormData.relatedFiles.UserGuide, 'fileOriginalName'),
          uniqueUploadId: generateUniqueId('UserGuide'),
          fileUploadId: '',
        },
        ReleaseNotes: {
          fileOriginalName: defaultIfUndefined(originalFormData.relatedFiles.ReleaseNotes, 'fileOriginalName'),
          uniqueUploadId: generateUniqueId('ReleaseNotes'),
          fileUploadId: '',
        },
      },
      associatedFiles: {
        ...associatedContentItems,
      },
    };

    const uploads: Dict<UploadState> = {
      [contentItemDetail.relatedFiles.MasterContent.uniqueUploadId]: newUpload,
      [contentItemDetail.relatedFiles.Thumbnail.uniqueUploadId]: newUpload,
      [contentItemDetail.relatedFiles.UserGuide.uniqueUploadId]: newUpload,
      [contentItemDetail.relatedFiles.ReleaseNotes.uniqueUploadId]: newUpload,
    };

    for (const key of keys) {
      if (originalFormData.associatedFiles.hasOwnProperty(key)) {
        uploads[contentItemDetail.associatedFiles[key].uniqueUploadId] = newUpload;
      }
    }

    return {
      ...state,
      originalFormData: {
        ...contentItemDetail,
      },
      pendingFormData: {
        ...contentItemDetail,
      },
      uploads,
      disclaimerInputState: 'edit',
    };
  },
  REMOVE_EXISTING_FILE: (state, action: PublishingActions.RemoveExistingFile) => {
    const relatedFiles: RelatedFiles = { ...state.pendingFormData.relatedFiles };
    const associatedFiles: Dict<AssociatedContentItemUpload> = { ...state.pendingFormData.associatedFiles };

    if (action.uploadId.split('-')[0] !== 'associatedContent') {
      const relatedFilesKeys = Object.keys(state.pendingFormData.relatedFiles);
      for (const key of relatedFilesKeys) {
        if (relatedFiles.hasOwnProperty(key) &&
          relatedFiles[key].uniqueUploadId === action.uploadId) {
          relatedFiles[key] = {
            ...relatedFiles[key],
            fileUploadId: '',
            fileOriginalName: '[Pending Removal]',
          };
        }
      }
    } else {
      const associatedContentKeys = Object.keys(state.pendingFormData.associatedFiles);
      for (const key of associatedContentKeys) {
        if (associatedFiles.hasOwnProperty(key) &&
          associatedFiles[key].uniqueUploadId === action.uploadId) {
          associatedFiles[key] = {
            ...associatedFiles[key],
            fileOriginalName: '[Pending Removal]',
          };
        }
      }
    }

    const thumbnailLink = (action.uploadId.split('-')[0] !== 'Thumbnail')
      ? state.pendingFormData.thumbnailLink
      : '';

    return {
      ...state,
      pendingFormData: {
        ...state.pendingFormData,
        relatedFiles,
        associatedFiles,
        thumbnailLink,
      },
    };
  },
  BEGIN_FILE_UPLOAD: (state, action: UploadActions.BeginFileUpload) => {
    const relatedFiles: RelatedFiles = { ...state.pendingFormData.relatedFiles };
    const associatedFiles: Dict<AssociatedContentItemUpload> = { ...state.pendingFormData.associatedFiles };

    if (action.fileName.split('-')[0] !== 'associatedContent') {
      const relatedFilesKeys = Object.keys(state.pendingFormData.relatedFiles);
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
      const associatedContentKeys = Object.keys(state.pendingFormData.associatedFiles);
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
      pendingFormData: {
        ...state.pendingFormData,
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
  CANCEL_FILE_UPLOAD: (state, action: UploadActions.CancelFileUpload) => {
    const { relatedFiles } = state.pendingFormData;
    const relatedFilesKeys = Object.keys(state.pendingFormData.relatedFiles);
    let relatedFileType;
    for (const key of relatedFilesKeys) {
      if (relatedFiles.hasOwnProperty(key) &&
        relatedFiles[key].uniqueUploadId === action.uploadId) {
        relatedFileType = key;
      }
    }
    const newUploadId = generateUniqueId(relatedFileType);

    const uploads = { ...state.uploads };
    delete uploads[action.uploadId];
    uploads[newUploadId] = newUpload;

    return {
      ...state,
      originalFormData: {
        ...state.originalFormData,
        relatedFiles: {
          ...state.originalFormData.relatedFiles,
          [relatedFileType]: {
            ...state.originalFormData.relatedFiles[relatedFileType],
            uniqueUploadId: newUploadId,
            fileUploadId: '',
          },
        },
      },
      pendingFormData: {
        ...state.pendingFormData,
        relatedFiles: {
          ...state.pendingFormData.relatedFiles,
          [relatedFileType]: {
            ...state.pendingFormData.relatedFiles[relatedFileType],
            uniqueUploadId: newUploadId,
            fileUploadId: '',
            fileOriginalName: state.originalFormData.relatedFiles[relatedFileType].fileOriginalName,
          },
        },
      },
      uploads,
    };
  },
  FINALIZE_UPLOAD: (state, action: UploadActions.FinalizeUpload) => {
    const fileUploads = { ...state.pendingFormData.relatedFiles };
    for (const key in fileUploads) {
      if (fileUploads[key].uniqueUploadId === action.uploadId) {
        fileUploads[key].fileUploadId = action.Guid;
      }
    }

    return {
      ...state,
      pendingFormData: {
        ...state.pendingFormData,
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

    return {
      ...state,
      originalFormData: {
        ...state.originalFormData,
        id: detail.id,
        clientId: detail.clientId,
        contentName: detail.contentName,
        contentTypeId: detail.contentTypeId,
        doesReduce: detail.doesReduce,
        contentDescription: detail.contentDescription,
        contentDisclaimer: detail.contentDisclaimer,
        contentNotes: detail.contentNotes,
        typeSpecificDetailObject: detail.typeSpecificDetailObject,
      },
      pendingFormData: {
        ...state.pendingFormData,
        id: detail.id,
        clientId: detail.clientId,
        contentName: detail.contentName,
        contentTypeId: detail.contentTypeId,
        doesReduce: detail.doesReduce,
        contentDescription: detail.contentDescription,
        contentDisclaimer: detail.contentDisclaimer,
        contentNotes: detail.contentNotes,
        typeSpecificDetailObject: detail.typeSpecificDetailObject,
      },
      formState: 'read',
    };
  },
  UPDATE_CONTENT_ITEM_SUCCEEDED: (state, action: PublishingActions.UpdateContentItemSucceeded) => {
    const { detail } = action.response;
    const updatedContentItemData: ContentItemDetail = {
      ...state.originalFormData,
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
      originalFormData: updatedContentItemData,
      pendingFormData: updatedContentItemData,
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
      ...state.pendingFormData,
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
      originalFormData: {
        ...contentItemDetail,
      },
      pendingFormData: {
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
      ...state.pendingFormData,
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
      originalFormData: {
        ...contentItemDetail,
      },
      pendingFormData: {
        ...contentItemDetail,
      },
      formErrors: {},
      uploads: {
        ...uploads,
      },
    };
  },
  SET_DISCLAIMER_INPUT_STATE: (state, action: PublishingActions.SetDisclaimerInputState) => ({
    ...state,
    disclaimerInputState: action.value,
  }),
});

const goLiveSummary = createReducer<GoLiveSummaryData>(_initialGoLiveData, {
  FETCH_GO_LIVE_SUMMARY: (_state, action: PublishingActions.FetchGoLiveSummary) => ({
    rootContentItemId: action.request.rootContentItemId,
    goLiveSummary: null,
    elementsToConfirm: null,
    onlyChangesShown: false,
  }),
  FETCH_GO_LIVE_SUMMARY_SUCCEEDED: (state, action: PublishingActions.FetchGoLiveSummarySucceeded) => {
    const elementsToConfirm: ElementsToConfirm = {};
    if (action.response.masterContentLink) {
      elementsToConfirm.masterContent = false;
    }
    if (action.response.thumbnailLink) {
      elementsToConfirm.thumbnail = false;
    }
    if (action.response.userGuideLink) {
      elementsToConfirm.userguide = false;
    }
    if (action.response.releaseNotesLink) {
      elementsToConfirm.releaseNotes = false;
    }
    if (action.response.reductionHierarchy) {
      elementsToConfirm.reductionHierarchy = false;
    }
    if (action.response.selectionGroups) {
      elementsToConfirm.selectionGroups = false;
    }

    return {
      ...state,
      goLiveSummary: action.response,
      elementsToConfirm,
    };
  },
  FETCH_GO_LIVE_SUMMARY_FAILED: () => ({
    rootContentItemId: null,
    goLiveSummary: null,
    elementsToConfirm: null,
    onlyChangesShown: false,
  }),
  APPROVE_GO_LIVE_SUMMARY_SUCCEEDED: () => ({
    rootContentItemId: null,
    goLiveSummary: null,
    elementsToConfirm: null,
    onlyChangesShown: false,
  }),
  REJECT_GO_LIVE_SUMMARY_SUCCEEDED: () => ({
    rootContentItemId: null,
    goLiveSummary: null,
    elementsToConfirm: null,
    onlyChangesShown: false,
  }),
  SELECT_CLIENT: () => ({
    rootContentItemId: null,
    goLiveSummary: null,
    elementsToConfirm: null,
    onlyChangesShown: false,
  }),
  SELECT_ITEM: () => ({
    rootContentItemId: null,
    goLiveSummary: null,
    elementsToConfirm: null,
    onlyChangesShown: false,
  }),
  SET_FORM_FOR_NEW_CONTENT_ITEM: () => ({
    rootContentItemId: null,
    goLiveSummary: null,
    elementsToConfirm: null,
    onlyChangesShown: false,
  }),
  TOGGLE_SHOW_ONLY_CHANGES: (state) => ({
    ...state,
    onlyChangesShown: !state.onlyChangesShown,
  }),
  TOGGLE_GO_LIVE_CONFIRMATION_CHECKBOX: (
    state, action: PublishingActions.ToggleGoLiveConfirmationCheckbox,
  ) => {
    return {
      ...state,
      elementsToConfirm: {
        ...state.elementsToConfirm,
        [action.target]: action.status,
      },
    };
  },
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
    FETCH_GO_LIVE_SUMMARY: (state, action: PublishingActions.FetchGoLiveSummary) => ({
      ...state,
      item: action.request.rootContentItemId,
    }),
    APPROVE_GO_LIVE_SUMMARY_SUCCEEDED: (state) => ({
      ...state,
      item: null,
    }),
    REJECT_GO_LIVE_SUMMARY_SUCCEEDED: (state) => ({
      ...state,
      item: null,
    }),
    CREATE_NEW_CONTENT_ITEM_SUCCEEDED: (state, action: PublishingActions.CreateNewContentItemSucceeded) => ({
      ...state,
      item: action.response.detail.id,
    }),
  },
);

const modals = combineReducers({
  contentItemDeletion: createModalReducer(['OPEN_DELETE_CONTENT_ITEM_MODAL'], [
    'CLOSE_DELETE_CONTENT_ITEM_MODAL',
    'OPEN_DELETE_CONFIRMATION_MODAL',
  ]),
  contentItemDeleteConfirmation: createModalReducer(['OPEN_DELETE_CONFIRMATION_MODAL'], [
    'CLOSE_DELETE_CONFIRMATION_MODAL',
    'DELETE_CONTENT_ITEM_SUCCEEDED',
    'DELETE_CONTENT_ITEM_FAILED',
  ]),
  goLiveRejection: createModalReducer(['OPEN_GO_LIVE_REJECTION_MODAL'], [
    'CLOSE_GO_LIVE_REJECTION_MODAL',
    'REJECT_GO_LIVE_SUMMARY_SUCCEEDED',
    'REJECT_GO_LIVE_SUMMARY_FAILED',
  ]),
  formModified: createModalReducer(['OPEN_MODIFIED_FORM_MODAL'], [
    'CLOSE_MODIFIED_FORM_MODAL',
    'SELECT_CLIENT',
    'SELECT_ITEM',
    'OPEN_DELETE_CONTENT_ITEM_MODAL',
    'FETCH_CONTENT_ITEM_DETAIL',
    'RESET_CONTENT_ITEM_FORM',
    'SET_FORM_FOR_NEW_CONTENT_ITEM',
    'FETCH_GO_LIVE_SUMMARY',
  ]),
  cancelPublication: createModalReducer(['OPEN_CANCEL_PUBLICATION_MODAL'], [
    'CLOSE_CANCEL_PUBLICATION_MODAL',
    'CANCEL_PUBLICATION_REQUEST_FAILED',
    'CANCEL_PUBLICATION_REQUEST_SUCCEEDED',
  ]),
});

const cardAttributes = combineReducers({
  client: clientCardAttributes,
});

const pending = combineReducers({
  data: pendingData,
  statusTries: pendingStatusTries,
  uploads: uploadStatus,
  contentItemToDelete,
  publicationToCancel,
  afterFormModal,
});

const filters = combineReducers({
  client: createFilterReducer('SET_FILTER_TEXT_CLIENT'),
  item: createFilterReducer('SET_FILTER_TEXT_ITEM'),
});

export const contentPublishing = combineReducers({
  data,
  formData,
  goLiveSummary,
  selected,
  cardAttributes,
  pending,
  filters,
  modals,
  toastr: toastrReducer,
});
