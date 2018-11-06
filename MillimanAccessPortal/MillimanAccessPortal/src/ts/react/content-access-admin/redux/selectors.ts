import { isEqual } from 'lodash';

import { ReductionFieldset } from '../../models';
import { ContentAccessAdminState } from './store';

export function selectedClient(state: ContentAccessAdminState) {
  return state.data.clients.filter((c) => c.id === state.clientPanel.selectedCard)[0];
}

export function selectedItem(state: ContentAccessAdminState) {
  return state.data.items.filter((i) => i.id === state.itemPanel.selectedCard)[0];
}

export function selectedGroup(state: ContentAccessAdminState) {
  return state.data.groups.filter((g) => g.id === state.groupPanel.selectedCard)[0];
}

export function selectedReductionValues(state: ContentAccessAdminState) {
  return selectedGroup(state)
    ? selectedGroup(state).selectedValues.map((i) =>
      state.data.values.filter((v) => v.id === i)[0])
    : [];
}

export function pendingReductionValues(state: ContentAccessAdminState) {
  const _selectedGroup = selectedGroup(state);
  return _selectedGroup
    ? state.data.values.filter((v) => {
      const panelValue = state.selectionsPanel.values[v.id];
      return (_selectedGroup.selectedValues.indexOf(v.id) !== -1 && panelValue !== false) || panelValue;
    })
    : [];
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

export function activeItems(state: ContentAccessAdminState) {
  return state.data.items.filter((i) => i.clientId === state.clientPanel.selectedCard);
}

export function activeGroups(state: ContentAccessAdminState) {
  return state.data.groups.filter((i) => i.rootContentItemId === state.itemPanel.selectedCard);
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
