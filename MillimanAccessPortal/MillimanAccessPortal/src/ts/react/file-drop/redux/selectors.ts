import * as _ from 'lodash';

import { ClientWithStats } from '../../models';
import { FileDropState } from './store';

// ~~~~~~~~~~
// Interfaces
// ~~~~~~~~~~

/** Model used for populating the Client tree with sub-clients */
// TODO: Move this to a shared location since this is shared between at least 3 different selectors.ts files
interface ClientWithIndent extends ClientWithStats {
  indent: 1 | 2;
}

// ~~~~~~~~~~~~~~~~~
// Utility Functions
// ~~~~~~~~~~~~~~~~~

// ~~~~~~~~~~~~~~~~
// Client Selectors
// ~~~~~~~~~~~~~~~~

/**
 *  Return all clients as a tree
 *  This selector supports client tree structures with at most 2 layers
 */
export function clientsTree(state: FileDropState) {
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

/** Return all clients that match the client filter */
export function filteredClients(state: FileDropState) {
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

/** Return all clients that are visible to the user */
export function activeClients(state: FileDropState) {
  return filteredClients(state);
}

/** Return clients with additional rendering data */
export function clientEntities(state: FileDropState) {
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

/** Return the highlighted client */
export function selectedClient(state: FileDropState) {
  return state.selected.client
    ? state.data.clients[state.selected.client] as ClientWithStats
    : null;
}

/** Return the highlighted client if it is visible to the user */
export function activeSelectedClient(state: FileDropState) {
  return clientEntities(state)
    .filter((c) => c !== 'divider' && (c.id === state.selected.client))[0] as ClientWithIndent;
}

// ~~~~~~~~~~~~~~~~~~~
// File Drop Selectors
// ~~~~~~~~~~~~~~~~~~~

/** Select all content items that match the content item filter */
export function fileDropEntities(state: FileDropState) {
  const filterTextLower = state.filters.fileDrop.text.toLowerCase();
  const filteredFileDrops = _.filter(state.data.fileDrops, (fileDrop: FileDropWithStats) => (
    fileDrop.clientId === state.selected.client && (
      filterTextLower === ''
      || fileDrop.name.toLowerCase().indexOf(filterTextLower) !== -1
      || fileDrop.description.toLowerCase().indexOf(filterTextLower) !== -1
    )
  ));
  return _.sortBy(filteredFileDrops, ['name']);
}

// ~~~~~~~~~~~~~~~~~~~~~~~~
// Status Refresh Selectors
// ~~~~~~~~~~~~~~~~~~~~~~~~

/** Return the number of status refresh attempts remaining */
export function remainingStatusRefreshAttempts(state: FileDropState) {
  return state.pending.statusTries;
}
