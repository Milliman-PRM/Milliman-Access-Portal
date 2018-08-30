import '../../../images/add.svg';
import '../../../images/client-admin.svg';
import '../../../images/email.svg';
import '../../../images/expand-card.svg';
import '../../../images/group.svg';
import '../../../images/reports.svg';
import '../../../images/user.svg';
import '../../../scss/react/system-admin/system-admin.scss';

import * as React from 'react';

import { BasicNode, BasicTree } from '../../view-models/content-publishing';
import { ContentPanel } from '../shared-components/content-panel';
import { Entity } from '../shared-components/entity';
import { DataSource, Structure } from '../shared-components/interfaces';
import { NavBar } from '../shared-components/navbar';
import { ClientInfo, ProfitCenterInfo, RootContentItemInfo, UserInfo } from './interfaces';
import { PrimaryDetailPanel } from './primary-detail-panel';
import { SecondaryDetailPanel } from './secondary-detail-panel';

export interface SystemAdminState {
  primaryDataSource: string;
  secondaryDataSource: string;
  primarySelectedCard: string;
  secondarySelectedCard: string;
}

export class SystemAdmin extends React.Component<{}, SystemAdminState> {
  private controller: string = 'SystemAdmin';
  private readonly currentView: string = document
    .getElementsByTagName('body')[0].getAttribute('data-nav-location');
  private nullDataSource: DataSource<Entity> = {
    name: null,
    structure: 0,
    parentSources: [],
    displayName: '',
    infoAction: '',
    detailAction: '',
    createAction: null,
    processInfo: () => null,
    assignQueryFilter: () => null,
  };
  private dataSources: Array<DataSource<Entity>> = [
    {
      name: 'user',
      structure: Structure.List,
      parentSources: [
        null,
        {
          name: 'client',
          overrides: {
            createAction: 'AddUserToClient',
          },
        },
        {
          name: 'profitCenter',
          overrides: {
            createAction: 'AddUserToProfitCenter',
            displayName: 'Authorized Users',
          },
        },
      ],
      displayName: 'Users',
      sublistInfo: {
        title: 'Content Items',
        icon: 'reports',
        emptyText: 'This user does not have access to any reports.',
      },
      infoAction: 'Users',
      detailAction: 'UserDetail',
      createAction: 'CreateUser',
      processInfo: (response: UserInfo[]) => response.map((user) => ({
        id: user.Id,
        primaryText: `${user.LastName}, ${user.FirstName}`,
        secondaryText: user.UserName,
        primaryStat: user.ClientCount !== null && {
          name: 'Clients',
          value: user.ClientCount,
          icon: 'client-admin',
        },
        secondaryStat: user.RootContentItemCount !== null && {
          name: 'Reports',
          value: user.RootContentItemCount,
          icon: 'reports',
        },
        sublist: user.RootContentItems && user.RootContentItems.map((item) => ({
          id: item.Id,
          primaryText: item.Name,
        })),
        activated: user.Activated,
        email: user.Email,
        suspended: user.IsSuspended,
        isUserInProfitCenter: user.ProfitCenterId,
      })),
      assignQueryFilter: (userId: string) => ({ userId }),
    },
    {
      name: 'client',
      structure: Structure.Tree,
      parentSources: [
        null,
        'user',
        'profitCenter',
      ],
      displayName: 'Clients',
      infoAction: 'Clients',
      detailAction: 'ClientDetail',
      createAction: null,
      processInfo: (response: BasicTree<ClientInfo>) => {
        interface ClientDepth {
          client: ClientInfo;
          depth: number;
        }
        function traverse(node: BasicNode<ClientInfo>, list: ClientDepth[] = [], depth = 0): ClientDepth[] {
          if (node.Value !== null) {
            const clientDepth = {
              client: node.Value,
              depth,
            };
            list.push(clientDepth);
          }
          if (node.Children.length) {
            node.Children.forEach((child) => list = traverse(child, list, depth + 1));
          }
          return list;
        }
        const clientDepthList = traverse(response.Root);
        return clientDepthList.map((cd) => ({
          id: cd.client.Id,
          primaryText: cd.client.Name,
          secondaryText: cd.client.Code,
          primaryStat: cd.client.UserCount !== null && {
            name: 'Users',
            value: cd.client.UserCount,
            icon: 'user',
          },
          secondaryStat: cd.client.RootContentItemCount !== null && {
            name: 'Reports',
            value: cd.client.RootContentItemCount,
            icon: 'reports',
          },
          indent: cd.depth,
          readOnly: cd.client.ParentOnly,
        }));
      },
      assignQueryFilter: (clientId: string) => ({ clientId }),
    },
    {
      name: 'profitCenter',
      structure: Structure.List,
      parentSources: [
        null,
      ],
      displayName: 'Profit Center',
      infoAction: 'ProfitCenters',
      detailAction: 'ProfitCenterDetail',
      createAction: 'CreateProfitCenter',
      processInfo: (response: ProfitCenterInfo[]) => response.map((profitCenter) => ({
        id: profitCenter.Id,
        primaryText: profitCenter.Name,
        secondaryText: profitCenter.Office,
        primaryStat: {
          name: 'Authorized users',
          value: profitCenter.UserCount,
          icon: 'user',
        },
        secondaryStat: {
          name: 'Clients',
          value: profitCenter.ClientCount,
          icon: 'client-admin',
        },
        isProfitCenter: true,
      })),
      assignQueryFilter: (profitCenterId: string) => ({ profitCenterId }),
    },
    {
      name: 'rootContentItem',
      structure: Structure.List,
      parentSources: [
        {
          name: 'user',
          overrides: {
            displayName: 'Authorized Content',
          },
        },
        'client',
      ],
      displayName: 'Content Items',
      sublistInfo: {
        title: 'Members',
        icon: 'user',
        emptyText: 'No users have access to this report.',
      },
      infoAction: 'RootContentItems',
      detailAction: 'RootContentItemDetail',
      createAction: null,
      processInfo: (response: RootContentItemInfo[]) => response.map((item) => ({
        id: item.Id,
        primaryText: item.Name,
        secondaryText: item.ClientName,
        primaryStat: item.UserCount !== null && {
          name: 'Users',
          value: item.UserCount,
          icon: 'user',
        },
        secondaryStat: item.SelectionGroupCount !== null && {
          name: 'Selection Groups',
          value: item.SelectionGroupCount,
          icon: 'group',
        },
        sublist: item.Users && item.Users.map((user) => ({
          id: user.Id,
          primaryText: `${user.FirstName} ${user.LastName}`,
          secondaryText: user.UserName,
        })),
        suspended: item.IsSuspended,
      })),
      assignQueryFilter: (rootContentItemId: string) => ({ rootContentItemId }),
    },
  ];

