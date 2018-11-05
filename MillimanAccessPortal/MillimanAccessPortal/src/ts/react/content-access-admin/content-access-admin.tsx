import * as React from 'react';
import { connect } from 'react-redux';

import {
  Client, ReductionFieldset, ReductionFieldValue, RootContentItem, SelectionGroup,
} from '../models';
import { CardPanel, CardPanelProps } from '../shared-components/card-panel';
import { Guid } from '../shared-components/interfaces';
import { NavBar } from '../shared-components/navbar';
import { FieldsetProps } from './fieldset';
import * as actions from './redux/actions';
import {
  activeGroups, activeItems, activeReductionFields, activeReductionFieldsets, activeReductionValues,
  selectedGroup, selectedItem, selectedReductionValues,
} from './redux/selectors';
import { ContentAccessAdminState } from './redux/store';
import { SelectionsPanel } from './selections-panel';

interface ContentAccessAdminProps {
  clients: Client[];
  items: RootContentItem[];
  groups: SelectionGroup[];
  reductionFieldsets: ReductionFieldset[];
  clientPanel: {
    cards: {
      [id: string]: {
        expanded: boolean;
        profitCenterModalOpen: boolean;
      };
    };
    selectedCard: Guid;
  };
  itemPanel: {
    cards: {
      [id: string]: {
        expanded: boolean;
        profitCenterModalOpen: boolean;
      };
    };
    selectedCard: Guid;
  };
  groupPanel: {
    cards: {
      [id: string]: {
        expanded: boolean;
        profitCenterModalOpen: boolean;
      };
    };
    selectedCard: Guid;
  };
  selectedItem: RootContentItem;
  selectedGroup: SelectionGroup;
  selectedValues: Guid[];
}
interface ContentAccessAdminActions {
  nop: () => void;
  selectClientCard: (id: Guid) => actions.ActionWithId;
  selectItemCard: (id: Guid) => actions.ActionWithId;
  selectGroupCard: (id: Guid) => actions.ActionWithId;
  setValueSelected: (id: Guid, bValue: boolean) => actions.ActionWithId & actions.ActionWithBoolean;
}

class ContentAccessAdmin extends React.Component<ContentAccessAdminProps & ContentAccessAdminActions> {
  private readonly currentView: string = document
    .getElementsByTagName('body')[0].getAttribute('data-nav-location');

  private nullProps: CardPanelProps = {
    createAction: null,
    modalOpen: false,
    onCardSelect: () => null,
    onClientUserRemove: () => null,
    onExpandedToggled: () => null,
    onFilterTextChange: () => null,
    onModalClose: () => null,
    onModalOpen: () => null,
    onProfitCenterDelete: () => null,
    onProfitCenterModalClose: () => null,
    onProfitCenterModalOpen: () => null,
    onProfitCenterUserRemove: () => null,
    onSendReset: () => null,
    queryFilter: null,
    selectedCard: null,
    filterText: '',
    cards: {},
    entities: [],
    panelHeader: '',
  };

  public componentDidMount() {
    this.props.nop();
  }

  public render() {
    return (
      <>
        <NavBar currentView={this.currentView} />
        {this.renderClientPanel()}
        {this.renderItemPanel()}
        {this.renderGroupPanel()}
        {this.renderSelectionsPanel()}
      </>
    );
  }

  private renderClientPanel() {
    const { clients, clientPanel } = this.props;
    return (
      <CardPanel
        {...this.nullProps}
        {...clientPanel}
        entities={clients}
        panelHeader={'Clients'}
        onCardSelect={this.props.selectClientCard}
        cardStats={[
          {
            name: 'Users',
            value: () => '?' as any,
            icon: 'user',
          },
          {
            name: 'Reports',
            value: () => '?' as any,
            icon: 'reports',
          },
        ]}
      />
    );
  }

  private renderItemPanel() {
    const { items, clientPanel, itemPanel } = this.props;
    return clientPanel.selectedCard && (
      <CardPanel
        {...this.nullProps}
        {...itemPanel}
        entities={items}
        panelHeader={'Content Items'}
        onCardSelect={this.props.selectItemCard}
      />
    );
  }

  private renderGroupPanel() {
    const { groups, itemPanel, groupPanel } = this.props;
    return itemPanel.selectedCard && (
      <CardPanel
        {...this.nullProps}
        {...groupPanel}
        entities={groups}
        panelHeader={'Selection Groups'}
        onCardSelect={this.props.selectGroupCard}
      />
    );
  }

  private renderSelectionsPanel() {
    const {
      selectedItem: item,
      selectedGroup: group,
      reductionFieldsets,
      groupPanel,
      setValueSelected,
      selectedValues,
    } = this.props;
    const fieldsets = reductionFieldsets.map((s) => ({
      name: s.field.displayName,
      fields: s.values.map((v) => ({
        name: v.value,
        selected: selectedValues.indexOf(v.id) !== -1,
        onChange: (selected: boolean) => setValueSelected(v.id, selected),
      })),
    }));
    return groupPanel.selectedCard && (
      <SelectionsPanel
        isSuspended={group.isSuspended}
        doesReduce={item.doesReduce}
        isMaster={group.isMaster}
        fieldsets={fieldsets}
      />
    );
  }
}

function mapStateToProps(state: ContentAccessAdminState): ContentAccessAdminProps {
  const { clientPanel, itemPanel, groupPanel } = state;
  const { clients } = state.data;
  return {
    clients,
    items: activeItems(state),
    groups: activeGroups(state),
    reductionFieldsets: activeReductionFieldsets(state),
    clientPanel,
    itemPanel,
    groupPanel,
    selectedItem: selectedItem(state),
    selectedGroup: selectedGroup(state),
    selectedValues: selectedGroup(state)
      ? selectedGroup(state).selectedValues
      : [],
  };
}

export const ConnectedContentAccessAdmin = connect(
  mapStateToProps,
  actions,
)(ContentAccessAdmin);
