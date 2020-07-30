import * as _ from 'lodash';
import { ClientWithStats, User } from '../../models';
import { AccessState } from './store';

/**
 * Select all clients as a tree
 *
 * This selector supports client tree structures with at most 2 layers.
 *
 * @param state Redux store
 */
export function clientsTree(state: AccessState) {
  const clients = _.toArray(state.data.clients);
  const parentGroups: { [id: string]: ClientWithStats[] } = clients.reduce((groups, cur) =>
    groups[cur.parentId]
      ? { ...groups, [cur.parentId]: [...groups[cur.parentId], cur] }
      : { ...groups, [cur.parentId]: [cur] },
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
export function filteredClients(state: AccessState) {
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
 * Select all users that match the client filter.
 * @param state Redux store
 */
export function filteredUsers(state: AccessState) {
  const filterTextLower = state.filters.user.text.toLowerCase();
  const filterFunc = (user: User) => {
    const userFullName = user.firstName + ' ' + user.lastName;
    return filterTextLower === ''
      || (userFullName && userFullName.toLowerCase().indexOf(filterTextLower) !== -1)
      || (user.userName && user.userName.toLowerCase().indexOf(filterTextLower) !== -1);
  };
  return state.data.assignedUsers.filter(filterFunc);
}

interface ClientWithIndent extends ClientWithStats {
  indent: 1 | 2;
}
/**
 * Select clients with additional rendering data.
 * @param state Redux store
 */
export function clientEntities(state: AccessState) {
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
 * Select all clients that are visible to the user.
 * @param state Redux store
 */
export function activeClients(state: AccessState) {
  return filteredClients(state);
}

export function activeUsers(state: AccessState) {
  return filteredUsers(state);
}
