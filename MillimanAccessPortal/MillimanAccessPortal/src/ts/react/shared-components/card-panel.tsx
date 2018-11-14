import '../../../scss/react/shared-components/card-panel.scss';

import * as React from 'react';

import { BasicNode } from '../../view-models/content-publishing';
import {
  ClientInfo, ClientInfoWithDepth, EntityInfo, EntityInfoCollection, isClientInfo, isClientInfoTree,
  isProfitCenterInfo, isUserInfo,
} from '../system-admin/interfaces';
import { AddUserToClientModal } from '../system-admin/modals/add-user-to-client';
import { AddUserToProfitCenterModal } from '../system-admin/modals/add-user-to-profit-center';
import { CardModal } from '../system-admin/modals/card-modal';
import { CreateProfitCenterModal } from '../system-admin/modals/create-profit-center';
import { CreateUserModal } from '../system-admin/modals/create-user';
import { ActionIcon } from './action-icon';
import { Card, CardAttributes } from './card/card';
import { ColumnSelector, ColumnSelectorProps } from './column-selector';
import { EntityHelper } from './entity';
import { Filter } from './filter';
import { Guid, QueryFilter } from './interfaces';

export interface CardPanelAttributes {
  filterText: string;
  onFilterTextChange: (text: string) => void;
  modalOpen: boolean;
  onModalOpen: () => void;
  onModalClose: () => void;
  createAction: string;
}
export interface CardPanelProps<TEntity> extends CardPanelAttributes {
  panelHeader: PanelHeader;
  onExpandedToggled: (id: Guid) => void;
  cards: {
    [id: string]: CardAttributes;
  };
  onCardSelect: (id: Guid) => void;
  selectedCard: string;
  queryFilter: QueryFilter;
  entities: TEntity[];
  renderEntity: (entity: TEntity, key: number) => JSX.Element;
  onProfitCenterModalOpen: (id: Guid) => void;
  onProfitCenterModalClose: (id: Guid) => void;
  onSendReset: (email: string) => void;
  onProfitCenterDelete: (id: Guid) => void;
  onProfitCenterUserRemove: (userId: Guid, profitCenterId: Guid) => void;
  onClientUserRemove: (userId: Guid, clientId: Guid) => void;
}

export type PanelHeader = string | ColumnSelectorProps;

export class CardPanel<TEntity> extends React.Component<CardPanelProps<TEntity>> {
  public render() {

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

    const { entities, renderEntity } = this.props;
    return (
      <div
        className="admin-panel-container flex-item-12-12 flex-item-for-tablet-up-4-12 flex-item-for-desktop-up-3-12"
      >
        {this.renderColumnSelector()}
        <div className="admin-panel-list">
          <div className="admin-panel-toolbar">
            <Filter
              placeholderText={''}
              setFilterText={this.props.onFilterTextChange}
              filterText={this.props.filterText}
            />
            <div className="admin-panel-action-icons-container">
              {actionIcon}
            </div>
          </div>
          <div className="admin-panel-content-container">
            <ul className="admin-panel-content">
              {entities.map((entity, i) => renderEntity(entity, i))}
            </ul>
          </div>
        </div>
        {modal}
      </div>
    );
  }

  private renderColumnSelector() {
    const { panelHeader } = this.props;
    if (typeof(panelHeader) === 'string') {
      return (
        <h3 className="admin-panel-header">{this.props.panelHeader}</h3>
      );
    } else if (panelHeader.columns.length) {
      return (
        <ColumnSelector
          columns={panelHeader.columns}
          onColumnSelect={panelHeader.onColumnSelect}
          selectedColumn={panelHeader.selectedColumn}
        />
      );
    }
    return null;
  }

