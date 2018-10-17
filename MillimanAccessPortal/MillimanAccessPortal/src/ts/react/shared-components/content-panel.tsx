import '../../../scss/react/shared-components/content-panel.scss';

import * as React from 'react';

import { BasicNode } from '../../view-models/content-publishing';
import {
  ClientInfo, ClientInfoWithDepth, EntityInfo, EntityInfoCollection, isClientInfo, isClientInfoTree,
  isProfitCenterInfo, isUserInfo,
} from '../system-admin/interfaces';
import { AddUserToClientModal } from '../system-admin/modals/add-user-to-client';
import { AddUserToProfitCenterModal } from '../system-admin/modals/add-user-to-profit-center';
import { CreateProfitCenterModal } from '../system-admin/modals/create-profit-center';
import { CreateUserModal } from '../system-admin/modals/create-user';
import { ActionIcon } from './action-icon';
import { Card, CardAttributes } from './card';
import { ColumnIndicator, ColumnSelector } from './column-selector';
import { EntityHelper } from './entity';
import { Filter } from './filter';
import { Guid, QueryFilter } from './interfaces';

export interface ContentPanelAttributes {
  filterText: string;
  onFilterTextChange: (text: string) => void;
  modalOpen: boolean;
  onModalOpen: () => void;
  onModalClose: () => void;
  createAction: string;
}
export interface ContentPanelProps extends ContentPanelAttributes {
  columns: ColumnIndicator[];
  onColumnSelect: (id: string) => void;
  selectedColumn: ColumnIndicator;
  onExpandedToggled: (id: Guid) => void;
  cards: {
    [id: string]: CardAttributes;
  };
  onCardSelect: (id: Guid) => void;
  selectedCard: string;
  queryFilter: QueryFilter;
  entities: EntityInfoCollection;
  onProfitCenterModalOpen: (id: Guid) => void;
  onProfitCenterModalClose: (id: Guid) => void;
  onSendReset: (email: string) => void;
  onProfitCenterDelete: (id: Guid) => void;
  onProfitCenterUserRemove: (userId: Guid, profitCenterId: Guid) => void;
}

export class ContentPanel extends React.Component<ContentPanelProps> {
  public render() {

    const filterPlaceholder = this.props.selectedColumn
      ? `Filter ${this.props.selectedColumn.name}...`
      : '';
    const actionIcon = this.props.createAction
      && (
        <ActionIcon
          title={'Add'}
          action={this.props.onModalOpen}
          icon={'add'}
        />
      );

    const modal = (() => {
      switch (this.props.createAction) {
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
          columns={this.props.columns}
          onColumnSelect={this.props.onColumnSelect}
          selectedColumn={this.props.selectedColumn}
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
      return this.props.selectedColumn
        ? <div>No {this.props.selectedColumn.name.toLowerCase()} found.</div>
        : null;
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
              onSelect={() => this.props.onCardSelect(entity.Id)}
              expanded={this.props.cards[entity.Id].expanded}
              onExpandedToggled={() => this.props.onExpandedToggled(entity.Id)}
              indentation={entity.depth}
              profitCenterModalOpen={this.props.cards[entity.Id].profitCenterModalOpen}
              onProfitCenterModalOpen={() => this.props.onProfitCenterModalOpen(entity.Id)}
              onProfitCenterModalClose={() => this.props.onProfitCenterModalClose(entity.Id)}
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
            onSelect={() => this.props.onCardSelect(entity.Id)}
            expanded={this.props.cards[entity.Id].expanded}
            onExpandedToggled={() => this.props.onExpandedToggled(entity.Id)}
            activated={isUserInfo(entity) ? entity.Activated : null}
            resetButton={isUserInfo(entity)}
            profitCenterModalOpen={this.props.cards[entity.Id].profitCenterModalOpen}
            onProfitCenterModalOpen={() => this.props.onProfitCenterModalOpen(entity.Id)}
            onProfitCenterModalClose={() => this.props.onProfitCenterModalClose(entity.Id)}
            onSendReset={this.getOnSendReset(entity)}
            onProfitCenterDelete={this.getOnProfitCenterDelete(entity)}
            onProfitCenterUserRemove={this.getOnProfitCenterUserRemove(entity)}
          />
        </li>
      ));
    }
  }

  private getOnSendReset = (entity: EntityInfo) => isUserInfo(entity)
      ? () => this.props.onSendReset(entity.Email)
      : null

  private getOnProfitCenterDelete = (entity: EntityInfo) => isProfitCenterInfo(entity)
      ? () => this.props.onProfitCenterDelete(entity.Id)
      : null

  private getOnProfitCenterUserRemove = (entity: EntityInfo) => (isUserInfo(entity) && entity.ProfitCenterId !== null)
      ? () => this.props.onProfitCenterUserRemove(entity.Id, entity.ProfitCenterId)
      : null
}
