import * as _ from 'lodash';

import {
  AvailableEligibleUsers, FileDropClientWithStats, FileDropWithStats, Guid,
  PermissionGroupsChangesModel, PGChangeModel,
} from '../../models';
import { Dict } from '../../shared-components/redux/store';
import { FileDropState } from './store';

// ~~~~~~~~~~
// Interfaces
// ~~~~~~~~~~

/** Model used for populating the Client tree with sub-clients */
// TODO: Move this to a shared location since this is shared between at least 3 different selectors.ts files
interface ClientWithIndent extends FileDropClientWithStats {
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
  const parentGroups: { [id: string]: FileDropClientWithStats[] } = clients.reduce((groups, cur) =>
    groups[cur.parentId]
      ? { ...groups, [cur.parentId]: [ ...groups[cur.parentId], cur ] }
      : { ...groups, [cur.parentId]: [ cur ] },
    {} as { [id: string]: FileDropClientWithStats[] });
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
  const filterFunc = (client: FileDropClientWithStats) => (
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
    ? state.data.clients[state.selected.client] as FileDropClientWithStats
    : null;
}

/** Return the highlighted client if it is visible to the user */
export function activeSelectedClient(state: FileDropState) {
  return (state.selected.client) ? state.data.clients[state.selected.client] : null;
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

// ~~~~~~~~~~~~~~~~~~~~~~~~~~
// Permission Group Selectors
// ~~~~~~~~~~~~~~~~~~~~~~~~~~

/** Return the pending changes to the permissions tab data */
export function pendingPermissionGroupsChanges(state: FileDropState): PermissionGroupsChangesModel {
  if (state.data.permissionGroups && state.pending.permissionGroupsTab) {
    const { permissionGroups: pgRaw } = state.data.permissionGroups;
    const { permissionGroups: pgPending } = state.pending.permissionGroupsTab;
    const fileDropId = state.data.permissionGroups.fileDropId;

    const rawPGIds = Object.keys(pgRaw).sort();
    const pendingPGIds = Object.keys(pgPending).sort();

    // Removed Permission Groups
    // const removedPermissionGroups = rawPGIds.filter((pg) => pendingPGIds.indexOf(pg) === -1);
    const removedPermissionGroupIds = _.difference(rawPGIds, pendingPGIds);

    // Added Permission Groups
    const newPermissionGroups = _.difference(pendingPGIds, rawPGIds)
      .map((pg) => ({
        id: null,
        name: pgPending[pg].name,
        isPersonalGroup: pgPending[pg].isPersonalGroup,
        authorizedMapUsers: pgPending[pg].authorizedMapUsers,
        authorizedSftpAccounts: pgPending[pg].authorizedSftpAccounts,
        readAccess: pgPending[pg].readAccess,
        writeAccess: pgPending[pg].writeAccess,
        deleteAccess: pgPending[pg].deleteAccess,
      }));

    // Updated Permission Groups
    const updatedPermissionGroups: Dict<PGChangeModel> = {};
    _.intersection(rawPGIds, pendingPGIds)
      .filter((pg) => !(_.isEqual(pgRaw[pg], pgPending[pg])))
      .forEach((pg) => {
        const pendingPG = pgPending[pg];
        const rawPG = pgRaw[pg];
        updatedPermissionGroups[pg] = {
          id: pendingPG.id,
          name: pendingPG.name,
          usersAdded: _.difference(pendingPG.authorizedMapUsers, rawPG.authorizedMapUsers),
          usersRemoved: _.difference(rawPG.authorizedMapUsers, pendingPG.authorizedMapUsers),
          readAccess: pendingPG.readAccess,
          writeAccess: pendingPG.writeAccess,
          deleteAccess: pendingPG.deleteAccess,
        };
      });

    return {
      fileDropId,
      removedPermissionGroupIds,
      newPermissionGroups,
      updatedPermissionGroups,
    };
  } else {
    return null;
  }
}

/** Return a boolean value indicating that pending Permission Group changes exist */
export function permissionGroupChangesPending(state: FileDropState) {
  const { data, pending } = state;
  return data.permissionGroups
    && pending.permissionGroupsTab
    && (
      Object.keys(data.permissionGroups.permissionGroups).length
      !== Object.keys(pending.permissionGroupsTab.permissionGroups).length
      || !_.isEqual(data.permissionGroups.permissionGroups, pending.permissionGroupsTab.permissionGroups)
    );
}

/** Return an array of eligible users that have not yet been assigned to a Permission Group */
export function unassignedEligibleUsers(state: FileDropState) {
  if (state.data.permissionGroups && state.pending.permissionGroupsTab) {
    const { eligibleUsers } = state.data.permissionGroups;
    const { permissionGroups } = state.pending.permissionGroupsTab;
    const assignedUserIds: Guid[] = [];
    // Loop through all of the existing Permission Groups and add assigned users to the assignedUserIds array
    Object.keys(permissionGroups).forEach((pg) => {
      permissionGroups[pg].authorizedMapUsers.forEach((userId) => assignedUserIds.push(userId));
    });
    const availableUsers: AvailableEligibleUsers[] =
      Object.keys(eligibleUsers)
        .map((userId) => {
          return {
            id: eligibleUsers[userId].id,
            name: [eligibleUsers[userId].firstName, eligibleUsers[userId].lastName].join(' '),
            sortName: [eligibleUsers[userId].lastName, eligibleUsers[userId].firstName].join(' '),
            username: eligibleUsers[userId].username,
          };
        })
        .filter((user) => assignedUserIds.indexOf(user.id) === -1)
        .sort((a, b) => {
          const aSort = a.sortName.toLowerCase();
          const bSort = b.sortName.toLowerCase();
          return aSort.localeCompare(bSort);
        });

    return availableUsers;
  }
}

// ~~~~~~~~~~~~~~~~~~~~~~~~
// Status Refresh Selectors
// ~~~~~~~~~~~~~~~~~~~~~~~~

/** Return the number of status refresh attempts remaining */
export function remainingStatusRefreshAttempts(state: FileDropState) {
  return state.pending.statusTries;
}
