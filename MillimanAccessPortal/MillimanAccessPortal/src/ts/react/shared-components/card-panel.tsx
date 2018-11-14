import '../../../scss/react/shared-components/card-panel.scss';

import * as React from 'react';

import { AddUserToClientModal } from '../system-admin/modals/add-user-to-client';
import { AddUserToProfitCenterModal } from '../system-admin/modals/add-user-to-profit-center';
import { CreateProfitCenterModal } from '../system-admin/modals/create-profit-center';
import { CreateUserModal } from '../system-admin/modals/create-user';
import { ActionIcon } from './action-icon';
import { CardAttributes } from './card/card';
import { ColumnSelector, ColumnSelectorProps } from './column-selector';
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
}
