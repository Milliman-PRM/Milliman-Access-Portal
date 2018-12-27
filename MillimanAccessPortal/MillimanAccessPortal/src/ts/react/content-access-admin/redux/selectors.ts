import * as _ from 'lodash';

import {
  isReductionActive, publicationStatusNames, reductionStatusNames,
} from '../../../view-models/content-publishing';
import { Guid, ReductionFieldset } from '../../models';
import { AccessState } from './store';

// Modified status selectors
export function pendingReductionValues(state: AccessState) {
  const _selectedGroup = selectedGroup(state);
  const _relatedReduction = relatedReduction(state, _selectedGroup && _selectedGroup.id);
  return _selectedGroup
    ? (_relatedReduction && isReductionActive(_relatedReduction.taskStatus))
      ? _relatedReduction.selectedValues.map((i) => state.data.values[i])
      : _.filter(state.data.values, (v) => {
        const selectionChanges = state.pending.selections || new Map();
        return _selectedGroup.selectedValues && _selectedGroup.selectedValues.find((sv) => sv === v.id)
          ? !selectionChanges.has(v.id) || selectionChanges.get(v.id).selected
          : selectionChanges.has(v.id) && selectionChanges.get(v.id).selected;
      })
    : [];
}
export function modifiedReductionValues(state: AccessState) {
  return _.xor(
    selectedReductionValues(state),
    pendingReductionValues(state),
  );
}
export function pendingMaster(state: AccessState) {
  const _selectedGroup = selectedGroup(state);
  const { isMaster } = state.pending;
  return _selectedGroup
    ? isMaster === null
      ? _selectedGroup.isMaster
      : isMaster
    : false;
}
export function reductionValuesModified(state: AccessState) {
  return !_.isEqual(
    selectedReductionValues(state),
    pendingReductionValues(state),
  );
}
export function masterModified(state: AccessState) {
  const _selectedGroup = selectedGroup(state);
  const { isMaster } = state.pending;
  return _selectedGroup
    ? (isMaster !== null && isMaster !== _selectedGroup.isMaster)
    : false;
}
export function selectionsFormModified(state: AccessState) {
  return reductionValuesModified(state) || masterModified(state);
}
export function pendingGroupName(state: AccessState, groupId: Guid) {
  const group = state.data.groups[groupId];
  return state.pending.group.id === groupId
      && state.pending.group.name !== null
      ? state.pending.group.name
      : group.name;
}
export function pendingGroupUserAssignments(state: AccessState, groupId: Guid) {
  const group = state.data.groups[groupId];
  const users = [...group.assignedUsers];
  const pendingUsers = state.pending.group.id === group.id
    ? state.pending.group.users
    : new Map();
  for (const userId of pendingUsers.keys()) {
    if (pendingUsers.get(userId).assigned) {
      if (users.find((id) => id === userId) === undefined) {
        users.push(userId);
      }
    } else {
      if (users.find((id) => id === userId) !== undefined) {
        users.splice(users.indexOf(userId), 1);
      }
    }
  }
  return users;
}

// Filter selectors
export function filteredClients(state: AccessState) {
  const filterTextLower = state.filters.client.text.toLowerCase();
  return _.filter(state.data.clients, (client) => (
    filterTextLower === ''
    || client.name.toLowerCase().indexOf(filterTextLower) !== -1
    || client.code.toLowerCase().indexOf(filterTextLower) !== -1
  ));
}
export function filteredItems(state: AccessState) {
  const filterTextLower = state.filters.item.text.toLowerCase();
  return _.filter(state.data.items, (item) => (
    filterTextLower === ''
    || item.name.toLowerCase().indexOf(filterTextLower) !== -1
  ));
}
export function filteredGroups(state: AccessState) {
  const filterTextLower = state.filters.group.text.toLowerCase();
  return _.filter(state.data.groups, (group) => (
    filterTextLower === ''
    || group.name.toLowerCase().indexOf(filterTextLower) !== -1
  ));
}
export function filteredFields(state: AccessState) {
  const fieldIds = filteredValues(state).map((v) => v.reductionFieldId);
  return _.filter(state.data.fields, (field) => fieldIds.indexOf(field.id) !== -1);
}
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

// Active entity selectors
export function activeClients(state: AccessState) {
  return filteredClients(state);
}
function queueDetailsForPublication(state: AccessState, publicationId: Guid) {
  return state.data.publicationQueue[publicationId];
}
function relatedPublication(state: AccessState, itemId: Guid) {
  const publication = state.data.publications[itemId];
  const queueDetails = publication && queueDetailsForPublication(state, publication.id);
  return publication
    ? { ...publication, queueDetails }
    : null;
}
function activeItems(state: AccessState) {
  return filteredItems(state).filter((i) => i.clientId === state.selected.client);
}
export function activeItemsWithStatus(state: AccessState) {
  return activeItems(state).map((i) => {
    const publication = relatedPublication(state, i.id);
    return {
      ...i,
      status: {
        ...publication,
        applicationUser: publication && state.data.users[publication.applicationUserId],
        requestStatusName: publication && publicationStatusNames[publication.requestStatus],
      },
    };
  });
}

