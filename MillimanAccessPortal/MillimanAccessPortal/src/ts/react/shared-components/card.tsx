import '../../../images/delete.svg';
import '../../../images/edit.svg';
import '../../../images/email.svg';
import '../../../images/expand-card.svg';
import '../../../images/remove-circle.svg';

import * as React from 'react';

import { ContentPublicationRequest, ContentReductionTask, Guid } from '../models';
import {
  EntityInfo, isClientInfo, isProfitCenterInfo, isRootContentItemInfo, isUserInfo,
} from '../system-admin/interfaces';
import { UpdateProfitCenterModal } from '../system-admin/modals/update-profit-center';
import CardButton, { CardButtonColor } from './card-button';
import { CardStat, CardStatIcon } from './card-stat';

export interface CardStatPropEvaluator {
  name: string;
  value: (id: Guid) => number;
  icon: CardStatIcon;
}

export interface CardAttributes {
  expanded: boolean;
  profitCenterModalOpen: boolean;
}

export interface CardProps {
  entity: EntityInfo;
  selected: boolean;
  onSelect: () => void;
  expanded: boolean;
  onExpandedToggled: () => void;
  resetButton?: boolean;
  resetButtonText?: string;
  activated?: boolean;
  suspended?: boolean;
  indentation?: number;
  profitCenterModalOpen: boolean;
  onProfitCenterModalOpen: () => void;
  onProfitCenterModalClose: () => void;
  onSendReset?: () => void;
  onProfitCenterDelete?: () => void;
  onProfitCenterUserRemove?: () => void;
  onClientUserRemove?: () => void;
  cardStats?: CardStatPropEvaluator[];
  status?: ContentPublicationRequest | ContentReductionTask;
}

export class Card extends React.Component<CardProps> {
  private indentClasses: { [indent: number]: string; } = {
    1: 'card-100',
    2: 'card-90',
    3: 'card-80',
  };

  public render() {
    const cardClass = 'card-container'
      + (this.props.indentation ? ` ${this.indentClasses[this.props.indentation] || this.indentClasses[1]}` : '');
    const cardBodyClass = 'card-body-container'
      + (this.props.selected ? ' selected' : '')
      + (this.props.suspended ? ' suspended' : '');
    return (
      <>
        <div className={cardClass} onClick={this.props.onSelect}>
          <div className={cardBodyClass}>
            <div className="card-body-main-container">
              {this.renderPrimaryContainer()}
              {this.renderStats()}
              {this.renderSideButtons()}
            </div>
            {this.renderExpansion()}
          </div>
          {this.renderStatus()}
        </div>
        {this.renderProfitCenterModal()}
      </>
    );
  }

  private renderPrimaryContainer() {
    return (
      <div className="card-body-primary-container">
        {this.renderPrimaryText()}
        {this.renderSecondaryText()}
      </div>
    );
  }

  private renderPrimaryText() {
    const { entity } = this.props;
    let text: string;
    if (isUserInfo(entity)) {
      const { firstName, lastName } = entity;
      text = this.props.activated ? `${firstName} ${lastName}` : '(Unactivated)';
    } else {
      text = entity.name;
    }
    return (
      <h2 className="card-body-primary-text">
        {`${text}${this.props.suspended ? ' (Suspended)' : ''}`}
      </h2>
    );
  }

  private renderSecondaryText() {
    const { entity } = this.props;
    let text: string;
    if (isUserInfo(entity)) {
      text = entity.userName;
    } else if (isRootContentItemInfo(entity)) {
      text = entity.clientName;
    } else if (isClientInfo(entity) || isProfitCenterInfo(entity)) {
      text = entity.code;
    } else {
      text = '';
    }
    return <p className="card-body-secondary-text">{text}</p>;
  }

  private renderStats() {
    const { entity } = this.props;
    const cardStats: CardStatPropEvaluator[] = [];
    if (this.props.cardStats) {
      cardStats.push(...this.props.cardStats);
    } else if (isUserInfo(entity)) {
      const { clientCount: clients, rootContentItemCount: items } = entity;
      if (clients !== null) {
        cardStats.push({
          name: 'Clients',
          value: () => clients,
          icon: 'client',
        });
      }
      if (items !== null) {
        cardStats.push({
          name: 'Reports',
          value: () => items,
          icon: 'reports',
        });
      }
    } else if (isClientInfo(entity)) {
      const { userCount: users, rootContentItemCount: items } = entity;
      if (users !== null) {
        cardStats.push({
          name: 'Users',
          value: () => users,
          icon: 'user',
        });
      }
      if (items !== null) {
        cardStats.push({
          name: 'Reports',
          value: () => items,
          icon: 'reports',
        });
      }
    } else if (isProfitCenterInfo(entity)) {
      const { userCount: users, clientCount: clients } = entity;
      if (users !== null) {
        cardStats.push({
          name: 'Authorized users',
          value: () => users,
          icon: 'user',
        });
      }
      if (clients !== null) {
        cardStats.push({
          name: 'Clients',
          value: () => clients,
          icon: 'client',
        });
      }
    } else if (isRootContentItemInfo(entity)) {
      const { userCount: users, selectionGroupCount: groups } = entity;
      if (users !== null) {
        cardStats.push({
          name: 'Users',
          value: () => users,
          icon: 'user',
        });
      }
      if (groups !== null) {
        cardStats.push({
          name: 'Selection Groups',
          value: () => groups,
          icon: 'group',
        });
      }
    }
    if (cardStats.length === 0) {
      return null;
    }

    const stats = cardStats.map(({ name, value, icon }, i) => (
      <CardStat
        key={i}
        name={name}
        value={value(entity.id)}
        icon={icon}
      />
    ));
    return <div className="card-stats-container">{stats}</div>;
  }

