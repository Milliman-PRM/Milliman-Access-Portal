import { ContentAccessAdminState } from './store';

export function selectedItems(state: ContentAccessAdminState) {
  return state.data.items.filter((i) => i.clientId === state.clientPanel.selectedCard);
}

export function selectedGroups(state: ContentAccessAdminState) {
  return state.data.groups.filter((i) => i.rootContentItemId === state.itemPanel.selectedCard);
}
