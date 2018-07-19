import '../../../scss/react/shared-components/content-panel.scss';

import { ajax } from 'jquery';
import * as React from 'react';

import { Card } from './card';
import { Filter } from './filter';
import { Entity, QueryFilter } from './interfaces';

export interface ContentListProps {
  setQueryFilter: (queryFilter: QueryFilter) => void;
  queryFilter: QueryFilter;
  controller: string;
  action: string;
}
interface ContentListState<T extends Entity> {
  entities: T[];
  filterText: string;
  selectedCard: number;
}

export abstract class ContentList<TEntity extends Entity, TCard extends Card<TEntity>>
    extends React.Component<ContentListProps, ContentListState<TEntity>> {
  protected _selectedColumn: number = null;
  protected _selectedCard: number = null;

  private get url() {
    return this.props.action
      ? `${this.props.controller}/${this.props.action}/`
      : '/';
  }

  public constructor(props) {
    super(props);

    this.state = {
      entities: [],
      filterText: '',
      selectedCard: 0,
    };

    this.selectCard = this.selectCard.bind(this);
    this.setFilterText = this.setFilterText.bind(this);
  }

  public render() {
    // create ctor function from parameterized type
    // see https://github.com/Microsoft/TypeScript/issues/3960#issuecomment-165330151
    type ListCard = new () => TCard;
    const ListCard = Card as ListCard;  // tslint:disable-line:variable-name

    const cards = this.state.entities.map((entity) => (
      <li
        key={entity.Id}
        // tslint:disable-next-line:jsx-no-lambda
        onClick={() => this.selectCard(entity.Id)}
      >
        <ListCard
          entity={entity}
          selected={this.state.selectedCard === entity.Id}
        />
      </li>
    ));
    return (
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
    );
  }

  public componentDidMount() {
    this.fetch();
  }

  protected abstract renderQueryFilter(id: number): QueryFilter;

  private fetch() {
    ajax({
      data: this.props.queryFilter,
      method: 'GET',
      url: this.url,
    }).done((response: TEntity[]) => {
      this.setState({
        entities: response,
      });
    }).fail((response) => {
      throw new Error(response.getResponseHeader('Warning')
        || 'An unknown error has occurred.');
    });
  }

  private selectCard(id: number) {
    this.setState({
      selectedCard: id,
    });
    this.props.setQueryFilter(this.renderQueryFilter(id));
  }
  private setFilterText(filterText: string) {
    this.setState({
      filterText,
    });
  }
}