  public constructor(props) {
    super(props);

    this.state = {
      primaryDataSource: 'user',
      secondaryDataSource: null,
      primarySelectedCard: null,
      secondarySelectedCard: null,
    };

    this.setPrimaryDataSource = this.setPrimaryDataSource.bind(this);
    this.setSecondaryDataSource = this.setSecondaryDataSource.bind(this);
    this.setPrimarySelectedCard = this.setPrimarySelectedCard.bind(this);
    this.setSecondarySelectedCard = this.setSecondarySelectedCard.bind(this);
  }

  public render() {

    // pass available data sources as props based on defined data sources
    const primaryDataSources = this.getDataSources(null);
    const primaryDataSource = this.getDataSourceByName(primaryDataSources, this.state.primaryDataSource);
    const secondaryDataSources = this.getDataSources(this.state.primaryDataSource);
    const secondaryDataSource = this.getDataSourceByName(secondaryDataSources, this.state.secondaryDataSource);

    const secondaryQueryFilter = Object.assign(
      {}, primaryDataSource.assignQueryFilter(this.state.primarySelectedCard));
    const finalQueryFilter = Object.assign(
      {},
      primaryDataSource.assignQueryFilter(this.state.primarySelectedCard),
      secondaryDataSource.assignQueryFilter(this.state.secondarySelectedCard),
    );

    const secondaryColumnComponent = this.state.primarySelectedCard
      ? (
        <ContentPanel
          controller={this.controller}
          dataSources={secondaryDataSources}
          setSelectedDataSource={this.setSecondaryDataSource}
          selectedDataSource={secondaryDataSource}
          setSelectedCard={this.setSecondarySelectedCard}
          selectedCard={this.state.secondarySelectedCard}
          queryFilter={secondaryQueryFilter}
        />
      )
      : null;
    return (
      <>
        <NavBar
          currentView={this.currentView}
        />
        <ContentPanel
          controller={this.controller}
          dataSources={primaryDataSources}
          setSelectedDataSource={this.setPrimaryDataSource}
          selectedDataSource={primaryDataSource}
          setSelectedCard={this.setPrimarySelectedCard}
          selectedCard={this.state.primarySelectedCard}
          queryFilter={{}}
        />
        {secondaryColumnComponent}
        <div
          className="admin-panel-container flex-item-12-12 flex-item-for-tablet-up-4-12 flex-item-for-desktop-up-6-12"
          style={{overflowY: 'auto'}}
        >
          <PrimaryDetailPanel
            controller={this.controller}
            selectedDataSource={primaryDataSource}
            selectedCard={this.state.primarySelectedCard}
            queryFilter={secondaryQueryFilter}
          />
          <SecondaryDetailPanel
            controller={this.controller}
            primarySelectedDataSource={primaryDataSource}
            secondarySelectedDataSource={secondaryDataSource}
            selectedCard={this.state.secondarySelectedCard}
            queryFilter={finalQueryFilter}
          />
        </div>
      </>
    );
  }

