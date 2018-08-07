import '../../../scss/react/shared-components/content-panel.scss';

import { ajax } from 'jquery';
import { isEqual } from 'lodash';
import * as React from 'react';

import { ActionIcon } from './action-icon';
import { Card } from './card';
import { ColumnSelector } from './column-selector';
import { Entity, EntityHelper } from './entity';
import { Filter } from './filter';
import { DataSource, QueryFilter, Structure } from './interfaces';

export interface ContentPanelProps {
  controller: string;
  dataSources: Array<DataSource<Entity>>;
  setSelectedDataSource: (sourceName: string) => void;
  selectedDataSource: DataSource<Entity>;
  setSelectedCard: (cardId: number) => void;
  selectedCard: number;
  queryFilter: QueryFilter;
}
interface ContentPanelState {
  entities: Entity[];
  filterText: string;
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
    return this.props.selectedDataSource.infoAction
      && `${this.props.controller}/${this.props.selectedDataSource.infoAction}/`;
  }

  public constructor(props) {
    super(props);

    this.state = {
      entities: null,
      filterText: '',
      prevQuery: null,
    };

    this.setFilterText = this.setFilterText.bind(this);
    this.addAction = this.addAction.bind(this);
  }

  public componentDidMount() {
    this.props.setSelectedDataSource(this.props.dataSources[0] && this.props.dataSources[0].name);
    this.fetch();
  }

  public componentDidUpdate() {
    if (this.state.entities === null) {
      this.fetch();
    }
    if (this.props.selectedDataSource.name === null) {
      this.props.setSelectedDataSource(this.props.dataSources[0] && this.props.dataSources[0].name);
    }
  }

  public render() {
    const filteredCards = this.state.entities && this.state.entities
      .filter((entity) => EntityHelper.applyFilter(entity, this.state.filterText));
    let cards = null;
    if (filteredCards === null) {
      cards = (<div>Loading...</div>);
    } else if (filteredCards.length === 0) {
      cards = (
        <div>No {this.props.selectedDataSource.displayName.toLowerCase()} found.</div>
      );
    } else if (this.props.selectedDataSource.structure === Structure.Tree) {
      const rootIndices = [];
      filteredCards.forEach((entity, i) => {
        if (entity.indent === 1) {
          rootIndices.push(i);
        }
      });
      const cardGroups = rootIndices.map((_, i) =>
        filteredCards.slice(rootIndices[i], rootIndices[i + 1]));
      cards = cardGroups.map((group, i) => {
        const groupCards = group.map((entity) => (
          <li
            key={entity.id}
            // tslint:disable-next-line:jsx-no-lambda
            onClick={() => this.props.setSelectedCard(entity.id)}
          >
            <Card
              {...entity}
              selected={entity.id === this.props.selectedCard}
            />
          </li>
        ));
        if (i + 1 !== cardGroups.length) {
          groupCards.push((<div key="hr-{i}" className="hr" />));
        }
        return groupCards;
      });
    } else {
      cards = filteredCards.map((entity) => (
        <li
          key={entity.id}
          // tslint:disable-next-line:jsx-no-lambda
          onClick={() => this.props.setSelectedCard(entity.id)}
        >
          <Card
            {...entity}
            selected={entity.id === this.props.selectedCard}
          />
        </li>
      ));
    }
    const filterPlaceholder = this.props.selectedDataSource.displayName
      ? `Filter ${this.props.selectedDataSource.displayName}...`
      : '';
    const actionIcon = this.props.selectedDataSource.createAction
      && (
        <ActionIcon
          title={'Add'}
          action={this.addAction}
          icon={'add'}
        />
      );
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
              filterText={this.state.filterText}
            />
            <div className="admin-panel-action-icons-container">
              {actionIcon}
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
      if (this.props.selectedDataSource) {
        this.setState({
          entities: this.props.selectedDataSource.processInfo(response),
        });
      }
    }).fail((response) => {
      throw new Error(response.getResponseHeader('Warning') || 'Unknown error');
    });
  }

  private setFilterText(filterText: string) {
    this.setState({ filterText });
  }

  private addAction() {
    throw new Error('Not implemented');
  }
}
