import '../../../scss/react/shared-components/content-panel.scss';

import { isEqual } from 'lodash';
import * as React from 'react';
import * as Modal from 'react-modal';

import { getData, postData } from '../../shared';
import { AddUserToClientModal } from '../system-admin/modals/add-user-to-client';
import { AddUserToProfitCenterModal } from '../system-admin/modals/add-user-to-profit-center';
import { CreateProfitCenterModal } from '../system-admin/modals/create-profit-center';
import { CreateUserModal } from '../system-admin/modals/create-user';
import { ActionIcon } from './action-icon';
import { Card, withActivated } from './card';
import { ColumnSelector } from './column-selector';
import { Entity, EntityHelper } from './entity';
import { Filter } from './filter';
import { DataSource, QueryFilter, Structure } from './interfaces';

export interface ContentPanelProps {
  controller: string;
  dataSources: Array<DataSource<Entity>>;
  setSelectedDataSource: (sourceName: string) => void;
  selectedDataSource: DataSource<Entity>;
  setSelectedCard: (cardId: string) => void;
  selectedCard: string;
  queryFilter: QueryFilter;
}
interface ContentPanelState {
  entities: Entity[];
  filterText: string;
  prevQuery: {
    queryFilter: QueryFilter;
    sourceName: string;
  };
  modalOpen: boolean;
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

  // tslint:disable-next-line:variable-name
  private UserCard = withActivated(Card);

  private get url() {
    return this.props.selectedDataSource.infoAction
      && `/${this.props.controller}/${this.props.selectedDataSource.infoAction}`;
  }

  private get createUrl() {
    return this.props.selectedDataSource.createAction
      && `/${this.props.controller}/${this.props.selectedDataSource.createAction}`;
  }

  public constructor(props) {
    super(props);

    this.state = {
      entities: null,
      filterText: '',
      prevQuery: null,
      modalOpen: false,
    };

    this.setFilterText = this.setFilterText.bind(this);
    this.addAction = this.addAction.bind(this);
    this.closeModal = this.closeModal.bind(this);
    this.handleCreate = this.handleCreate.bind(this);
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
          >
            <Card
              {...entity}
              selected={entity.id === this.props.selectedCard}
              // tslint:disable-next-line:jsx-no-lambda
              setSelected={() => this.props.setSelectedCard(entity.id)}
              sublistInfo={this.props.selectedDataSource.sublistInfo}
            />
          </li>
        ));
        if (i + 1 !== cardGroups.length) {
          groupCards.push((<div key="hr-{i}" className="hr" />));
        }
        return groupCards;
      });
    } else if (this.props.selectedDataSource.name === 'user') {
      cards = filteredCards.map((entity) => (
        <li
          key={entity.id}
        >
          <this.UserCard
            {...entity}
            selected={entity.id === this.props.selectedCard}
            // tslint:disable-next-line:jsx-no-lambda
            setSelected={() => this.props.setSelectedCard(entity.id)}
            activated={entity.activated}
            resetButton={true}
            sublistInfo={this.props.selectedDataSource.sublistInfo}
          />
        </li>
      ));
    } else {
      cards = filteredCards.map((entity) => (
        <li
          key={entity.id}
        >
          <Card
            {...entity}
            selected={entity.id === this.props.selectedCard}
            // tslint:disable-next-line:jsx-no-lambda
            setSelected={() => this.props.setSelectedCard(entity.id)}
            sublistInfo={this.props.selectedDataSource.sublistInfo}
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

    const modalStyle = {
      overlay: {
        zIndex: 200,
      },
    };

    const modal = (() => {
      switch (this.props.selectedDataSource.createAction) {
        case 'CreateUser':
          return (
            <CreateUserModal
              isOpen={this.state.modalOpen}
              onRequestClose={this.closeModal}
              style={modalStyle}
            />
          );
        case 'CreateProfitCenter':
          return (
            <CreateProfitCenterModal
              isOpen={this.state.modalOpen}
              onRequestClose={this.closeModal}
              style={modalStyle}
            />
          );
        case 'AddUserToClient':
          return (
            <AddUserToClientModal
              isOpen={this.state.modalOpen}
              onRequestClose={this.closeModal}
              style={modalStyle}
              clientId={this.props.queryFilter.clientId}
            />
          );
        case 'AddUserToProfitCenter':
          return (
            <AddUserToProfitCenterModal
              isOpen={this.state.modalOpen}
              onRequestClose={this.closeModal}
              style={modalStyle}
              profitCenterId={this.props.queryFilter.profitCenterId}
            />
          );
        default:
          return (
            <Modal
              isOpen={this.state.modalOpen}
              onRequestClose={this.closeModal}
              ariaHideApp={false}
              style={modalStyle}
            >
              <div>Please press the button</div>
              <button
                type="button"
                className="button-submit blue-button"
                onClick={this.handleCreate}
              >
                Click Me
              </button>
            </Modal>
          );
      }
    })();

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
        {modal}
      </div>
    );
  }

  private fetch() {
    if (!this.url) {
      return this.setState({ entities: [] });
    }

    getData(this.url, this.props.queryFilter)
    .then((response) => {
      if (this.props.selectedDataSource) {
        this.setState({
          entities: this.props.selectedDataSource.processInfo(response),
        });
      }
    });
  }

  private setFilterText(filterText: string) {
    this.setState({ filterText });
  }

  private addAction() {
    this.setState({
      modalOpen: true,
    });
  }

  private closeModal() {
    this.setState({
      modalOpen: false,
    });
  }

  private handleCreate() {
    postData(this.createUrl)
    .then(() => {
      throw new Error('Not implemented');
    });
  }
}
