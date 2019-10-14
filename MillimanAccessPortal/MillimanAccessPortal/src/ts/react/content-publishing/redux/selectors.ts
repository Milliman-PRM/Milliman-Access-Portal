import * as _ from 'lodash';
import * as moment from 'moment';

import { publicationStatusNames } from '../../../view-models/content-publishing';
import {
  ClientWithStats, ContentPublicationRequest, ContentReductionTask,
    Guid, RootContentItemWithStats,
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
  const formChanged = !_.isEqual(state.formData.formData, state.formData.originalData);
  return formChanged;
}
