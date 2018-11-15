import { isEqual, xor } from 'lodash';

import {
  isPublicationActive, isReductionActive, publicationStatusNames, reductionStatusNames,
} from '../../../view-models/content-publishing';
import { Guid, ReductionFieldset } from '../../models';
import { ContentAccessAdminState } from './store';

// Modified status selectors
export function pendingReductionValues(state: ContentAccessAdminState) {
  const _selectedGroup = selectedGroup(state);
  const _relatedReduction = relatedReduction(state, _selectedGroup && _selectedGroup.id);
  return _selectedGroup
    ? (_relatedReduction && isReductionActive(_relatedReduction.taskStatus))
      ? _relatedReduction.selectedValues.map((i) =>
        state.data.values.filter((v) => v.id === i)[0])
      : state.data.values.filter((v) => {
        const panelValue = state.selectionsPanel.values[v.id];
        return (_selectedGroup.selectedValues.indexOf(v.id) !== -1 && panelValue !== false) || panelValue;
      })
    : [];
}
export function modifiedReductionValues(state: ContentAccessAdminState) {
  return xor(
    selectedReductionValues(state),
    pendingReductionValues(state),
  );
}
export function pendingMaster(state: ContentAccessAdminState) {
  const _selectedGroup = selectedGroup(state);
  return _selectedGroup
    ? state.selectionsPanel.isMaster === null
      ? _selectedGroup.isMaster
      : state.selectionsPanel.isMaster
    : false;
}
export function reductionValuesModified(state: ContentAccessAdminState) {
  return !isEqual(
    selectedReductionValues(state),
    pendingReductionValues(state),
  );
}
export function masterModified(state: ContentAccessAdminState) {
  const _selectedGroup = selectedGroup(state);
  const { isMaster } = state.selectionsPanel;
  return _selectedGroup
    ? (isMaster !== null && isMaster !== _selectedGroup.isMaster)
    : false;
}
export function selectionsFormModified(state: ContentAccessAdminState) {
  return reductionValuesModified(state) || masterModified(state);
}

