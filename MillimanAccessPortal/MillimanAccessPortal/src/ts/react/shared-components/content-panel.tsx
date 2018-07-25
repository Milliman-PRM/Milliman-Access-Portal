import '../../../scss/react/shared-components/content-panel.scss';

import { ajax } from 'jquery';
import { isEqual } from 'lodash';
import * as React from 'react';

import { ActionIcon } from './action-icon';
import { Card } from './card';
import { ColumnSelector } from './column-selector';
import { Entity } from './entity';
import { Filter } from './filter';
import { DataSource, QueryFilter } from './interfaces';

export interface ContentPanelProps {
  controller: string;
  dataSources: Array<DataSource<Entity>>;
  setSelectedDataSource: (sourceName: string) => void;
  selectedDataSource: DataSource<Entity>;
  setQueryFilter: (queryFilter: QueryFilter) => void;
  queryFilter: QueryFilter;
}
interface ContentPanelState {
  entities: Entity[];
  filterText: string;
  selectedCard: number;
  prevQuery: {
    queryFilter: QueryFilter;
    sourceName: string;
  };
}

export class ContentPanel extends React.Component<ContentPanelProps, ContentPanelState> {

  // see https://github.com/reactjs/rfcs/issues/26#issuecomment-365744134
  public static getDerivedStateFromProps(
    nextProps: ContentPanelProps, prevState: ContentPanelState,
  ): Partial<ContentPanelState> {
    const nextQuery = {
      queryFilter: nextProps.queryFilter,
      sourceName: nextProps.selectedDataSource.name,
    };
    if (!isEqual(nextQuery, prevState.prevQuery)) {
      return {
        prevQuery: nextQuery,
        entities: null,
      };
    }
    return null;
  }

  private get url() {
    return this.props.selectedDataSource.action
      && `${this.props.controller}/${this.props.selectedDataSource.action}/`;
  }

  public constructor(props) {
    super(props);

    this.state = {
      entities: null,
      filterText: '',
      selectedCard: null,
      prevQuery: null,
    };

    this.setFilterText = this.setFilterText.bind(this);
    this.setSelectedCard = this.setSelectedCard.bind(this);
  }

  public componentDidMount() {
    this.fetch();
  }

  public componentDidUpdate() {
    if (this.state.entities === null) {
      this.fetch();
    }
  }

  public render() {
    const cards = this.state.entities === null
      ? (<div>Loading...</div>)
      : this.state.entities
        .filter((entity) => entity.applyFilter(this.state.filterText))
        .map((entity) => (
          <li
            key={entity.id}
            // tslint:disable-next-line:jsx-no-lambda
            onClick={() => this.setSelectedCard(entity.id)}
          >
            <Card
              id={entity.id}
              primaryText={entity.primaryText}
              selected={entity.id === this.state.selectedCard}
            />
          </li>
        ));
    const filterPlaceholder = this.props.selectedDataSource.displayName
      ? `Filter ${this.props.selectedDataSource.displayName}...`
      : '';
    return (
      <div
        className="admin-panel-container flex-item-12-12 flex-item-for-tablet-up-4-12 flex-item-for-desktop-up-3-12"
      >
        <ColumnSelector
          {...this.props}
        />
        <div className="admin-panel-list">
          <div className="admin-panel-toolbar">
            <Filter
              placeholderText={filterPlaceholder}
              setFilterText={this.setFilterText}
              filterText={''}
            />
            <div className="admin-panel-action-icons-container">
              <ActionIcon
                title={''}
                action={() => {}}
                icon={'add'}
              />
            </div>
          </div>
          <div className="admin-panel-content-container">
            <ul className="admin-panel-content">
              {cards}
            </ul>
          </div>
        </div>
      </div>
    );
  }

  private fetch() {
    if (!this.url) {
      return this.setState({ entities: [] });
    }

    ajax({
      data: this.props.queryFilter,
      method: 'GET',
      url: this.url,
    }).done((response) => {
      if (typeof(response.map) !== 'function') {
        throw new Error('Malformed response');
      }
      const process = this.props.selectedDataSource
        ? this.props.selectedDataSource.processResponse
        : (entity) => entity;
      this.setState({
        entities: response.map(process),
      });
    }).fail((response) => {
      throw new Error(response.getResponseHeader('Warning') || 'Unknown error');
    });
  }

  private setFilterText(filterText: string) {
    this.setState({ filterText });
  }

  private setSelectedCard(id: number) {
    this.setState((prevState) => ({
      selectedCard: prevState.selectedCard === id
        ? null
        : id,
    }));

    const queryFilter = {...this.props.queryFilter};
    Object.assign(queryFilter, this.props.selectedDataSource.assignQueryFilter(
      id === this.state.selectedCard ? null : id));
    this.props.setQueryFilter(queryFilter);
  }
}
