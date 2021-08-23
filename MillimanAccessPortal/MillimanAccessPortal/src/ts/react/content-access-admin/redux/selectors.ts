import * as _ from 'lodash';
import * as moment from 'moment';

import {
  isReductionActive, publicationStatusNames, ReductionStatus, reductionStatusNames,
} from '../../../view-models/content-publishing';
import {
    ClientWithEligibleUsers, ClientWithStats, ContentPublicationRequest, ContentReductionTask, Guid,
    ReductionFieldset, RootContentItemWithStats, SelectionGroupWithAssignedUsers, User,
} from '../../models';
import { AccessState } from './store';

// Utility functions
const sortReductions = (left: ContentReductionTask, right: ContentReductionTask) =>
  sortMomentDescending(left.createDateTimeUtc, right.createDateTimeUtc);
const sortPublications = (left: ContentPublicationRequest, right: ContentPublicationRequest) =>
  sortMomentDescending(left.createDateTimeUtc, right.createDateTimeUtc);

const sortMomentDescending = (left: string, right: string) =>
  moment(left).isBefore(right)
    ? 1
    : moment(left).isAfter(right)
      ? -1
      : 0;

/**
 * Select the set of values pending submission/go-live.
 * @param state Redux store
 */
export function pendingReductionValues(state: AccessState) {
  const _selectedGroup = selectedGroup(state);
  const _relatedReduction = relatedReduction(state, _selectedGroup && _selectedGroup.id);

  if (!_selectedGroup) { return []; }
  if (_relatedReduction
    && _relatedReduction.selectedValues
    && (isReductionActive(_relatedReduction.taskStatus)
      || _relatedReduction.taskStatus === ReductionStatus.Warning)
  ) {
    return _relatedReduction.selectedValues.map((i) => state.data.values[i]).filter((i) => i);
  }
  return _.filter(state.data.values, (v) => {
    const selectionChanges = state.pending.selections || {};
    return _selectedGroup.selectedValues && _selectedGroup.selectedValues.filter((sv) => sv === v.id).length
      ? selectionChanges[v.id].selected === undefined || selectionChanges[v.id].selected
      : selectionChanges[v.id].selected !== undefined && selectionChanges[v.id].selected;
  });
}

/**
 * Select the set of values whose selection status has changed.
 * @param state Redux store
 */
export function modifiedReductionValues(state: AccessState) {
  return _.xor(
    selectedReductionValues(state),
    pendingReductionValues(state),
  );
}

/**
 * Select whether there are any pending selection changes.
 * @param state Redux store
 */
export function reductionValuesModified(state: AccessState) {
  return modifiedReductionValues(state).length > 0;
}

/**
 * Select master status pending submission.
 * @param state Redux store
 */
export function pendingMaster(state: AccessState) {
  const _selectedGroup = selectedGroup(state);
  const _relatedReduction = _selectedGroup && relatedReduction(state, _selectedGroup.id);
  const { isMaster } = state.pending;
  if (isMaster !== null) {
    return isMaster;
  }
  if (_relatedReduction
    && _relatedReduction.selectedValues !== null
    && _relatedReduction.selectedValues.length > 0) {
    return false;
  }
  return _selectedGroup && _selectedGroup.isMaster;
}

/**
 * Select whether there is a pending master status change.
 * @param state Redux store
 */
export function masterModified(state: AccessState) {
  const _selectedGroup = selectedGroup(state);
  const { isMaster } = state.pending;
  return _selectedGroup
    ? (isMaster !== null && isMaster !== _selectedGroup.isMaster)
    : false;
}

/**
 * Select whether any part of the selections form is modified.
 * @param state Redux store
 */
export function selectionsFormModified(state: AccessState) {
  return reductionValuesModified(state) || masterModified(state);
}

/**
 * Select the name to display for a selection group.
 * @param state Redux store
 * @param groupId The ID of the group to check
 */
export function pendingGroupName(state: AccessState, groupId: Guid) {
  const group = state.data.groups[groupId];
  return state.pending.group.id === groupId && state.pending.group.name !== null
    ? state.pending.group.name
    : group.name;
}

