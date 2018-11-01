import * as React from 'react';

import { ContentPanel, ContentPanelProps } from '../shared-components/content-panel';
import { Guid } from '../shared-components/interfaces';
import { NavBar } from '../shared-components/navbar';
import { Client, RootContentItem, SelectionGroup } from '../models';
import { connect } from 'react-redux';
import * as actions from './redux/actions';
import { ContentAccessAdminState } from './redux/store';
import { selectedItems, selectedGroups } from './redux/selectors';


interface ContentAccessAdminProps {
  clients: Client[];
  selectedItems: RootContentItem[];
  selectedGroups: SelectionGroup[];
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
}
interface ContentAccessAdminActions {
  nop: () => void;
}

class ContentAccessAdmin extends React.Component<ContentAccessAdminProps & ContentAccessAdminActions> {
  private readonly currentView: string = document
    .getElementsByTagName('body')[0].getAttribute('data-nav-location');

  private nullProps: ContentPanelProps = {
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
      </>
    );
  }

  private renderClientPanel() {
    const { clients, clientPanel } = this.props;
    return (
      <ContentPanel
        {...this.nullProps}
        {...clientPanel}
        entities={clients}
        panelHeader={'Clients'}
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
    const { selectedItems, clientPanel, itemPanel } = this.props;
    return clientPanel.selectedCard && (
      <ContentPanel
        {...this.nullProps}
        {...itemPanel}
        entities={selectedItems}
        panelHeader={'Content Items'}
      />
    );
  }

  private renderGroupPanel() {
    const { selectedGroups, itemPanel, groupPanel } = this.props;
    return itemPanel.selectedCard && (
      <ContentPanel
        {...this.nullProps}
        {...groupPanel}
        entities={selectedGroups}
        panelHeader={'Selection Groups'}
      />
    );
  }
}

function mapStateToProps(state: ContentAccessAdminState): ContentAccessAdminProps {
  const { clientPanel, itemPanel, groupPanel } = state;
  const { clients } = state.data;
  return {
    clients,
    selectedItems: selectedItems(state),
    selectedGroups: selectedGroups(state),
    clientPanel,
    itemPanel,
    groupPanel,
  };
}

export const ConnectedContentAccessAdmin = connect(
  mapStateToProps,
  actions,
)(ContentAccessAdmin);
