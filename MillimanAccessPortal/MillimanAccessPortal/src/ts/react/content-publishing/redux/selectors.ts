import * as _ from 'lodash';
import * as moment from 'moment';

import {
  publicationStatusNames, PublishRequest, UploadedRelatedFile,
} from '../../../view-models/content-publishing';
import {
  ClientWithStats, ContentItemPublicationDetail, ContentPublicationRequest,
  ContentReductionTask, Guid, RootContentItemWithStats,
} from '../../models';
import { PublishingState } from './store';

// Utility functions
const sortReductions = (left: ContentReductionTask, right: ContentReductionTask) =>
  sortMomentDescending(left.createDateTimeUtc, right.createDateTimeUtc);
const sortPublications = (
  left: ContentPublicationRequest,
  right: ContentPublicationRequest) =>
  sortMomentDescending(left.createDateTimeUtc, right.createDateTimeUtc);

const sortMomentDescending = (left: string, right: string) =>
  moment(left).isBefore(right)
    ? 1
    : moment(left).isAfter(right)
      ? -1
      : 0;

/**
 * Select all clients as a tree
 *
 * This selector supports client tree structures with at most 2 layers.
 *
 * @param state Redux store
 */
export function clientsTree(state: PublishingState) {
  const clients = _.toArray(state.data.clients);
  const parentGroups: { [id: string]: ClientWithStats[] } = clients.reduce((groups, cur) =>
    groups[cur.parentId]
      ? { ...groups, [cur.parentId]: [ ...groups[cur.parentId], cur ] }
      : { ...groups, [cur.parentId]: [ cur ] },
    {} as { [id: string]: ClientWithStats[] });
  const iteratees = ['name', 'code'];
  const clientTree = _.sortBy(parentGroups.null, iteratees).map((c) => ({
    parent: c,
    children: _.sortBy(parentGroups[c.id] || [], iteratees),
  }));
  return clientTree;
}

/**
 * Select all clients that match the client filter.
 * @param state Redux store
 */
export function filteredClients(state: PublishingState) {
  const filterTextLower = state.filters.client.text.toLowerCase();
  const filterFunc = (client: ClientWithStats) => (
    filterTextLower === ''
    || (client.name && client.name.toLowerCase().indexOf(filterTextLower) !== -1)
    || (client.code && client.code.toLowerCase().indexOf(filterTextLower) !== -1)
  );
  return clientsTree(state).map(({ parent, children }) => ({
    parent,
    children: filterFunc(parent)
      ? children
      : children.filter((child) => filterFunc(child)),
  })).filter(({ parent, children }) => filterFunc(parent) || children.length);
}

/**
 * Select all content items that match the content item filter.
 * @param state Redux store
 */
export function filteredItems(state: PublishingState) {
  const filterTextLower = state.filters.item.text.toLowerCase();
  return _.filter(state.data.items, (item: RootContentItemWithStats) => (
    filterTextLower === ''
    || item.name.toLowerCase().indexOf(filterTextLower) !== -1
    || (
      state.data.contentTypes[item.contentTypeId]
      && state.data.contentTypes[item.contentTypeId].displayName.toLowerCase().indexOf(filterTextLower) !== -1)
  ));
}

/**
 * Select all clients that are visible to the user.
 * @param state Redux store
 */
export function activeClients(state: PublishingState) {
  return filteredClients(state);
}

/**
 * Select the most recent publication for a content item.
 * @param state Redux store
 * @param itemId The ID of the content item to check
 */
function relatedPublication(state: PublishingState, itemId: Guid) {
  const publications = _.filter(state.data.publications, (p) => p.rootContentItemId === itemId);
  const publication = publications.sort(sortPublications)[0];
  const queueDetails = publication && state.data.publicationQueue[publication.id];
  return publication
    ? { ...publication, queueDetails }
    : null;
}

/**
 * Select all content items that are visible to the user.
 * @param state Redux store
 */
function activeItems(state: PublishingState) {
  const filtered = filteredItems(state).filter((i) => i.clientId === state.selected.client);
  return _.sortBy(filtered, ['name']);
}

/**
 * Select all content items with publication status that are visible to the user.
 * @param state Redux store
 */
export function activeItemsWithStatus(state: PublishingState) {
  return activeItems(state).map((i) => {
    const publication = relatedPublication(state, i.id);
    return {
      ...i,
      status: {
        ...publication,
        requestStatusName: publication && publicationStatusNames[publication.requestStatus],
      },
    };
  });
}

interface ClientWithIndent extends ClientWithStats {
  indent: 1 | 2;
}

/**
 * Select clients with additional rendering data.
 * @param state Redux store
 */
