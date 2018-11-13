import * as React from 'react';
import { connect } from 'react-redux';

import { ReductionStatus } from '../../view-models/content-publishing';
import {
  Client, ReductionFieldset, RootContentItem, RootContentItemWithStatus, SelectionGroupWithStatus,
} from '../models';
import { CardAttributes } from '../shared-components/card';
import { CardPanel, CardPanelProps } from '../shared-components/card-panel';
import { Guid } from '../shared-components/interfaces';
import { NavBar } from '../shared-components/navbar';
import * as actions from './redux/actions';
import {
  activeGroupsWithStatus, activeItemsWithStatus, activeReductionFieldsets, itemCardAttributes,
  modifiedReductionValues, pendingMaster, pendingReductionValues, selectedGroupWithStatus,
  selectedItem, selectionsFormModified,
} from './redux/selectors';
import { ContentAccessAdminState } from './redux/store';
import { SelectionsPanel } from './selections-panel';

interface ContentAccessAdminProps {
  clients: Client[];
  items: RootContentItemWithStatus[];
  groups: SelectionGroupWithStatus[];
  reductionFieldsets: ReductionFieldset[];
  clientPanel: {
    selectedCard: Guid;
  };
  itemPanel: {
    cards: {
      [id: string]: CardAttributes;
    };
    selectedCard: Guid;
  };
  groupPanel: {
    cards: {
      [id: string]: CardAttributes;
    };
    selectedCard: Guid;
  };
  selectedItem: RootContentItem;
  selectedGroup: SelectionGroupWithStatus;
  selectedValues: Guid[];
  modifiedValues: Guid[];
  selectedMaster: boolean;
  formModified: boolean;
}
interface ContentAccessAdminActions {
  nop: () => void;
  selectClientCard: (id: Guid) => actions.ActionWithId;
  selectItemCard: (id: Guid) => actions.ActionWithId;
  selectGroupCard: (id: Guid) => actions.ActionWithId;
  setMasterSelected: (bValue: boolean) => actions.ActionWithBoolean;
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
        cards={itemPanel.cards}
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
      modifiedValues,
      selectedMaster,
      setMasterSelected,
      formModified,
    } = this.props;
    const fieldsets = reductionFieldsets.map((s) => ({
      name: s.field.displayName,
      fields: s.values.map((v) => ({
        name: v.value,
        selected: selectedValues.indexOf(v.id) !== -1,
        modified: modifiedValues.indexOf(v.id) !== -1,
        onChange: (selected: boolean) => setValueSelected(v.id, selected),
      })),
    }));
    return groupPanel.selectedCard && (
      <SelectionsPanel
        isSuspended={group.isSuspended}
        doesReduce={item.doesReduce}
        isModified={formModified}
        isMaster={selectedMaster}
        onIsMasterChange={setMasterSelected}
        title={group.name}
        subtitle={item.name}
        status={group.status ? group.status.reductionStatus : ReductionStatus.Unspecified}
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
    items: activeItemsWithStatus(state),
    groups: activeGroupsWithStatus(state),
    reductionFieldsets: activeReductionFieldsets(state),
    clientPanel,
    itemPanel: {
      ...itemPanel,
      cards: itemCardAttributes(state),
    },
    groupPanel,
    selectedItem: selectedItem(state),
    selectedGroup: selectedGroupWithStatus(state),
    selectedValues: pendingReductionValues(state)
      ? pendingReductionValues(state).map((v) => v.id)
      : [],
    modifiedValues: modifiedReductionValues(state)
      ? modifiedReductionValues(state).map((v) => v.id)
      : [],
    selectedMaster: pendingMaster(state),
    formModified: selectionsFormModified(state),
  };
}

export const ConnectedContentAccessAdmin = connect(
  mapStateToProps,
  actions,
)(ContentAccessAdmin);
