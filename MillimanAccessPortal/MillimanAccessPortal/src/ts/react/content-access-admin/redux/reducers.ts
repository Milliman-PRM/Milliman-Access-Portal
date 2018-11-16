import { combineReducers } from 'redux';

import { Guid } from '../../models';
import { CardAttributes } from '../../shared-components/card/card';
import { AccessAction } from './actions';
import {
  AccessStateData, AccessStateSelected, FilterState, ModalState,
} from './store';

// utility functions
interface Handlers<TState, TAction> {
  [type: string]: (state: TState, action: TAction) => TState;
}
function createReducer<TState>(initialState: TState, handlers: Handlers<TState, any>) {
  return (state: TState = initialState, action) => {
    return action.type in handlers
      ? handlers[action.type](state, action)
      : state;
  };
}
const createFilterReducer = (actionType: AccessAction) =>
  createReducer<FilterState>({ text: '' }, {
    [actionType]: (state, action) => ({
      ...state,
      text: action.text,
    }),
  });
const createModalReducer = (openActionType: AccessAction, closeActionType: AccessAction) =>
  createReducer<ModalState>({ isOpen: false }, {
    [openActionType]: (state) => ({
      ...state,
      isOpen: true,
    }),
    [closeActionType]: (state) => ({
      ...state,
      isOpen: false,
    }),
  });
function updateList<T>(list: T[], selector: (item: T) => boolean, value?: T, transform?: (prev: T) => T): T[] {
  const filtered = list.filter(selector);
  return value === undefined
      ? filtered
    : transform === undefined
      ? [...filtered, value].sort()
    : list.length === filtered.length
      ? [...filtered, transform(value)].sort()
      : list.map((i) => selector(i) ? i : transform(i)).sort();
}

const groupCardAttributes = createReducer<CardAttributes[]>([],
  {
    [AccessAction.SetExpandedGroup]: (state, action) =>
      updateList(state, (c) => c.id === action.id, { id: action.id }, (prev) => ({ ...prev, expanded: true })),
    [AccessAction.SetCollapsedGroup]: (state, action) =>
      updateList(state, (c) => c.id === action.id, { id: action.id }, (prev) => ({ ...prev, expanded: false })),
    [AccessAction.SetAllExpandedGroup]: (state, action) =>
      updateList(state, () => true, { id: action.id }, (prev) => ({ ...prev, expanded: true })),
    [AccessAction.SetAllCollapsedGroup]: (state, action) =>
      updateList(state, () => true, { id: action.id }, (prev) => ({ ...prev, expanded: false })),
  },
);
const pendingIsMaster = createReducer<boolean>(false, {
  [AccessAction.SetPendingIsMaster]: (_, action) => action.isMaster,
});
const pendingSelections = createReducer<Guid[]>([], {
  [AccessAction.SetPendingSelectionOn]: (state, action) =>
    updateList(state, (i) => i !== action.id, action.id),
  [AccessAction.SetPendingSelectionOff]: (state, action) =>
    updateList(state, (i) => i !== action.id),
});
const pendingNewGroupName = createReducer<string>('', {
  [AccessAction.SetPendingGroupName]: (_, action) => action.name,
});

const data = (state: AccessStateData) => state;
const selected = createReducer<AccessStateSelected>(
  {
    client: null,
    item: null,
    group: null,
  },
  {
    [AccessAction.SelectClient]: (state, action) => action.id === state.client
      ? state
      : {
        client: action.id,
        item: null,
        group: null,
      },
    [AccessAction.SelectItem]: (state, action) => action.id === state.item
      ? state
      : {
        ...state,
        item: action.id,
        group: null,
      },
    [AccessAction.SelectGroup]: (state, action) => ({
      ...state,
      group: action.id,
    }),
  },
);
const cardAttributes = combineReducers({
  group: groupCardAttributes,
});
const pending = combineReducers({
  isMaster: pendingIsMaster,
  selections: pendingSelections,
  newGroupName: pendingNewGroupName,
});
const filters = combineReducers({
  client: createFilterReducer(AccessAction.SetFilterTextClient),
  item: createFilterReducer(AccessAction.SetFilterTextItem),
  group: createFilterReducer(AccessAction.SetFilterTextGroup),
  selections: createFilterReducer(AccessAction.SetFilterTextSelections),
});
const modals = combineReducers({
  addGroup: createModalReducer(AccessAction.OpenAddGroupModal, AccessAction.CloseAddGroupModal),
});

export const contentAccessAdmin = combineReducers({
  data,
  selected,
  cardAttributes,
  pending,
  filters,
  modals,
});
