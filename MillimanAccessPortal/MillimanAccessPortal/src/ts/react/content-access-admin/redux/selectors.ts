import { isEqual, xor } from 'lodash';

import {
  isPublicationActive, isReductionActive, publicationStatusNames, reductionStatusNames,
} from '../../../view-models/content-publishing';
import { Guid, ReductionFieldset } from '../../models';
import { AccessState } from './store';

// Modified status selectors
export function pendingReductionValues(state: AccessState) {
  const _selectedGroup = selectedGroup(state);
  const _relatedReduction = relatedReduction(state, _selectedGroup && _selectedGroup.id);
  return _selectedGroup
    ? (_relatedReduction && isReductionActive(_relatedReduction.taskStatus))
      ? _relatedReduction.selectedValues.map((i) =>
        state.data.values.filter((v) => v.id === i)[0])
      : state.data.values.filter((v) => state.pending.selections.indexOf(v.id) !== -1)
    : [];
}
export function modifiedReductionValues(state: AccessState) {
  return xor(
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
  return !isEqual(
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

// Filter selectors
export function filteredClients(state: AccessState) {
  const filterTextLower = state.filters.client.text.toLowerCase();
  return state.data.clients.filter((c) => {
    return (
      filterTextLower === ''
      || c.name.toLowerCase().indexOf(filterTextLower) !== -1
      || c.code.toLowerCase().indexOf(filterTextLower) !== -1
    );
  });
}
export function filteredItems(state: AccessState) {
  const filterTextLower = state.filters.item.text.toLowerCase();
  return state.data.items.filter((i) => {
    return (
      filterTextLower === ''
      || i.name.toLowerCase().indexOf(filterTextLower) !== -1
    );
  });
}
export function filteredGroups(state: AccessState) {
  const filterTextLower = state.filters.group.text.toLowerCase();
  return state.data.groups.filter((g) => {
    return (
      filterTextLower === ''
      || g.name.toLowerCase().indexOf(filterTextLower) !== -1
    );
  });
}
export function filteredFields(state: AccessState) {
  const fieldIds = filteredValues(state).map((v) => v.reductionFieldId);
  return state.data.fields.filter((f) => fieldIds.indexOf(f.id) !== -1);
}
export function filteredValues(state: AccessState) {
  const filterTextLower = state.filters.selections.text.toLowerCase();
  return state.data.values.filter((v) => {
    const field = state.data.fields.filter((f) => v.reductionFieldId === f.id)[0];
    return (
      filterTextLower === ''
      || v.value.toLowerCase().indexOf(filterTextLower) !== -1
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
  return state.data.publicationQueue.filter((q) => q.publicationId === publicationId)[0];
}
function relatedPublication(state: AccessState, itemId: Guid) {
  const publication = state.data.publications.filter((p) => p.rootContentItemId === itemId)[0];
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
        applicationUser: publication && state.data.users.filter((u) => u.id === publication.applicationUserId)[0],
        requestStatusName: publication && publicationStatusNames[publication.requestStatus],
      },
    };
  });
}

function queueDetailsForReduction(state: AccessState, reductionId: Guid) {
  return state.data.reductionQueue.filter((q) => q.reductionId === reductionId)[0];
}
function relatedReduction(state: AccessState, groupId: Guid) {
  const reduction = state.data.reductions.filter((r) => r.selectionGroupId === groupId)[0];
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
        applicationUser: reduction && state.data.users.filter((u) => u.id === reduction.applicationUserId)[0],
        taskStatusName: reduction && reductionStatusNames[reduction.taskStatus],
      },
    };
  });
}
export function allGroupsExpanded(state: AccessState) {
  return activeGroups(state).reduce((prev, g) => {
    const card = state.cardAttributes.group.get(g.id);
    return prev && card && card.expanded;
  }, true);
}
export function allGroupsCollapsed(state: AccessState) {
  return activeGroups(state).reduce((prev, g) => {
    const card = state.cardAttributes.group.get(g.id);
    return prev && (!card || !card.expanded);
  }, true);
}

export function activeReductions(state: AccessState) {
  return state.data.reductions.filter((r) => (
    activeGroups(state).map((g) => g.id).indexOf(r.selectionGroupId) !== -1));
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
    reports: state.data.items.filter((i) => i.clientId === c.id).length,
    eligibleUsers: c.eligibleUsers.length,
  }));
}
export function itemEntities(state: AccessState) {
  return activeItemsWithStatus(state).map((i) => {
    const groups = state.data.groups.filter((g) => g.rootContentItemId === i.id);
    return {
      ...i,
      contentTypeName: state.data.contentTypes.filter((c) => c.id === i.contentTypeId)[0].name,
      selectionGroups: groups.length,
      assignedUsers: groups.reduce((prev, cur) => prev + cur.assignedUsers.length, 0),
    };
  });
}
export function groupEntities(state: AccessState) {
  return activeGroupsWithStatus(state).map((g) => ({
    ...g,
    assignedUsers: g.assignedUsers.map((id) => state.data.users.find((u) => u.id === id)),
    name: state.cardAttributes.group.get(g.id).editing && state.pending.group
      ? state.pending.group.get(g.id).name
      : g.name,

  }));
}

// Selected entity selectors
export function selectedClient(state: AccessState) {
  return state.data.clients.filter((c) => c.id === state.selected.client)[0];
}
export function activeSelectedClient(state: AccessState) {
  return activeClients(state).filter((c) => c.id === state.selected.client)[0];
}

export function selectedItem(state: AccessState) {
  return state.data.items.filter((i) => i.id === state.selected.item)[0];
}
export function activeSelectedItem(state: AccessState) {
  return activeItems(state).filter((i) => i.id === state.selected.item)[0];
}

function selectedGroup(state: AccessState) {
  return state.data.groups.filter((g) => g.id === state.selected.group)[0];
}
export function activeSelectedGroup(state: AccessState) {
  return activeGroups(state).filter((g) => g.id === state.selected.group)[0];
}
export function selectedGroupWithStatus(state: AccessState) {
  return activeGroupsWithStatus(state).filter((g) => g.id === state.selected.group)[0];
}

export function selectedReductionValues(state: AccessState) {
  return selectedGroup(state)
    ? selectedGroup(state).selectedValues.map((i) =>
      state.data.values.filter((v) => v.id === i)[0])
    : [];
}
