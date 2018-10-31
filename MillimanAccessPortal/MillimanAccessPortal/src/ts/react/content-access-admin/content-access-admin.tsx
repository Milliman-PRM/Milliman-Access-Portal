import * as React from 'react';

import { ReductionFieldInfo, ReductionFieldValueInfo } from '../../view-models/content-publishing';
import { SelectionGroupInfo } from '../models';
import { CardAttributes } from '../shared-components/card';
import { ContentPanel, ContentPanelProps } from '../shared-components/content-panel';
import { NavBar } from '../shared-components/navbar';
import { ClientInfo, RootContentItemInfo, UserInfo } from '../system-admin/interfaces';

interface NestedCardAttributes extends CardAttributes {
  parentId: string;
}

interface ContentAccessAdminProps {
  data: {
    clients: ClientInfo[];
    items: RootContentItemInfo[];
    groups: SelectionGroupInfo[];
    users: UserInfo[];
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
  };
  itemPanel: {
    cards: {
      [id: string]: {
        expanded: boolean;
        profitCenterModalOpen: boolean;
      };
    };
  };
  groupPanel: {
    cards: {
      [id: string]: {
        expanded: boolean;
        profitCenterModalOpen: boolean;
      };
    };
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
    return (
      <>
        <NavBar currentView={this.currentView} />
        <ContentPanel
          {...this.nullProps}
          cards={this.props.clientPanel.cards}
          entities={this.props.data.clients}
          panelHeader={'Clients'}
        />
        <ContentPanel
          {...this.nullProps}
          cards={this.props.itemPanel.cards}
          entities={this.props.data.items}
          panelHeader={'Content Items'}
        />
        <ContentPanel
          {...this.nullProps}
          cards={this.props.groupPanel.cards}
          entities={this.props.data.groups}
          panelHeader={'Selection Groups'}
        />
      </>
    );
  }
}