/**
 * Select the user assignments pending submission for a selection group.
 * @param state Redux store
 * @param groupId The ID of the group to check
 */
export function pendingGroupUserAssignments(state: AccessState, groupId: Guid) {
  const group = state.data.groups[groupId];
  const users = [...group.assignedUsers];
  const pendingUsers = state.pending.group.id === group.id
    ? state.pending.group.users
    : {};
  for (const userId in pendingUsers) {
    if (pendingUsers[userId].assigned) {
      if (users.filter((id) => id === userId).length === 0) {
        users.push(userId);
      }
    } else {
      if (users.filter((id) => id === userId).length) {
        users.splice(users.indexOf(userId), 1);
      }
    }
  }
  return users;
}

/**
 * Select filtered pending user assignments for a selection group
 * @param state Redux store
 * @param groupId The ID of the group to check
 */
export function filteredPendingGroupUserAssignments(state: AccessState, groupId: Guid) {
  const users: User[] = _.sortBy(
    pendingGroupUserAssignments(state, groupId).map((id) => state.data.users[id]),
    [(u: User) => u.firstName && u.lastName && (u.firstName + u.lastName), 'username'],
  );
  const filterTextLower = state.filters.group.text.toLowerCase();
  const group = state.data.groups[groupId];
  return _.filter(users, (user: User) => (
    filterTextLower === ''
    || (group.name && group.name.toLowerCase().indexOf(filterTextLower) !== -1)
    || (user.userName && user.userName.toLowerCase().indexOf(filterTextLower) !== -1)
    || (`${user.firstName || ''} ${user.lastName || ''}`.toLowerCase().indexOf(filterTextLower) !== -1)
  ));
}

/**
 * Select the group that is pending deletion
 * @param state Redux store
 */
export function groupToDelete(state: AccessState) {
  const groupId = state.pending.deleteGroup;
  return groupId && state.data.groups[groupId];
}

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
 * Select all content items that match the content item filter.
 * @param state Redux store
 */