export function clientEntities(state: PublishingState) {
  const entities: Array<ClientWithIndent | 'divider'> = [];
  activeClients(state).forEach(({ parent, children }) => {
    entities.push({
      ...parent,
      indent: 1,
    });
    children.forEach((child) => {
      entities.push({
        ...child,
        indent: 2,
      });
    });
    entities.push('divider');
  });
  entities.pop();  // remove last divider
  return entities;
}

/**
 * Select items with additional rendering data.
 * @param state Redux store
 */
export function itemEntities(state: PublishingState) {
  return activeItemsWithStatus(state).map((i) => {
    return {
      ...i,
      contentTypeName: state.data.contentTypes[i.contentTypeId].displayName,
    };
  });
}

/**
 * Select the highlighted client.
 * @param state Redux store
 */
export function selectedClient(state: PublishingState) {
  return state.selected.client
    ? state.data.clients[state.selected.client] as ClientWithStats
    : null;
}

/**
 * Select the highlighted client if it is visible to the user.
 * @param state Redux store
 */
export function activeSelectedClient(state: PublishingState) {
  return clientEntities(state)
    .filter((c) => c !== 'divider' && (c.id === state.selected.client))[0] as ClientWithIndent;
}

/**
 * Select the highlighted item.
 * @param state Redux store
 */
export function selectedItem(state: PublishingState) {
  return state.selected.item
    ? state.data.items[state.selected.item]
    : null;
}

/**
 * Select the highlighted item if it is visible to the user.
 * @param state Redux store
 */
export function activeSelectedItem(state: PublishingState) {
  return activeItems(state).filter((i) => i.id === state.selected.item)[0];
}

/**
 * Select the number of status refresh attempts remaining
 * @param state Redux store
 */
export function remainingStatusRefreshAttempts(state: PublishingState) {
  return state.pending.statusTries;
}

/**
 * Select the Content Types
 * @param state Redux store
 */
export function availableContentTypes(state: PublishingState) {
  const { contentTypes } = state.data;
  const contentTypesArray: Array<{ selectionValue: string | number, selectionLabel: string }> = [];
  for (const contentType in contentTypes) {
    contentTypesArray.push({
      selectionValue: contentTypes[contentType].id,
      selectionLabel: contentTypes[contentType].displayName,
    });
  }
  return contentTypesArray.sort((a, b) => (a.selectionValue > b.selectionValue) ? 1 : -1);
}

/**
 * Select the Associated Content Types
 * @param state Redux store
 */
export function availableAssociatedContentTypes(state: PublishingState) {
  const { contentAssociatedFileTypes } = state.data;
  const AssociatedContentTypesArray: Array<{ selectionValue: string | number, selectionLabel: string }> = [];
  for (const contentType in contentAssociatedFileTypes) {
    AssociatedContentTypesArray.push({
      selectionValue: contentAssociatedFileTypes[contentType].typeEnum,
      selectionLabel: contentAssociatedFileTypes[contentType].displayName,
    });
  }
  return AssociatedContentTypesArray.sort((a, b) => (a.selectionValue > b.selectionValue) ? 1 : -1);
}

/**
 * Determine if the Content Item form submit button should be enabled
 * @param state Redux store
 */
export function submitButtonIsActive(state: PublishingState) {
  const { pendingFormData } = state.formData;
  const formChanged = !_.isEqual(state.formData.pendingFormData, state.formData.originalFormData);
  const noActiveUpload = _.size(state.pending.uploads) === 0;
  const formValid = pendingFormData.clientId
    && pendingFormData.contentName.trim()
    && pendingFormData.contentTypeId
    && pendingFormData.relatedFiles.MasterContent.fileOriginalName.length > 0;
  return formChanged && noActiveUpload && formValid;
}

/**
 * Determine if there are uploads pending publishing
 * @param state Redux store
 */
export function uploadChangesPending(state: PublishingState) {
  const { pendingFormData, originalFormData } = state.formData;
  const changesPending = !_.isEqual(pendingFormData.relatedFiles, originalFormData.relatedFiles);
  return changesPending;
}

/**
 * Determine if there are pending form changes
 * @param state Redux store
 */
export function formChangesPending(state: PublishingState) {
  const { pendingFormData, originalFormData } = state.formData;
  const changesPending = (pendingFormData.id !== originalFormData.id)
    || (pendingFormData.clientId !== originalFormData.clientId)
    || (pendingFormData.contentName !== originalFormData.contentName)
    || (pendingFormData.contentDescription !== originalFormData.contentDescription)
    || (pendingFormData.contentDisclaimer !== originalFormData.contentDisclaimer)
    || (pendingFormData.contentNotes !== originalFormData.contentNotes)
    || (pendingFormData.doesReduce !== originalFormData.doesReduce)
    || !_.isEqual(pendingFormData.typeSpecificDetailObject, originalFormData.typeSpecificDetailObject);
  return changesPending;
}

