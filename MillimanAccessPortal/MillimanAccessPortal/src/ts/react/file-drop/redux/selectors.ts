import * as _ from 'lodash';
import * as moment from 'moment';

import {
  AvailableEligibleUsers, FileDropClientWithStats, FileDropWithStats, Guid,
  PermissionGroupModel, PermissionGroupsChangesModel, PGChangeModel,
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

/** Return the highlighted File Drop if it is visible to the user */
export function activeSelectedFileDrop(state: FileDropState) {
  return (state.selected.fileDrop) ? state.data.fileDrops[state.selected.fileDrop] : null;
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
        assignedMapUserIds: pgPending[pg].assignedMapUserIds,
        assignedSftpAccountIds: pgPending[pg].assignedSftpAccountIds,
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
          usersAdded: _.difference(pendingPG.assignedMapUserIds, rawPG.assignedMapUserIds),
          usersRemoved: _.difference(rawPG.assignedMapUserIds, pendingPG.assignedMapUserIds),
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

/** Return a boolean value indicating that the Permission Group changes are ready to be submitted */
export function permissionGroupChangesReady(state: FileDropState) {
  const { pending } = state;
  return _.every(_.toPairs(pending.permissionGroupsTab.permissionGroups),
      ([_key, value]: [string, PermissionGroupModel]) => {
        return (value.isPersonalGroup && value.assignedMapUserIds.length === 1) ||
          (!value.isPersonalGroup && value.name.length > 0);
      });
}

/** Return an array of eligible users that have not yet been assigned to a Permission Group */
export function unassignedEligibleUsers(state: FileDropState) {
  if (state.data.permissionGroups && state.pending.permissionGroupsTab) {
    const { eligibleUsers } = state.data.permissionGroups;
    const { permissionGroups } = state.pending.permissionGroupsTab;
    const assignedUserIds: Guid[] = [];
    // Loop through all of the existing Permission Groups and add assigned users to the assignedUserIds array
    Object.keys(permissionGroups).forEach((pg) => {
      permissionGroups[pg].assignedMapUserIds.forEach((userId) => assignedUserIds.push(userId));
    });
    const availableUsers: AvailableEligibleUsers[] =
      Object.keys(eligibleUsers)
        .map((userId) => {
          return {
            id: eligibleUsers[userId].id,
            name: [eligibleUsers[userId].firstName, eligibleUsers[userId].lastName].join(' '),
            sortName: [eligibleUsers[userId].lastName, eligibleUsers[userId].firstName].join(' '),
            userName: eligibleUsers[userId].userName,
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

/** Return filterd Permission Group data */
export function permissionGroupEntities(state: FileDropState) {
  const { text: filterText } = state.filters.permissions;
  if (filterText) {
    const { permissionGroups, eligibleUsers, fileDropId } = _.cloneDeep(state.pending.permissionGroupsTab);
    const permissionGroupIds = Object.keys(permissionGroups);
    const filteredPermissionGroups: Dict<PermissionGroupModel> = {};
    permissionGroupIds.forEach((pg) => {
      if (_.includes(permissionGroups[pg].name.toLowerCase(), filterText.toLowerCase())) {
        filteredPermissionGroups[pg] = permissionGroups[pg];
      } else {
        permissionGroups[pg].assignedMapUserIds.forEach((user) => {
          if (
            _.includes(
              [eligibleUsers[user].firstName, eligibleUsers[user].lastName].join(' ').toLowerCase(),
              filterText.toLowerCase(),
            ) ||
            _.includes(eligibleUsers[user].userName.toLowerCase(), filterText.toLowerCase())
          ) {
            if (!(pg in filteredPermissionGroups)) {
              filteredPermissionGroups[pg] = permissionGroups[pg];
              filteredPermissionGroups[pg].assignedMapUserIds = [];
            }
            filteredPermissionGroups[pg].assignedMapUserIds.push(user);
          }
        });
      }
    });
    return {
      fileDropId,
      eligibleUsers,
      permissionGroups: filteredPermissionGroups,
    };
  } else {
    return state.pending.permissionGroupsTab;
  }
}

// ~~~~~~~~~~~~~~~~~~~~~~
// Activity Log Selectors
// ~~~~~~~~~~~~~~~~~~~~~~

/** Return filterd Activity Log data */
export function activityLogEntities(state: FileDropState) {
  const { selectedFileDropTab } = state.pending;
  const { text: filterText } = state.filters.activityLog;
  const { activityLogEvents } = state.data;
  if (selectedFileDropTab === 'activityLog') {
    if (filterText) {
      const filterTextLower = filterText.toLowerCase();
      const filteredActivityLogEvents = activityLogEvents.filter((event) =>
        _.includes(event.fullName.toLowerCase(), filterTextLower)
        || _.includes(event.userName.toLowerCase(), filterTextLower)
        || _.includes(event.eventType.toLowerCase(), filterTextLower)
        || _.includes(JSON.stringify(event.eventData).toLowerCase(), filterTextLower)
        || _.includes(moment(event.timeStampUtc).local().format('M/D/YY h:mm A').toLowerCase(), filterTextLower));
      return filteredActivityLogEvents;
    } else {
      return activityLogEvents;
    }
  } else {
    return null;
  }
}

// ~~~~~~~~~~~~~~~~~~~~~~~~
// Status Refresh Selectors
// ~~~~~~~~~~~~~~~~~~~~~~~~

/** Return the number of status refresh attempts remaining */
export function remainingStatusRefreshAttempts(state: FileDropState) {
  return state.pending.statusTries;
}
