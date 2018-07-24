import '../../../scss/react/shared-components/content-panel.scss';

import { ajax } from 'jquery';
import * as React from 'react';

import { Card } from './card';
import { ColumnSelector } from './column-selector';
import { Filter } from './filter';
import { DataSource, Entity, QueryFilter } from './interfaces';

export interface ContentPanelProps {
  controller: string;
  dataSources: Array<DataSource<Entity>>;
  setSelectedDataSource: (dataSource: string) => void;
  selectedDataSource: DataSource<Entity>;
  setQueryFilter: (queryFilter: QueryFilter) => void;
  queryFilter: QueryFilter;
}
interface ContentPanelState {
  entities: Entity[];
  filterText: string;
  selectedCard: number;
}

export class ContentPanel extends React.Component<ContentPanelProps, ContentPanelState> {
  private get url() {
    return this.props.selectedDataSource
      && `${this.props.controller}/${this.props.selectedDataSource.action}/`;
  }

  public constructor(props) {
    super(props);

    this.state = {
      entities: [],
      filterText: '',
      selectedCard: null,
    };

    this.setFilterText = this.setFilterText.bind(this);
    this.setSelectedCard = this.setSelectedCard.bind(this);
  }

  public componentDidMount() {
    this.fetch();
  }

  public render() {
    const cards = this.state.entities.map((entity) => (
      <li
        key={entity.Id}
        // tslint:disable-next-line:jsx-no-lambda
        onClick={() => this.setSelectedCard(entity.Id)}
      >
        <Card
          {...entity}
          selected={false}
        />
      </li>
    ));
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
              placeholderText={'this.props.???'}
              setFilterText={this.setFilterText}
              filterText={''}
            />
            <div className="admin-panel-action-icons-container">
              {'addIcon'}
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
      return this.setState({
        entities: [],
      });
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
    this.setState({ selectedCard: id });
  }
}