/**
 * Return the related files for publishing
 * @param state Redux store
 */
export function filesForPublishing(state: PublishingState, rootContentItemId: Guid): PublishRequest {
  const { relatedFiles } = state.formData.pendingFormData;
  const filesToPublish: UploadedRelatedFile[] = [];
  const deleteFilePurposes: string[] = [];
  const { contentTypes } = state.data;
  const { pendingFormData } = state.formData;
  const isPowerBI = pendingFormData.contentTypeId
    && contentTypes[pendingFormData.contentTypeId].displayName === 'Power BI';
  for (const key in relatedFiles) {
    if (relatedFiles[key].fileUploadId) {
      filesToPublish.push({
        filePurpose: key,
        fileOriginalName: relatedFiles[key].fileOriginalName,
        fileUploadId: relatedFiles[key].fileUploadId,
      });
    }
    if (relatedFiles[key].fileOriginalName === '[Pending Removal]') {
      deleteFilePurposes.push(key);
    }
  }
  const typeSpecificPublishingDetail = (isPowerBI && pendingFormData.doesReduce) ? {
    roleList: pendingFormData.typeSpecificDetailObject.roleList,
  } : null;

  return {
    rootContentItemId,
    newRelatedFiles: filesToPublish,
    associatedFiles: [],
    deleteFilePurposes,
    typeSpecificPublishingDetail,
  };
}

export function goLiveApproveButtonIsActive(state: PublishingState): boolean {
  if (state.goLiveSummary && state.goLiveSummary.elementsToConfirm) {
    const { elementsToConfirm } = state.goLiveSummary;
    const allChecksApproved = Object.keys(elementsToConfirm).every((key) => elementsToConfirm[key] === true);
    return allChecksApproved;
  } else {
    return false;
  }
}

/**
 * Select the content item that is pending deletion
 * @param state Redux store
 */
export function contentItemToBeDeleted(state: PublishingState) {
  const contentItemIdPendingDeletion = state.pending.contentItemToDelete;
  return (state.data.items.hasOwnProperty(contentItemIdPendingDeletion))
    ? state.data.items[contentItemIdPendingDeletion]
    : null;
}

export function contentItemToBeCanceled(state: PublishingState) {
  const contentItemIdPendingCancelation = state.pending.publicationToCancel;
  return (state.data.items.hasOwnProperty(contentItemIdPendingCancelation))
    ? state.data.items[contentItemIdPendingCancelation]
    : null;
}

/**
 * Return the content item information for creating and updating content items
 * @param state Redux store
 */
export function contentItemForPublication(state: PublishingState): ContentItemPublicationDetail {
  const { contentTypes } = state.data;
  const { pendingFormData } = state.formData;
  const isPowerBI = pendingFormData.contentTypeId
    && contentTypes[pendingFormData.contentTypeId].displayName === 'Power BI';
  const contentItemInformation: ContentItemPublicationDetail = {
    ClientId: pendingFormData.clientId,
    ContentName: pendingFormData.contentName,
    ContentTypeId: pendingFormData.contentTypeId,
    Description: pendingFormData.contentDescription,
    ContentDisclaimer: pendingFormData.contentDisclaimer,
    Notes: pendingFormData.contentNotes,
  };

  if (pendingFormData.id) {
    contentItemInformation.Id = pendingFormData.id;
  }

  if (!pendingFormData.id) {
    contentItemInformation.DoesReduce = pendingFormData.doesReduce;
  }

  if (isPowerBI) {
    contentItemInformation.TypeSpecificDetailObject = {
      EditableEnabled: pendingFormData.typeSpecificDetailObject.editableEnabled,
      BookmarksPaneEnabled: pendingFormData.typeSpecificDetailObject.bookmarksPaneEnabled,
      FilterPaneEnabled: pendingFormData.typeSpecificDetailObject.filterPaneEnabled,
      NavigationPaneEnabled: pendingFormData.typeSpecificDetailObject.navigationPaneEnabled,
    };
  }

  return contentItemInformation;
}

/**
 * Return whether or not item can be downloaded by the publisher.
 * Currently only available for Editable PowerBI documents.
 * @param state Redux store
 */
export function canDownloadCurrentContentItem(state: PublishingState): boolean {
  const currentlySelectedItem = selectedItem(state);
  return currentlySelectedItem
    && state.data.contentTypes[currentlySelectedItem.contentTypeId].displayName === 'Power BI'
    && state.formData.originalFormData.isEditable
    && state.formData.formState === 'read'
    && !formChangesPending(state);
}