// Filter selectors
export function filteredClients(state: ContentAccessAdminState) {
  const filterTextLower = state.clientPanel.filterText.toLowerCase();
  return state.data.clients.filter((c) => {
    return (
      filterTextLower === ''
      || c.name.toLowerCase().indexOf(filterTextLower) !== -1
      || c.code.toLowerCase().indexOf(filterTextLower) !== -1
    );
  });
}
export function filteredItems(state: ContentAccessAdminState) {
  const filterTextLower = state.itemPanel.filterText.toLowerCase();
  return state.data.items.filter((i) => {
    return (
      filterTextLower === ''
      || i.name.toLowerCase().indexOf(filterTextLower) !== -1
    );
  });
}
export function filteredGroups(state: ContentAccessAdminState) {
  const filterTextLower = state.groupPanel.filterText.toLowerCase();
  return state.data.groups.filter((g) => {
    return (
      filterTextLower === ''
      || g.name.toLowerCase().indexOf(filterTextLower) !== -1
    );
  });
}
export function filteredFields(state: ContentAccessAdminState) {
  const fieldIds = filteredValues(state).map((v) => v.reductionFieldId);
  return state.data.fields.filter((f) => fieldIds.indexOf(f.id) !== -1);
}
export function filteredValues(state: ContentAccessAdminState) {
  const filterTextLower = state.selectionsPanel.filterText.toLowerCase();
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
export function activeClients(state: ContentAccessAdminState) {
  return filteredClients(state);
}
function queueDetailsForPublication(state: ContentAccessAdminState, publicationId: Guid) {
  return state.data.publicationQueue.filter((q) => q.publicationId === publicationId)[0];
}
function relatedPublication(state: ContentAccessAdminState, itemId: Guid) {
  const publication = state.data.publications.filter((p) => p.rootContentItemId === itemId)[0];
  const queueDetails = publication && queueDetailsForPublication(state, publication.id);
  return publication
    ? { ...publication, queueDetails }
    : null;
}
function activeItems(state: ContentAccessAdminState) {
  return filteredItems(state).filter((i) => i.clientId === state.clientPanel.selectedCard);
}
export function activeItemsWithStatus(state: ContentAccessAdminState) {
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

function queueDetailsForReduction(state: ContentAccessAdminState, reductionId: Guid) {
  return state.data.reductionQueue.filter((q) => q.reductionId === reductionId)[0];
}
function relatedReduction(state: ContentAccessAdminState, groupId: Guid) {
  const reduction = state.data.reductions.filter((r) => r.selectionGroupId === groupId)[0];
  const queueDetails = reduction && queueDetailsForReduction(state, reduction.id);
  return reduction
    ? { ...reduction, queueDetails }
    : null;
}
export function activeGroups(state: ContentAccessAdminState) {
  return filteredGroups(state).filter((i) => i.rootContentItemId === state.itemPanel.selectedCard);
}
export function activeGroupsWithStatus(state: ContentAccessAdminState) {
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
export function allGroupsExpanded(state: ContentAccessAdminState) {
  return activeGroups(state).reduce((prev, g) => {
    const card = state.groupPanel.cards[g.id];
    return prev && card && card.expanded;
  }, true);
}
export function allGroupsCollapsed(state: ContentAccessAdminState) {
  return activeGroups(state).reduce((prev, g) => {
    const card = state.groupPanel.cards[g.id];
    return prev && (!card || !card.expanded);
  }, true);
}

export function activeReductions(state: ContentAccessAdminState) {
  return state.data.reductions.filter((r) => (
    activeGroups(state).map((g) => g.id).indexOf(r.selectionGroupId) !== -1));
}

export function activeReductionFields(state: ContentAccessAdminState) {
  return selectedItem(state)
    ? filteredFields(state).filter((f) => selectedItem(state).id === f.rootContentItemId)
    : [];
}

export function activeReductionValues(state: ContentAccessAdminState) {
  const activeReductionFieldIds = activeReductionFields(state).map((f) => f.id);
  return filteredValues(state).filter((f) => activeReductionFieldIds.indexOf(f.reductionFieldId) !== -1);
}

export function activeReductionFieldsets(state: ContentAccessAdminState): ReductionFieldset[] {
  return activeReductionFields(state).map((f) => ({
    field: f,
    values: activeReductionValues(state).filter((v) => v.reductionFieldId === f.id),
  }));
}

export function itemCardAttributes(state: ContentAccessAdminState) {
  const cards = { ...state.itemPanel.cards };
  state.data.publications.forEach((p) => {
    if (isPublicationActive(p.requestStatus)) {
      if (cards[p.rootContentItemId]) {
        cards[p.rootContentItemId].disabled = true;
      } else {
        cards[p.rootContentItemId] = { disabled: true };
      }
    }
  });
  return cards;
}

export function clientEntities(state: ContentAccessAdminState) {
  return activeClients(state).map((c) => ({
    ...c,
    reports: state.data.items.filter((i) => i.clientId === c.id).length,
    eligibleUsers: c.eligibleUsers.length,
  }));
}
export function itemEntities(state: ContentAccessAdminState) {
  return activeItemsWithStatus(state).map((i) => {
    const groups = state.data.groups.filter((g) => g.rootContentItemId === i.id);
    return {
      ...i,
      selectionGroups: groups.length,
      assignedUsers: groups.reduce((prev, cur) => prev + cur.assignedUsers.length, 0),
    };
  });
}
export function groupEntities(state: ContentAccessAdminState) {
  return activeGroupsWithStatus(state).map((g) => ({
    ...g,
    assignedUsers: g.assignedUsers.length,
  }));
}

// Selected entity selectors
export function selectedClient(state: ContentAccessAdminState) {
  return state.data.clients.filter((c) => c.id === state.clientPanel.selectedCard)[0];
}
export function activeSelectedClient(state: ContentAccessAdminState) {
  return activeClients(state).filter((c) => c.id === state.clientPanel.selectedCard)[0];
}

export function selectedItem(state: ContentAccessAdminState) {
  return state.data.items.filter((i) => i.id === state.itemPanel.selectedCard)[0];
}
export function activeSelectedItem(state: ContentAccessAdminState) {
  return activeItems(state).filter((i) => i.id === state.itemPanel.selectedCard)[0];
}

function selectedGroup(state: ContentAccessAdminState) {
  return state.data.groups.filter((g) => g.id === state.groupPanel.selectedCard)[0];
}
export function activeSelectedGroup(state: ContentAccessAdminState) {
  return activeGroups(state).filter((g) => g.id === state.groupPanel.selectedCard)[0];
}
export function selectedGroupWithStatus(state: ContentAccessAdminState) {
  return activeGroupsWithStatus(state).filter((g) => g.id === state.groupPanel.selectedCard)[0];
}

export function selectedReductionValues(state: ContentAccessAdminState) {
  return selectedGroup(state)
    ? selectedGroup(state).selectedValues.map((i) =>
      state.data.values.filter((v) => v.id === i)[0])
    : [];
}
