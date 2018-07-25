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
  private dataSources: Array<DataSource<Entity>> = [
    {
      name: 'user',
      sources: [
        null,
        'client',
        'profitCenter',
      ],
      displayName: 'Users',
      action: 'Users',
      processResponse: (response: UserInfo) => (new Entity(
        response.Id,
        response.Name,
      )),
    },
    {
      name: 'client',
      sources: [
        null,
        'user',
        'profitCenter',
      ],
      displayName: 'Clients',
      action: 'Clients',
      processResponse: (response: ClientInfo) => (new Entity(
        response.Id,
        response.Name,
      )),
    },
    {
      name: 'profitCenter',
      sources: [
        null,
      ],
      displayName: 'PCs',
      action: 'ProfitCenters',
      processResponse: (response: ProfitCenterInfo) => (new Entity(
        response.Id,
        response.Name,
      )),
    },
    {
      name: 'rootContentItem',
      sources: [
        'user',
        'client',
      ],
      displayName: 'Content',
      action: 'RootContentItems',
      processResponse: (response: RootContentItemInfo) => (new Entity(
        response.Id,
        response.Name,
      )),
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
    const primaryDataSources = this.dataSources.filter((source) =>
      source.sources.filter((parent) => parent === null).length);
    const primaryDataSource = primaryDataSources.filter((source) =>
      this.state.primaryDataSource === source.name)[0];

    const secondaryDataSources = this.dataSources.filter((source) =>
      source.sources.filter((parent) =>
        parent === this.state.primaryDataSource).length);
    const secondaryDataSource = secondaryDataSources.filter((source) =>
      this.state.secondaryDataSource === source.name)[0];
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
}
