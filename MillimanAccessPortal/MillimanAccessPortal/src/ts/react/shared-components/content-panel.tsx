import '../../../scss/react/shared-components/content-panel.scss';

import { isEqual } from 'lodash';
import * as React from 'react';
import * as Modal from 'react-modal';

import { getData, postData } from '../../shared';
import { BasicNode } from '../../view-models/content-publishing';
import {
  ClientInfo, ClientInfoWithDepth, EntityInfo, EntityInfoCollection, isClientInfo, isClientInfoTree,
  isUserInfo,
} from '../system-admin/interfaces';
import { AddUserToClientModal } from '../system-admin/modals/add-user-to-client';
import { AddUserToProfitCenterModal } from '../system-admin/modals/add-user-to-profit-center';
import { CreateProfitCenterModal } from '../system-admin/modals/create-profit-center';
import { CreateUserModal } from '../system-admin/modals/create-user';
import { ActionIcon } from './action-icon';
import { Card } from './card';
import { ColumnSelector } from './column-selector';
import { Entity, EntityHelper } from './entity';
import { Filter } from './filter';
import { DataSource, QueryFilter, Structure } from './interfaces';

export interface ContentPanelAttributes {
  filterText: string;
  onFilterTextChange: (text: string) => void;
  modalOpen: boolean;
  onModalOpen: () => void;
  onModalClose: () => void;
}
export interface ContentPanelProps extends ContentPanelAttributes {
  dataSources: Array<DataSource<Entity>>;
  setSelectedDataSource: (sourceName: string) => void;
  selectedDataSource: DataSource<Entity>;
  setSelectedCard: (cardId: string) => void;
  selectedCard: string;
  queryFilter: QueryFilter;
  entities: EntityInfoCollection;
}

export class ContentPanel extends React.Component<ContentPanelProps> {
  public render() {

    const filterPlaceholder = this.props.selectedDataSource.displayName
      ? `Filter ${this.props.selectedDataSource.displayName}...`
      : '';
    const actionIcon = this.props.selectedDataSource.createAction
      && (
        <ActionIcon
          title={'Add'}
          action={this.props.onModalOpen}
          icon={'add'}
        />
      );

    const modal = (() => {
      switch (this.props.selectedDataSource.createAction) {
        case 'CreateUser':
          return (
            <CreateUserModal
              isOpen={this.props.modalOpen}
              onRequestClose={this.props.onModalClose}
            />
          );
        case 'CreateProfitCenter':
          return (
            <CreateProfitCenterModal
              isOpen={this.props.modalOpen}
              onRequestClose={this.props.onModalClose}
            />
          );
        case 'AddUserToClient':
          return (
            <AddUserToClientModal
              isOpen={this.props.modalOpen}
              onRequestClose={this.props.onModalClose}
              clientId={this.props.queryFilter.clientId}
            />
          );
        case 'AddUserToProfitCenter':
          return (
            <AddUserToProfitCenterModal
              isOpen={this.props.modalOpen}
              onRequestClose={this.props.onModalClose}
              profitCenterId={this.props.queryFilter.profitCenterId}
            />
          );
        default:
          return null;
      }
    })();

    return (
      <div
        className="admin-panel-container flex-item-12-12 flex-item-for-tablet-up-4-12 flex-item-for-desktop-up-3-12"
      >
        <ColumnSelector
          columns={this.props.dataSources.map(({name: id, displayName: name}) => ({id, name}))}
          onColumnSelect={this.props.setSelectedDataSource}
          selectedColumn={{id: this.props.selectedDataSource.name, name: this.props.selectedDataSource.displayName}}
        />
        <div className="admin-panel-list">
          <div className="admin-panel-toolbar">
            <Filter
              placeholderText={filterPlaceholder}
              setFilterText={this.props.onFilterTextChange}
              filterText={this.props.filterText}
            />
            <div className="admin-panel-action-icons-container">
              {actionIcon}
            </div>
          </div>
          <div className="admin-panel-content-container">
            <ul className="admin-panel-content">
              {this.renderCards()}
            </ul>
          </div>
        </div>
        {modal}
      </div>
    );
  }

  private renderCards() {
    if (this.props.entities === null) {
      return <div>Loading...</div>;
    }

    let filteredCards: EntityInfo[];
    if (isClientInfoTree(this.props.entities)) {
      // flatten basic tree into an array
      const traverse = (node: BasicNode<ClientInfo>, list: ClientInfoWithDepth[] = [], depth = 0) => {
        if (node.Value !== null) {
          const clientDepth = {
            ...node.Value,
            depth,
          };
          list.push(clientDepth);
        }
        if (node.Children.length) {
          node.Children.forEach((child) => list = traverse(child, list, depth + 1));
        }
        return list;
      };
      filteredCards = traverse(this.props.entities.Root);
    } else {
      filteredCards = this.props.entities;
    }

    // apply filter
    filteredCards = filteredCards.filter((entity) =>
        EntityHelper.applyFilter(entity, this.props.filterText));

    if (filteredCards.length === 0) {
      return <div>No {this.props.selectedDataSource.displayName.toLowerCase()} found.</div>;
    } else if (isClientInfo(filteredCards[0])) {
      const rootIndices = [];
      filteredCards.forEach((entity: ClientInfoWithDepth, i) => {
        if (!entity.ParentId) {
          rootIndices.push(i);
        }
      });
      const cardGroups = rootIndices.map((_, i) =>
        filteredCards.slice(rootIndices[i], rootIndices[i + 1]));
      return cardGroups.map((group, i) => {
        const groupCards = group.map((entity: ClientInfoWithDepth) => (
          <li key={entity.Id}>
            <Card
              entity={entity}
              selected={entity.Id === this.props.selectedCard}
              onSelect={() => this.props.setSelectedCard(entity.Id)}
              indentation={entity.depth}
            />
          </li>
        ));
        if (i + 1 !== cardGroups.length) {
          groupCards.push((<div key="hr-{i}" className="hr" />));
        }
        return groupCards;
      });
    } else {
      return filteredCards.map((entity) => (
        <li key={entity.Id}>
          <Card
            entity={entity}
            selected={entity.Id === this.props.selectedCard}
            onSelect={() => this.props.setSelectedCard(entity.Id)}
            activated={isUserInfo(entity) ? entity.Activated : null}
            resetButton={isUserInfo(entity)}
          />
        </li>
      ));
    }
  }
}