  private renderSideButtons() {
    const { entity } = this.props;
    if (isUserInfo(entity) || isProfitCenterInfo(entity)) {
      const buttons: JSX.Element[] = [];
      if (isUserInfo(entity)) {
        buttons.push((
          <CardButton
            key={1}
            color={CardButtonColor.BLUE}
            tooltip={this.props.activated ? 'Send password reset email' : 'Resend account activation email'}
            onClick={this.onSendReset}
            icon={'email'}
          />
        ));
        if (this.props.onProfitCenterUserRemove) {
          buttons.push((
            <CardButton
              key={2}
              color={CardButtonColor.RED}
              tooltip={'Remove from profit center'}
              onClick={this.onProfitCenterUserRemove}
              icon={'remove-circle'}
            />
          ));
        } else if (this.props.onClientUserRemove) {
          buttons.push((
            <CardButton
              key={2}
              color={CardButtonColor.RED}
              tooltip={'Remove from client'}
              onClick={this.onClientUserRemove}
              icon={'remove-circle'}
            />
          ));
        }
      } else {
        buttons.push((
          <CardButton
            key={1}
            color={CardButtonColor.RED}
            tooltip={'Delete profit center'}
            onClick={this.onProfitCenterDelete}
            icon={'delete'}
          />
        ));
        buttons.push((
          <CardButton
            key={2}
            color={CardButtonColor.BLUE}
            tooltip={'Update profit center'}
            onClick={this.onProfitCenterModalOpen}
            icon={'edit'}
          />
        ));
      }
      return <div className="card-button-side-container">{buttons}</div>;
    }
    return null;
  }

  private renderExpansion() {
    const { entity } = this.props;
    if (isUserInfo(entity) || isRootContentItemInfo(entity)) {
      let title: string;
      if (isUserInfo(entity)) {
        title = 'Content Items';
      } else {
        title = 'Members';
      }
      const expansionList = this.renderExpansionList();

      return expansionList && (
        <div className={'card-expansion-container' + (this.props.expanded ? ' maximized' : '')}>
          <h4 className="card-expansion-category-label">{title}</h4>
          {expansionList}
          <div className="card-button-bottom-container">
            <div
              className="card-button-background card-button-expansion"
              onClick={this.onExpandedToggled}
            >
              <svg className="card-button-icon">
                <use xlinkHref="#expand-card" />
              </svg>
            </div>
          </div>
        </div>
      );
    }
    return null;
  }

  private renderExpansionList() {
    const { entity } = this.props;
    if (isUserInfo(entity) || isRootContentItemInfo(entity)) {
      let icon: string;
      let textList: Array<{
        primaryText: string;
        secondaryText: string;
      }>;
      if (isUserInfo(entity)) {
        if (entity.rootContentItems && entity.rootContentItems.length) {
          icon = 'reports';
          textList = entity.rootContentItems.map((itemInfo) => ({
            primaryText: itemInfo.name,
            secondaryText: itemInfo.clientName,
          }));
        } else {
          return null;
        }
      } else {
        if (entity.users && entity.users.length) {
          icon = 'user';
          textList = entity.users.map((userInfo) => ({
            primaryText: `${userInfo.firstName} ${userInfo.lastName}`,
            secondaryText: userInfo.userName,
          }));
        } else {
          return null;
        }
      }
      const list = textList.map(({ primaryText, secondaryText }, i) => (
        <li key={i}>
          <span className="detail-item-user">
            <div className="detail-item-user-icon">
              <svg className="card-user-icon">
                <use xlinkHref={`#${icon}`} />
              </svg>
            </div>
            <div className="detail-item-user-name">
              <h4 className="first-last">{primaryText}</h4>
              <span className="user-name">{secondaryText}</span>
            </div>
          </span>
        </li>
      ));
      return <ul>{list}</ul>;
    }
    return null;
  }

  private renderStatus() {
    return this.props.status
    ? (
      <div className="card-status-container status-30">
        <span className="status-top"><strong>Processed</strong></span>
        <span className="status-bot">Initiated by J. Sweeney 2 days ago</span>
      </div>
    )
    : null;
  }

  private renderProfitCenterModal() {
    return isProfitCenterInfo(this.props.entity)
      ? (
        <UpdateProfitCenterModal
          isOpen={this.props.profitCenterModalOpen}
          onRequestClose={this.props.onProfitCenterModalClose}
          profitCenterId={this.props.entity.id}
        />
      )
      : null;
  }

  private onProfitCenterModalOpen = (event: React.MouseEvent<HTMLDivElement>) => {
    event.stopPropagation();
    this.props.onProfitCenterModalOpen();
  }

  private onSendReset = (event: React.MouseEvent<HTMLDivElement>) => {
    event.stopPropagation();
    const { onSendReset } = this.props;
    if (onSendReset) {
      onSendReset();
    }
  }

  private onProfitCenterDelete = (event: React.MouseEvent<HTMLDivElement>) => {
    event.stopPropagation();
    const { onProfitCenterDelete } = this.props;
    if (onProfitCenterDelete) {
      onProfitCenterDelete();
    }
  }

  private onProfitCenterUserRemove = (event: React.MouseEvent<HTMLDivElement>) => {
    event.stopPropagation();
    const { onProfitCenterUserRemove } = this.props;
    if (onProfitCenterUserRemove) {
      onProfitCenterUserRemove();
    }
  }

  private onClientUserRemove = (event: React.MouseEvent<HTMLDivElement>) => {
    event.stopPropagation();
    const { onClientUserRemove } = this.props;
    if (onClientUserRemove) {
      onClientUserRemove();
    }
  }

  private onExpandedToggled = (event: React.MouseEvent<HTMLDivElement>) => {
    event.stopPropagation();
    this.props.onExpandedToggled();
  }
}
