import '../../../images/add.svg';
import '../../../scss/react/system-admin/system-admin.scss';

import * as React from 'react';

import { ContentPanel } from '../shared-components/content-panel';
import { Entity } from '../shared-components/entity';
import { DataSource, QueryFilter } from '../shared-components/interfaces';
import { ClientInfo, ProfitCenterInfo, RootContentItemInfo, UserInfo } from './interfaces';

export interface SystemAdminState {
  primaryDataSource: string;
  secondaryDataSource: string;
  secondaryQueryFilter: QueryFilter;
  finalQueryFilter: QueryFilter;
}

export class SystemAdmin extends React.Component<{}, SystemAdminState> {
  private controller: string = 'SystemAdmin';
  private nullDataSource: DataSource<Entity> = {
    name: null,
    parentSources: [],
    displayName: '',
    action: '',
    processResponse: () => null,
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
      processResponse: (response: UserInfo) => new Entity(
        response.Id,
        response.Name,
      ),
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
      processResponse: (response: ClientInfo) => new Entity(
        response.Id,
        response.Name,
      ),
    },
    {
      name: 'profitCenter',
      parentSources: [
        null,
      ],
      displayName: 'Profit Center',
      action: 'ProfitCenters',
      processResponse: (response: ProfitCenterInfo) => new Entity(
        response.Id,
        response.Name,
      ),
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
      processResponse: (response: RootContentItemInfo) => new Entity(
        response.Id,
        response.Name,
      ),
    },
  ];

  public constructor(props) {
    super(props);

    this.state = {
      primaryDataSource: 'user',
      secondaryDataSource: null,
      secondaryQueryFilter: {},
      finalQueryFilter: {},
    };

    this.setPrimaryDataSource = this.setPrimaryDataSource.bind(this);
    this.setSecondaryDataSource = this.setSecondaryDataSource.bind(this);
    this.setSecondaryQueryFilter = this.setSecondaryQueryFilter.bind(this);
    this.setFinalQueryFilter = this.setFinalQueryFilter.bind(this);
  }

  public render() {
    const primaryDataSources = this.getDataSources(null);
    const primaryDataSource = this.getDataSourceByName(primaryDataSources, this.state.primaryDataSource);

    const secondaryDataSources = this.getDataSources(this.state.primaryDataSource);
    const secondaryDataSource = this.getDataSourceByName(secondaryDataSources, this.state.secondaryDataSource);

    return [
      (
        <ContentPanel
          key={'primaryColumn'}
          controller={this.controller}
          dataSources={primaryDataSources}
          setSelectedDataSource={this.setPrimaryDataSource}
          selectedDataSource={primaryDataSource}
          setQueryFilter={this.setSecondaryQueryFilter}
          queryFilter={{}}
        />
      ),
      (
        <ContentPanel
          key={'secondaryColumn'}
          controller={this.controller}
          dataSources={secondaryDataSources}
          setSelectedDataSource={this.setSecondaryDataSource}
          selectedDataSource={secondaryDataSource}
          setQueryFilter={this.setFinalQueryFilter}
          queryFilter={{}}
        />
      ),
    ];
  }

  // callbacks for child components
  private setPrimaryDataSource(dataSource: string) {
    this.setState((prevState) => ({
      primaryDataSource: dataSource,
      secondaryDataSource: dataSource === prevState.primaryDataSource
        ? prevState.secondaryDataSource
        : null,
    }));
  }

  private setSecondaryDataSource(dataSource: string) {
    this.setState({
      secondaryDataSource: dataSource,
    });
  }

  private setSecondaryQueryFilter(queryFilter: QueryFilter) {
    this.setState({
      secondaryQueryFilter: queryFilter,
    });
  }

  private setFinalQueryFilter(queryFilter: QueryFilter) {
    this.setState({
      finalQueryFilter: queryFilter,
    });
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
