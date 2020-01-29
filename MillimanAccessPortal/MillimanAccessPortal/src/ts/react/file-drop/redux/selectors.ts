import * as _ from 'lodash';

import { ClientWithStats } from '../../models';
import { FileDropState } from './store';

// Utility functions

/**
 * Select all clients as a tree
 *
 * This selector supports client tree structures with at most 2 layers.
 *
 * @param state Redux store
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

/**
 * Select all clients that match the client filter.
 * @param state Redux store
 */
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

/**
 * Select all clients that are visible to the user.
 * @param state Redux store
 */
export function activeClients(state: FileDropState) {
  return filteredClients(state);
}

interface ClientWithIndent extends ClientWithStats {
  indent: 1 | 2;
}

/**
 * Select clients with additional rendering data.
 * @param state Redux store
 */
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

/**
 * Select the highlighted client.
 * @param state Redux store
 */
export function selectedClient(state: FileDropState) {
  return state.selected.client
    ? state.data.clients[state.selected.client] as ClientWithStats
    : null;
}

/**
 * Select the highlighted client if it is visible to the user.
 * @param state Redux store
 */
export function activeSelectedClient(state: FileDropState) {
  return clientEntities(state)
    .filter((c) => c !== 'divider' && (c.id === state.selected.client))[0] as ClientWithIndent;
}

/**
 * Select the number of status refresh attempts remaining
 * @param state Redux store
 */
export function remainingStatusRefreshAttempts(state: FileDropState) {
  return state.pending.statusTries;
}
