import * as toastr from 'react-redux-toastr';
import { applyMiddleware, createStore } from 'redux';
import { composeWithDevTools } from 'redux-devtools-extension';
import createSagaMiddleware from 'redux-saga';

import { UploadState } from '../../../upload/Redux/store';
import { PreLiveContentValidationSummary } from '../../../view-models/content-publishing';
import {
  ClientWithStats, ContentAssociatedFileType, ContentItemDetail, ContentItemFormErrors,
  ContentPublicationRequest, ContentType, Guid, PublicationQueueDetails, RootContentItemWithStats,
} from '../../models';
import { CardAttributes } from '../../shared-components/card/card';
import { Dict, FilterState } from '../../shared-components/redux/store';
import { contentPublishing } from './reducers';
import sagas from './sagas';

/**
 * Flags indicating whether the page is waiting on new data for an entity type.
 */
export interface PendingDataState {
  globalData: boolean;
  clients: boolean;
  items: boolean;
  contentItemDetail: boolean;
  goLiveSummary: boolean;
  contentItemDeletion: boolean;
  formSubmit: boolean;
  publishing: boolean;
}

/**
 * Entity data returned from the server.
 */
export interface PublishingStateData {
  clients: Dict<ClientWithStats>;
  items: Dict<RootContentItemWithStats>;
  contentTypes: Dict<ContentType>;
  contentAssociatedFileTypes: Dict<ContentAssociatedFileType>;
  publications: Dict<ContentPublicationRequest>;
  publicationQueue: Dict<PublicationQueueDetails>;
}

/**
 * Selected cards.
 */
export interface PublishingStateSelected {
  client: Guid;
  item: Guid | 'NEW CONTENT ITEM';
}

/**
 * Card attribute collections.
 */
export interface PublishingStateCardAttributes {
  client: Dict<CardAttributes>;
  item: Dict<CardAttributes>;
}

/**
 * All state that represents a change pending submission.
 */
export interface PublishingStatePending {
  data: PendingDataState;
  statusTries: number;
  uploads: Dict<UploadState>;
}

/**
 * All filter state.
 */
export interface PublishingStateFilters {
  client: FilterState;
  item: FilterState;
}

/**
 * Form data
 */
export interface PublishingFormData {
  originalData: ContentItemDetail;
  formData: ContentItemDetail;
  formErrors: ContentItemFormErrors;
  uploads: Dict<UploadState>;
  formState: 'read' | 'write';
}

/**
 * Go-Live Summary data
 */
export interface GoLiveSummaryData {
  rootContentItemId: Guid;
  goLiveSummary: PreLiveContentValidationSummary;
}

/**
 * All content access admin state.
 */
export interface PublishingState {
  data: PublishingStateData;
  formData: PublishingFormData;
  goLiveSummary: GoLiveSummaryData;
  selected: PublishingStateSelected;
  cardAttributes: PublishingStateCardAttributes;
  pending: PublishingStatePending;
  filters: PublishingStateFilters;
  toastr: toastr.ToastrState;
}

// Create the store and apply saga middleware
const sagaMiddleware = createSagaMiddleware();
export const store = createStore(
  contentPublishing,
  composeWithDevTools(
    applyMiddleware(sagaMiddleware),
  ));
sagaMiddleware.run(sagas);