  /*
  private renderCards() {
    if (this.props.entities === null) {
      return <div>Loading...</div>;
    }

    let filteredCards: EntityInfo[];
    if (isClientInfoTree(this.props.entities)) {
      // flatten basic tree into an array
      const traverse = (node: BasicNode<ClientInfo>, list: ClientInfoWithDepth[] = [], depth = 0) => {
        if (node.value !== null) {
          const clientDepth = {
            ...node.value,
            depth,
          };
          list.push(clientDepth);
        }
        if (node.children.length) {
          node.children.forEach((child) => list = traverse(child, list, depth + 1));
        }
        return list;
      };
      filteredCards = traverse(this.props.entities.root);
    } else {
      filteredCards = this.props.entities;
    }

    // apply filter
    filteredCards = filteredCards.filter((entity) =>
        EntityHelper.applyFilter(entity, this.props.filterText));

    if (filteredCards.length === 0) {
      const { panelHeader } = this.props;
      const panelTitle = typeof(panelHeader) === 'string'
        ? panelHeader
        : panelHeader.selectedColumn
          ? panelHeader.selectedColumn.name
          : null;
      return panelTitle && <div>No {panelTitle.toLowerCase()} found.</div>;
    } else if (isClientInfo(filteredCards[0])) {
      const rootIndices = [];
      filteredCards.forEach((entity: ClientInfoWithDepth, i) => {
        if (!entity.parentId) {
          rootIndices.push(i);
        }
      });
      const cardGroups = rootIndices.map((_, i) =>
        filteredCards.slice(rootIndices[i], rootIndices[i + 1]));
      return cardGroups.map((group, i) => {
        const groupCards = group
          .map((entity: ClientInfoWithDepth) => {
            const card = this.props.cards[entity.id];
            return (
              <li key={entity.id}>
                <Card
                  entity={entity}
                  selected={entity.id === this.props.selectedCard}
                  onSelect={() => this.props.onCardSelect(entity.id)}
                  expanded={card && card.expanded || false}
                  onExpandedToggled={() => this.props.onExpandedToggled(entity.id)}
                  indentation={entity.depth}
                  onProfitCenterModalOpen={() => null}
                  cardStats={this.props.cardStats}
                />
              </li>
            );
          });
        if (i + 1 !== cardGroups.length) {
          groupCards.push((<div key="hr-{i}" className="hr" />));
        }
        return groupCards;
      });
    } else if (isProfitCenterInfo(filteredCards[0])) {
      return filteredCards
        .map((entity) => {
          const card = this.props.cards[entity.id];
          return (
            <li key={entity.id}>
              <CardModal
                isOpen={card && card.profitCenterModalOpen || false}
                onRequestClose={() => this.props.onProfitCenterModalClose(entity.id)}
                render={() => (
                  <Card
                    entity={entity}
                    selected={entity.id === this.props.selectedCard}
                    onSelect={() => this.props.onCardSelect(entity.id)}
                    expanded={card && card.expanded || false}
                    onExpandedToggled={() => this.props.onExpandedToggled(entity.id)}
                    activated={isUserInfo(entity) ? entity.activated : null}
                    resetButton={isUserInfo(entity)}
                    onSendReset={this.getOnSendReset(entity)}
                    onProfitCenterModalOpen={() => this.props.onProfitCenterModalOpen(entity.id)}
                    onProfitCenterDelete={this.getOnProfitCenterDelete(entity)}
                    onProfitCenterUserRemove={this.getOnProfitCenterUserRemove(entity)}
                    onClientUserRemove={this.getOnClientUserRemove(entity)}
                    cardStats={this.props.cardStats}
                    status={(entity as any).status}
                  />
                )}
              />
            </li>
          );
        });
    } else {
      return filteredCards
        .map((entity) => {
          const card = this.props.cards[entity.id];
          return (
            <li key={entity.id}>
              <Card
                entity={entity}
                selected={entity.id === this.props.selectedCard}
                onSelect={(card && card.disabled) ? () => null : () => this.props.onCardSelect(entity.id)}
                expanded={card && card.expanded || false}
                onExpandedToggled={() => this.props.onExpandedToggled(entity.id)}
                activated={isUserInfo(entity) ? entity.activated : null}
                disabled={card && card.disabled || false}
                resetButton={isUserInfo(entity)}
                onProfitCenterModalOpen={() => null}
                onSendReset={this.getOnSendReset(entity)}
                onProfitCenterDelete={this.getOnProfitCenterDelete(entity)}
                onProfitCenterUserRemove={this.getOnProfitCenterUserRemove(entity)}
                onClientUserRemove={this.getOnClientUserRemove(entity)}
                cardStats={this.props.cardStats}
                status={(entity as any).status}
                suspended={(entity as any).isSuspended}
              />
            </li>
          );
        });
    }
  }
  */

  private getOnSendReset = (entity: EntityInfo) => isUserInfo(entity)
      ? () => this.props.onSendReset(entity.email)
      : null

  private getOnProfitCenterDelete = (entity: EntityInfo) => isProfitCenterInfo(entity)
      ? () => this.props.onProfitCenterDelete(entity.id)
      : null

  private getOnProfitCenterUserRemove = (entity: EntityInfo) => (isUserInfo(entity) && entity.profitCenterId !== null)
      ? () => this.props.onProfitCenterUserRemove(entity.id, entity.profitCenterId)
      : null

  private getOnClientUserRemove = (entity: EntityInfo) => (isUserInfo(entity) && entity.clientId !== null)
      ? () => this.props.onClientUserRemove(entity.id, entity.clientId)
      : null
}
