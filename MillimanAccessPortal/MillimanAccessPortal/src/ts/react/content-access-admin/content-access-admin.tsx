import * as React from 'react';

import { ReductionFieldInfo, ReductionFieldValueInfo } from '../../view-models/content-publishing';
import { Client, RootContentItem, SelectionGroup, User } from '../models';
import { ContentPanel, ContentPanelProps } from '../shared-components/content-panel';
import { Guid } from '../shared-components/interfaces';
import { NavBar } from '../shared-components/navbar';

export interface ContentAccessAdminProps {
  data: {
    clients: Client[];
    items: RootContentItem[];
    groups: SelectionGroup[];
    users: User[];
    fields: ReductionFieldInfo[];
    values: ReductionFieldValueInfo[];
  };
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

export class ContentAccessAdmin extends React.Component<ContentAccessAdminProps> {
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

  public render() {
    const { data } = this.props;
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
    const { data, clientPanel } = this.props;
    return (
      <ContentPanel
        {...this.nullProps}
        {...clientPanel}
        entities={data.clients}
        panelHeader={'Clients'}
        cardStats={[
          {
            name: 'Users',
            value: (_: Guid) => '?' as any,
            icon: 'user',
          },
          {
            name: 'Reports',
            value: (id: Guid) => data.items.filter(u => u.clientId === id).length,
            icon: 'reports',
          },
        ]}
      />
    );
  }

  private renderItemPanel() {
    const { data, clientPanel, itemPanel } = this.props;
    return clientPanel.selectedCard && (
      <ContentPanel
        {...this.nullProps}
        {...itemPanel}
        entities={data.items}
        panelHeader={'Content Items'}
      />
    );
  }

  private renderGroupPanel() {
    const { data, itemPanel, groupPanel } = this.props;
    return itemPanel.selectedCard && (
      <ContentPanel
        {...this.nullProps}
        {...groupPanel}
        entities={data.groups}
        panelHeader={'Selection Groups'}
      />
    );
  }
}
