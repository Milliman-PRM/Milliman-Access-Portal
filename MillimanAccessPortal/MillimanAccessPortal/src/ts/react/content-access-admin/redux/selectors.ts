import { isEqual, xor } from 'lodash';

import { isPublicationActive, isReductionActive } from '../../../view-models/content-publishing';
import { Guid, ReductionFieldset } from '../../models';
import { ContentAccessAdminState } from './store';

export function selectedClient(state: ContentAccessAdminState) {
  return state.data.clients.filter((c) => c.id === state.clientPanel.selectedCard)[0];
}

export function selectedItem(state: ContentAccessAdminState) {
  return state.data.items.filter((i) => i.id === state.itemPanel.selectedCard)[0];
}

function selectedGroup(state: ContentAccessAdminState) {
  return state.data.groups.filter((g) => g.id === state.groupPanel.selectedCard)[0];
}
export function selectedGroupWithStatus(state: ContentAccessAdminState) {
  const _selectedGroup = selectedGroup(state);
  return _selectedGroup
  ? {
    ..._selectedGroup,
    status: relatedReduction(state, _selectedGroup.id),
  }
  : null;
}

export function selectedReductionValues(state: ContentAccessAdminState) {
  return selectedGroup(state)
    ? selectedGroup(state).selectedValues.map((i) =>
      state.data.values.filter((v) => v.id === i)[0])
    : [];
}

export function pendingReductionValues(state: ContentAccessAdminState) {
  const _selectedGroup = selectedGroup(state);
  const _relatedReduction = relatedReduction(state, _selectedGroup && _selectedGroup.id);
  return _selectedGroup
    ? (_relatedReduction && isReductionActive(_relatedReduction.reductionStatus))
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
  return state.data.items.filter((i) => i.clientId === state.clientPanel.selectedCard);
}
export function activeItemsWithStatus(state: ContentAccessAdminState) {
  return activeItems(state).map((i) => ({
    ...i,
    status: relatedPublication(state, i.id),
  }));
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
function activeGroups(state: ContentAccessAdminState) {
  return state.data.groups.filter((i) => i.rootContentItemId === state.itemPanel.selectedCard);
}
export function activeGroupsWithStatus(state: ContentAccessAdminState) {
  return activeGroups(state).map((g) => ({
    ...g,
    status: relatedReduction(state, g.id),
  }));
}

export function activeReductions(state: ContentAccessAdminState) {
  return state.data.reductions.filter((r) => (
    activeGroups(state).map((g) => g.id).indexOf(r.selectionGroupId) !== -1));
}

export function activeReductionFields(state: ContentAccessAdminState) {
  return selectedItem(state)
    ? state.data.fields.filter((f) => selectedItem(state).id === f.rootContentItemId)
    : [];
}

export function activeReductionValues(state: ContentAccessAdminState) {
  const activeReductionFieldIds = activeReductionFields(state).map((f) => f.id);
  return state.data.values.filter((f) => activeReductionFieldIds.indexOf(f.reductionFieldId) !== -1);
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