function queueDetailsForReduction(state: AccessState, reductionId: Guid) {
  return state.data.reductionQueue[reductionId];
}
function relatedReduction(state: AccessState, groupId: Guid) {
  const reduction = state.data.reductions[groupId];
  const queueDetails = reduction && queueDetailsForReduction(state, reduction.id);
  return reduction
    ? { ...reduction, queueDetails }
    : null;
}
export function activeGroups(state: AccessState) {
  return filteredGroups(state).filter((i) => i.rootContentItemId === state.selected.item);
}
export function activeGroupsWithStatus(state: AccessState) {
  return activeGroups(state).map((g) => {
    const reduction = relatedReduction(state, g.id);
    return {
      ...g,
      status: {
        ...reduction,
        applicationUser: reduction && state.data.users[reduction.applicationUserId],
        taskStatusName: reduction && reductionStatusNames[reduction.taskStatus],
      },
    };
  });
}
export function allGroupsExpanded(state: AccessState) {
  return activeGroups(state)
    .filter((g) => g.assignedUsers.length || state.pending.group.id === g.id)
    .reduce((prev, g) => {
      const card = state.cardAttributes.group.get(g.id);
      return prev && card && card.expanded;
    }, true);
}
export function allGroupsCollapsed(state: AccessState) {
  return activeGroups(state)
    .filter((g) => g.assignedUsers.length)
    .reduce((prev, g) => {
      const card = state.cardAttributes.group.get(g.id);
      return prev && (!card || !card.expanded);
    }, true);
}

export function activeReductions(state: AccessState) {
  return _.filter(state.data.reductions, (reduction) =>
    activeGroups(state).map((g) => g.id).indexOf(reduction.selectionGroupId) !== -1);
}

export function activeReductionFields(state: AccessState) {
  return selectedItem(state)
    ? filteredFields(state).filter((f) => selectedItem(state).id === f.rootContentItemId)
    : [];
}

export function activeReductionValues(state: AccessState) {
  const activeReductionFieldIds = activeReductionFields(state).map((f) => f.id);
  return filteredValues(state).filter((f) => activeReductionFieldIds.indexOf(f.reductionFieldId) !== -1);
}

export function activeReductionFieldsets(state: AccessState): ReductionFieldset[] {
  return activeReductionFields(state).map((f) => ({
    field: f,
    values: activeReductionValues(state).filter((v) => v.reductionFieldId === f.id),
  }));
}

export function clientEntities(state: AccessState) {
  return activeClients(state).map((c) => ({
    ...c,
    reports: _.filter(state.data.items, (i) => i.clientId === c.id).length,
    eligibleUsers: c.eligibleUsers.length,
  }));
}
export function itemEntities(state: AccessState) {
  return activeItemsWithStatus(state).map((i) => {
    const groups = _.filter(state.data.groups, (g) => g.rootContentItemId === i.id);
    return {
      ...i,
      contentTypeName: state.data.contentTypes[i.contentTypeId].name,
      selectionGroups: groups.length,
      assignedUsers: groups.reduce((prev, cur) => prev + cur.assignedUsers.length, 0),
    };
  });
}
export function groupEntities(state: AccessState) {
  return activeGroupsWithStatus(state).map((g) => ({
    ...g,
    assignedUsers: pendingGroupUserAssignments(state, g.id).map((id) => state.data.users[id]),
    name: pendingGroupName(state, g.id),
    editing: state.pending.group.id === g.id,
    userQuery: state.pending.group
      ? state.pending.group.userQuery
      : '',
  }));
}

// Selected entity selectors
export function selectedClient(state: AccessState) {
  return state.data.clients[state.selected.client];
}
export function activeSelectedClient(state: AccessState) {
  return activeClients(state).filter((c) => c.id === state.selected.client)[0];
}

export function selectedItem(state: AccessState) {
  return state.data.items[state.selected.item];
}
export function activeSelectedItem(state: AccessState) {
  return activeItems(state).filter((i) => i.id === state.selected.item)[0];
}

function selectedGroup(state: AccessState) {
  return state.data.groups[state.selected.group];
}
export function activeSelectedGroup(state: AccessState) {
  return activeGroups(state).filter((g) => g.id === state.selected.group)[0];
}
export function selectedGroupWithStatus(state: AccessState) {
  return activeGroupsWithStatus(state).filter((g) => g.id === state.selected.group)[0];
}

export function selectedReductionValues(state: AccessState) {
  return selectedGroup(state) && selectedGroup(state).selectedValues
    ? selectedGroup(state).selectedValues.map((i) =>
      state.data.values[i])
    : [];
}

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
