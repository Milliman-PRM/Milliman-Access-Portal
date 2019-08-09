import * as toastr from 'react-redux-toastr';
import { applyMiddleware, createStore } from 'redux';
import { composeWithDevTools } from 'redux-devtools-extension';
import createSagaMiddleware from 'redux-saga';

import { UploadState } from '../../../upload/Redux/store';
import {
    ClientWithStats, ContentAssociatedFileType, ContentItemDetail, ContentPublicationRequest,
    ContentReductionTask, ContentType, Guid, PublicationQueueDetails, ReductionQueueDetails,
    RootContentItemWithStats, User,
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
}

/**
 * Entity data returned from the server.
 */
export interface PublishingStateData {
  clients: Dict<ClientWithStats>;
  items: Dict<RootContentItemWithStats>;
  contentItemDetail: ContentItemDetail | null;
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
  item: Guid;
}

/**
 * Card attribute collections.
 */
export interface PublishingStateCardAttributes {
  client: Dict<CardAttributes>;
  item: Dict<CardAttributes>;
}

/**
 * Content Item Form
 */
export interface PublishingContentItemFormData {
  name: string;
}

/**
 * All state that represents a change pending submission.
 */
export interface PublishingStatePending {
  data: PendingDataState;
  contentItemFormData: PublishingContentItemFormData;
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
 * All content access admin state.
 */
export interface PublishingState {
  data: PublishingStateData;
  selected: PublishingStateSelected;
  cardAttributes: PublishingStateCardAttributes;
  pending: PublishingStatePending;
  uploads: Dict<UploadState>;
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
