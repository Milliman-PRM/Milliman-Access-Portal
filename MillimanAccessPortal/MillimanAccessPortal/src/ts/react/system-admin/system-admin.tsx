import '../../../images/add.svg';
import '../../../images/client-admin.svg';
import '../../../images/group.svg';
import '../../../images/reports.svg';
import '../../../images/user.svg';
import '../../../scss/react/system-admin/system-admin.scss';

import * as React from 'react';

import { BasicNode, BasicTree, Nestable } from '../../view-models/content-publishing';
import { ContentPanel } from '../shared-components/content-panel';
import { Entity } from '../shared-components/entity';
import { DataSource } from '../shared-components/interfaces';
import { ClientInfo, ProfitCenterInfo, RootContentItemInfo, UserInfo } from './interfaces';

export interface SystemAdminState {
  primaryDataSource: string;
  secondaryDataSource: string;
  primarySelectedCard: number;
  secondarySelectedCard: number;
}

export class SystemAdmin extends React.Component<{}, SystemAdminState> {
  private controller: string = 'SystemAdmin';
  private nullDataSource: DataSource<Entity> = {
    name: null,
    parentSources: [],
    displayName: '',
    action: '',
    createAction: null,
    processResponse: () => null,
    assignQueryFilter: () => null,
  };
  private dataSources: Array<DataSource<Entity>> = [
    {
      name: 'user',
      parentSources: [
        null,
        'client',
        {
          name: 'profitCenter',
          overrides: {
            displayName: 'Authorized Users',
          },
        },
      ],
      displayName: 'Users',
      action: 'Users',
      createAction: 'CreateUser',
      processResponse: (response: UserInfo[]) => response.map((user) => ({
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
        detailList: user.RootContentItems && user.RootContentItems.map((item) => item.Name),
      })),
      assignQueryFilter: (userId: number) => ({ userId }),
    },
    {
      name: 'client',
      parentSources: [
        null,
        'user',
        'profitCenter',
      ],
      displayName: 'Clients',
      action: 'Clients',
      createAction: null,
      processResponse: (response: BasicTree<ClientInfo>) => {
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
      assignQueryFilter: (clientId: number) => ({ clientId }),
    },
    {
      name: 'profitCenter',
      parentSources: [
        null,
      ],
      displayName: 'Profit Center',
      action: 'ProfitCenters',
      createAction: 'CreateProfitCenter',
      processResponse: (response: ProfitCenterInfo[]) => response.map((profitCenter) => ({
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
      })),
      assignQueryFilter: (profitCenterId: number) => ({ profitCenterId }),
    },
    {
      name: 'rootContentItem',
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
      action: 'RootContentItems',
      createAction: null,
      processResponse: (response: RootContentItemInfo[]) => response.map((item) => ({
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
        detailList: item.Users && item.Users.map((user) => user.FirstName),
      })),
      assignQueryFilter: (rootContentItemId: number) => ({ rootContentItemId }),
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
    const primaryDataSources = this.getDataSources(null);
    const primaryDataSource = this.getDataSourceByName(primaryDataSources, this.state.primaryDataSource);

    const secondaryDataSources = this.getDataSources(this.state.primaryDataSource);
    const secondaryDataSource = this.getDataSourceByName(secondaryDataSources, this.state.secondaryDataSource);

    const secondaryQueryFilter = Object.assign(
      {}, primaryDataSource.assignQueryFilter(this.state.primarySelectedCard));

    return [
      (
        <ContentPanel
          key={'primaryColumn'}
          controller={this.controller}
          dataSources={primaryDataSources}
          setSelectedDataSource={this.setPrimaryDataSource}
          selectedDataSource={primaryDataSource}
          setSelectedCard={this.setPrimarySelectedCard}
          selectedCard={this.state.primarySelectedCard}
          queryFilter={{}}
        />
      ),
      this.state.primarySelectedCard && (
        <ContentPanel
          key={'secondaryColumn'}
          controller={this.controller}
          dataSources={secondaryDataSources}
          setSelectedDataSource={this.setSecondaryDataSource}
          selectedDataSource={secondaryDataSource}
          setSelectedCard={this.setSecondarySelectedCard}
          selectedCard={this.state.secondarySelectedCard}
          queryFilter={secondaryQueryFilter}
        />
      ),
    ];
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
    }));
  }

  private setSecondaryDataSource(sourceName: string) {
    this.setState({ secondaryDataSource: sourceName });
  }

  private setPrimarySelectedCard(cardId: number) {
    this.setState((prevState) => ({
      primarySelectedCard: prevState.primarySelectedCard === cardId
        ? null
        : cardId,
    }));
  }

  private setSecondarySelectedCard(cardId: number) {
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