  // callbacks for child components
  private setPrimaryDataSource(sourceName: string) {
    this.setState((prevState) => ({
      primaryDataSource: sourceName,
      secondaryDataSource: sourceName === prevState.primaryDataSource
        ? prevState.secondaryDataSource
        : null,
      primarySelectedCard: sourceName === prevState.primaryDataSource
        ? prevState.primarySelectedCard
        : null,
      secondarySelectedCard: sourceName === prevState.primaryDataSource
        ? prevState.secondarySelectedCard
        : null,
    }));
  }

  private setSecondaryDataSource(sourceName: string) {
    this.setState((prevState) => ({
      secondaryDataSource: sourceName,
      secondarySelectedCard: sourceName === prevState.secondaryDataSource
        ? prevState.secondarySelectedCard
        : null,
    }));
  }

  private setPrimarySelectedCard(cardId: string) {
    this.setState((prevState) => ({
      primarySelectedCard: prevState.primarySelectedCard === cardId
        ? null
        : cardId,
      secondarySelectedCard: null,
    }));
  }

  private setSecondarySelectedCard(cardId: string) {
    this.setState((prevState) => ({
      secondarySelectedCard: prevState.secondarySelectedCard === cardId
        ? null
        : cardId,
    }));
  }

  // utility methods
  private getDataSources(parentName: string): Array<DataSource<Entity>> {
    return this.dataSources
      // strip non-matching parent sources
      .map((dataSource) => {
        const filteredDataSource = {...dataSource};
        filteredDataSource.parentSources = filteredDataSource.parentSources.filter((parentSource) =>
          parentSource === null || typeof parentSource === 'string'
            ? parentSource === parentName
            : parentSource.name === parentName);
        return filteredDataSource;
      })
      // filter out data sources without the parent source
      .filter((dataSource) => dataSource.parentSources.length)
      // apply overrides if present
      .map((dataSource) => {
        const parentSource = dataSource.parentSources[0];
        if (parentSource !== null && typeof parentSource !== 'string') {
          Object.assign(dataSource, parentSource.overrides);
        }
        return dataSource;
      });
  }

  private getDataSourceByName(dataSources: Array<DataSource<Entity>>, name: string): DataSource<Entity> {
    return dataSources.filter((dataSource) => dataSource.name === name)[0]
      || this.nullDataSource;
  }
}
