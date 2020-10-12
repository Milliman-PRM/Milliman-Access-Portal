import * as _ from 'lodash';

import { ClientWithReviewDate } from '../../models';
import { AccessReviewState, ClientAccessReviewProgressEnum } from './store';

// Utility functions

/**
 * Select all clients as a tree
 * This selector supports client tree structures with at most 2 layers.
 */
export function clientsTree(state: AccessReviewState) {
  const { sortBy, sortOrder } = state.pending.clientSort;
  const clients = _.toArray(state.data.clients);
  const parentGroups: { [id: string]: ClientWithReviewDate[] } = clients.reduce((groups, cur) =>
    groups[cur.parentId]
      ? { ...groups, [cur.parentId]: [ ...groups[cur.parentId], cur ] }
      : { ...groups, [cur.parentId]: [ cur ] },
    {} as { [id: string]: ClientWithReviewDate[] });
  const sortItemsParent = sortBy === 'date'
    ? sortOrder === 'asc'
      ? ['minReviewDueDate']
      : ['maxReviewDueDate']
    : ['name', 'code'];
  const sortItemsChild = sortBy === 'date' ? ['reviewDueDateTimeUtc'] : ['name'];
  let sortOrderParent: Array<'asc' | 'desc'>;
  let sortOrderChild: Array<'asc' | 'desc'>;
  if (sortBy === 'date') {
    sortOrderParent = [sortOrder];
    sortOrderChild = [sortOrder];
  } else {
    sortOrderParent = [sortOrder, sortOrder];
    sortOrderChild = [sortOrder];
  }
  const clientTree = _.orderBy(parentGroups.null, sortItemsParent, sortOrderParent).map((c) => ({
    parent: c,
    children: _.orderBy(parentGroups[c.id] || [], sortItemsChild, sortOrderChild),
  }));
  return clientTree;
}

/**
 * Select all clients that match the client filter.
 */
export function filteredClients(state: AccessReviewState) {
  const filterTextLower = state.filters.client.text.toLowerCase();
  const filterFunc = (client: ClientWithReviewDate) => (
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
 * Select all clients that are visible to the user.
 */
export function activeClients(state: AccessReviewState) {
  return filteredClients(state);
}

interface ClientWithIndent extends ClientWithReviewDate {
  indent: 1 | 2;
}

/**
 * Select clients with additional rendering data.
 */
export function clientEntities(state: AccessReviewState) {
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
 * Select the highlighted client.
 */
export function selectedClient(state: AccessReviewState) {
  return state.selected.client
    ? state.data.clients[state.selected.client] as ClientWithReviewDate
    : null;
}

/**
 * Select the highlighted client if it is visible to the user.
 */
export function activeSelectedClient(state: AccessReviewState) {
  return clientEntities(state)
    .filter((c) => c !== 'divider' && (c.id === state.selected.client))[0] as ClientWithIndent;
}

/**
 * Select the number of status refresh attempts remaining
 */
export function remainingStatusRefreshAttempts(state: AccessReviewState) {
  return state.pending.statusTries;
}

/**
 * Define whether the Continue button is active or not
 */
export function continueButtonIsActive(state: AccessReviewState) {
  const { step, contentItemConfirmations, fileDropConfirmations } = state.pending.clientAccessReviewProgress;
  let buttonIsActive = false;
  switch (step) {
    case ClientAccessReviewProgressEnum.clientReview:
      buttonIsActive = true;
      break;
    case ClientAccessReviewProgressEnum.userRoles:
      buttonIsActive = true;
      break;
    case ClientAccessReviewProgressEnum.contentAccess:
      if (Object.keys(contentItemConfirmations).length === 0 ||
        Object.keys(contentItemConfirmations).every((ci) => contentItemConfirmations[ci] === true)) {
        buttonIsActive = true;
      }
      break;
    case ClientAccessReviewProgressEnum.fileDropAccess:
      if (Object.keys(fileDropConfirmations).length === 0 ||
        Object.keys(fileDropConfirmations).every((fd) => fileDropConfirmations[fd] === true)) {
        buttonIsActive = true;
      }
      break;
  }
  return buttonIsActive;
}

/**
 * Define the icon used for the Client Sort button
 */
export function clientSortIcon(state: AccessReviewState):
  'sort-alphabetically-asc' | 'sort-alphabetically-desc' | 'sort-date-asc' | 'sort-date-desc' {
  const { sortBy, sortOrder } = state.pending.clientSort;
  if (sortBy === 'name' && sortOrder === 'asc') {
    return 'sort-alphabetically-asc';
  }
  if (sortBy === 'name' && sortOrder === 'desc') {
    return 'sort-alphabetically-desc';
  }
  if (sortBy === 'date' && sortOrder === 'asc') {
    return 'sort-date-asc';
  }
  if (sortBy === 'date' && sortOrder === 'desc') {
    return 'sort-date-desc';
  }
}