export function filteredItems(state: AccessState) {
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
 * Select all selection groups that match the selection group filter.
 * @param state Redux store
 */
export function filteredGroups(state: AccessState) {
  const filterTextLower = state.filters.group.text.toLowerCase();
  return _.filter(state.data.groups, (group: SelectionGroupWithAssignedUsers) => (
    filterTextLower === ''
    || (
      group.name && group.name.toLowerCase().indexOf(filterTextLower) !== -1
      && pendingGroupUserAssignments(state, group.id).length === 0)
    || filteredPendingGroupUserAssignments(state, group.id).length > 0
  ));
}

/**
 * Select all fields that match, or have values that match, the selections filter.
 * @param state Redux store
 */
export function filteredFields(state: AccessState) {
  const fieldIds = filteredValues(state).map((v) => v.reductionFieldId);
  return _.filter(state.data.fields, (field) => fieldIds.indexOf(field.id) !== -1);
}

/**
 * Select all values that match the selections filter.
 * @param state Redux store
 */
export function filteredValues(state: AccessState) {
  const filterTextLower = state.filters.selections.text.toLowerCase();
  return _.filter(state.data.values, (value) => {
    const field = state.data.fields[value.reductionFieldId];
    return (
      filterTextLower === ''
      || value.value.toLowerCase().indexOf(filterTextLower) !== -1
      || field.fieldName.toLowerCase().indexOf(filterTextLower) !== -1
      || field.displayName.toLowerCase().indexOf(filterTextLower) !== -1
    );
  });
}

/**
 * Select all clients that are visible to the user.
 * @param state Redux store
 */
export function activeClients(state: AccessState) {
  return filteredClients(state);
}

/**
 * Select all clients that are visible to the user.
 * @param state Redux store
 */
function queueDetailsForPublication(state: AccessState, publicationId: Guid) {
  return state.data.publicationQueue[publicationId];
}

/**
 * Select the most recent publication for a content item.
 * @param state Redux store
 * @param itemId The ID of the content item to check
 */
function relatedPublication(state: AccessState, itemId: Guid) {
  const publications = _.filter(state.data.publications, (p) => p.rootContentItemId === itemId);
  const publication = publications.sort(sortPublications)[0];
  const queueDetails = publication && queueDetailsForPublication(state, publication.id);
  return publication
    ? { ...publication, queueDetails }
    : null;
}

/**
 * Select all content items that are visible to the user.
 * @param state Redux store
 */
function activeItems(state: AccessState) {
  const filtered = filteredItems(state).filter((i) => i.clientId === state.selected.client);
  return _.sortBy(filtered, ['name']);
}

/**
 * Select all content items with publication status that are visible to the user.
 * @param state Redux store
 */
export function activeItemsWithStatus(state: AccessState) {
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

/**
 * Select queue details for a reduction.
 * @param state Redux store
 * @param reductionId The ID of the reduction to check
 */
function queueDetailsForReduction(state: AccessState, reductionId: Guid) {
  return state.data.reductionQueue[reductionId];
}

/**
 * Select the most recent reduction for a selection group.
 * @param state Redux store
 * @param groupId The ID of the selection group to check
 */
function relatedReduction(state: AccessState, groupId: Guid) {
  const reductions = _.filter(state.data.reductions, (r) => r.selectionGroupId === groupId);
  const reduction = reductions.sort(sortReductions)[0];
  const queueDetails = reduction && queueDetailsForReduction(state, reduction.id);
  return reduction
    ? { ...reduction, queueDetails }
    : null;
}

/**
 * Select all selection groups that are visible to the user.
 * @param state Redux store
 */
export function activeGroups(state: AccessState) {
  const filtered = filteredGroups(state).filter((i) => i.rootContentItemId === state.selected.item);
  return _.sortBy(filtered, ['name']);
}

/**
 * Select all selection groups with reduction status that are visible to the user.
 * @param state Redux store
 */
export function activeGroupsWithStatus(state: AccessState) {
  return activeGroups(state).map((g) => {
    const reduction = relatedReduction(state, g.id);
    return {
      ...g,
      assignedUserCount: g.assignedUsers.length,
      status: {
        ...reduction,
        taskStatusName: reduction && reductionStatusNames[reduction.taskStatus],
      },
    };
  });
}

/**
 * Select whether all selection group cards are expanded.
 * @param state Redux store
 */
export function allGroupsExpanded(state: AccessState) {
  return activeGroups(state)
    .filter((g) => g.assignedUsers.length || state.pending.group.id === g.id)
    .reduce((prev, g) => {
      const card = state.cardAttributes.group[g.id];
      return prev && card && card.expanded;
    }, true);
}

/**
 * Select whether all selection group cards are collapsed.
 * @param state Redux store
 */
export function allGroupsCollapsed(state: AccessState) {
  return activeGroups(state)
    .filter((g) => g.assignedUsers.length)
    .reduce((prev, g) => {
      const card = state.cardAttributes.group[g.id];
      return prev && (!card || !card.expanded);
    }, true);
}

/**
 * Select whether all reduction values are selected.
 * @param state Redux store
 */
export function  allValuesSelected(state: AccessState) {
  return pendingReductionValues(state).length === activeReductionValues(state).length;
}

/**
 * Select whether all reduction values are deselected.
 * @param state Redux store
 */
export function allValuesDeselected(state: AccessState) {
  return pendingReductionValues(state).length === 0;
}

/**
 * Select all reductions that belong to an active selection group.
 * @param state Redux store
 */
export function activeReductions(state: AccessState) {
  return _.filter(state.data.reductions, (reduction) =>
    activeGroups(state).map((g) => g.id).indexOf(reduction.selectionGroupId) !== -1);
}

/**
 * Select all fields that are visible to the user.
 * @param state Redux store
 */
export function activeReductionFields(state: AccessState) {
  return selectedItem(state)
    ? filteredFields(state).filter((f) => selectedItem(state).id === f.rootContentItemId)
    : [];
}

/**
 * Select all values that are visible to the user.
 * @param state Redux store
 */
export function activeReductionValues(state: AccessState) {
  const activeReductionFieldIds = activeReductionFields(state).map((f) => f.id);
  return filteredValues(state).filter((f) => activeReductionFieldIds.indexOf(f.reductionFieldId) !== -1);
}

/**
 * Select all fields and values that are visible to the user as fieldsets.
 * @param state Redux store
 */
export function activeReductionFieldsets(state: AccessState): ReductionFieldset[] {
  const activeFields = activeReductionFields(state).map((f) => ({
    field: f,
    values: _.sortBy(
      activeReductionValues(state).filter((v) => v.reductionFieldId === f.id),
      ['value'],
    ),
  }));
  return _.sortBy(activeFields, ['displayName']);
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
 * Select items with additional rendering data.
 * @param state Redux store
 */
export function itemEntities(state: AccessState) {
  return activeItemsWithStatus(state).map((i) => {
    return {
      ...i,
      contentTypeName: state.data.contentTypes[i.contentTypeId].displayName,
    };
  });
}

/**
 * Select groups with additional rendering data.
 * @param state Redux store
 */
export function groupEntities(state: AccessState) {
  return activeGroupsWithStatus(state).map((g) => ({
    ...g,
    assignedUsers: filteredPendingGroupUserAssignments(state, g.id),
    name: pendingGroupName(state, g.id),
    editing: state.pending.group.id === g.id,
    userQuery: state.pending.group
      ? state.pending.group.userQuery
      : '',
  }));
}

/**
 * Select the highlighted client.
 * @param state Redux store
 */
export function selectedClient(state: AccessState) {
  return state.selected.client
    ? state.data.clients[state.selected.client] as ClientWithEligibleUsers
    : null;
}

/**
 * Select the highlighted client if it is visible to the user.
 * @param state Redux store
 */
export function activeSelectedClient(state: AccessState) {
  return clientEntities(state)
    .filter((c) => c !== 'divider' && (c.id === state.selected.client))[0] as ClientWithIndent;
}

/**
 * Select the highlighted item.
 * @param state Redux store
 */
export function selectedItem(state: AccessState) {
  return state.selected.item
    ? state.data.items[state.selected.item]
    : null;
}

/**
 * Select the highlighted item if it is visible to the user.
 * @param state Redux store
 */
export function activeSelectedItem(state: AccessState) {
  return activeItems(state).filter((i) => i.id === state.selected.item)[0];
}

/**
 * Select the highlighted group.
 * @param state Redux store
 */
function selectedGroup(state: AccessState) {
  return state.data.groups[state.selected.group];
}

/**
 * Select the highlighted group if it is visible to the user.
 * @param state Redux store
 */
export function activeSelectedGroup(state: AccessState) {
  return activeGroups(state).filter((g) => g.id === state.selected.group)[0];
}

/**
 * Select the highlighted group with reduction status if it is visible to the user.
 * @param state Redux store
 */
export function selectedGroupWithStatus(state: AccessState) {
  return activeGroupsWithStatus(state).filter((g) => g.id === state.selected.group)[0];
}

/**
 * Select all highlighted values.
 * @param state Redux store
 */
export function selectedReductionValues(state: AccessState) {
  return selectedGroup(state) && selectedGroup(state).selectedValues
    ? selectedGroup(state).selectedValues.map((i) => state.data.values[i]).filter((v) => v)
    : [];
}

/**
 * Select all users that can be added to a selection group.
 * @param state Redux store
 */
export function addableUsers(state: AccessState) {
  const client = selectedClient(state);
  return client
    ? _.difference(
      client.eligibleUsers,
      _.filter(state.data.groups, (g) => g.rootContentItemId === state.selected.item)
        .map((g) => pendingGroupUserAssignments(state, g.id))
        .reduce((prev, cur) => [...prev, ...cur], []),
    ).map((id) => state.data.users[id])
    : [];
}

/**
 * Select the number of status refresh attempts remaining
 * @param state Redux store
 */
export function remainingStatusRefreshAttempts(state: AccessState) {
  return state.pending.statusTries;
}

/**
 * Get content type display name of the currently selected content item
 * @param state Redux store
 */
export function contentTypeForSelectedItem(state: AccessState) {
  return (state.data.contentTypes && state.data.items && state.selected.item) ?
    state.data.contentTypes[state.data.items[state.selected.item].contentTypeId].displayName
    : null;
}
