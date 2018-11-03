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
