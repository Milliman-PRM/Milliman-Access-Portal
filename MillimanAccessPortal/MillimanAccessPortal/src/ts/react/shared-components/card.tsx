import '../../../images/delete.svg';
import '../../../images/edit.svg';
import '../../../images/email.svg';
import '../../../images/expand-card.svg';
import '../../../images/remove-circle.svg';

import * as React from 'react';

import { postData } from '../../shared';
import { UpdateProfitCenterModal } from '../system-admin/modals/update-profit-center';
import { Entity } from './entity';

export interface CardProps extends Entity {
  selected: boolean;
  setSelected: () => void;
  resetButton?: boolean;
  resetButtonText?: string;
  activated?: boolean;
  sublistInfo: {
    title: string;
    icon: string;
    emptyText: string;
  };
}

interface CardState {
  expanded: boolean;
  updateProfitCenterModalOpen: boolean;
}

export class Card extends React.Component<CardProps, CardState> {
  private indentClasses: { [indent: number]: string; } = {
    1: 'card-100',
    2: 'card-90',
    3: 'card-80',
  };

  public constructor(props) {
    super(props);

    this.state = {
      expanded: false,
      updateProfitCenterModalOpen: false,
    };

    this.sendPasswordReset = this.sendPasswordReset.bind(this);
    this.deleteAsProfitCenter = this.deleteAsProfitCenter.bind(this);
    this.editAsProfitCenter = this.editAsProfitCenter.bind(this);
    this.removeAsUser = this.removeAsUser.bind(this);
    this.toggleExpansion = this.toggleExpansion.bind(this);
    this.openUpdateModal = this.openUpdateModal.bind(this);
    this.closeModal = this.closeModal.bind(this);
    this.handleUpdate = this.handleUpdate.bind(this);
  }

  public render() {
    const stats = [this.props.primaryStat, this.props.secondaryStat]
      .filter((stat) => stat)
      .map((stat) => (
        <div
          key={stat.icon}
          className="card-stat-container"
          title={stat.name}
        >
          <svg className="card-stat-icon">
            <use xlinkHref={`#${stat.icon}`} />
          </svg>
          <h4 className="card-stat-value">{stat.value}</h4>
        </div>
      ));
    const resetButton = this.props.resetButton
      ? (
        <div
          className="card-button-background card-button-blue"
          title={this.props.activated ? 'Send password reset email' : 'Resend account activation email'}
          onClick={this.sendPasswordReset}
        >
          <svg className="card-button-icon">
            <use xlinkHref="#email" />
          </svg>
        </div>
      )
      : null;
    const deleteAsProfitCenterButton = this.props.isProfitCenter
      ? (
        <div
          className="card-button-background card-button-red"
          title="Delete profit center"
          onClick={this.deleteAsProfitCenter}
        >
          <svg className="card-button-icon">
            <use xlinkHref="#delete" />
          </svg>
        </div>
      )
      : null;
    const editAsProfitCenterButton = this.props.isProfitCenter
      ? (
        <div
          className="card-button-background card-button-blue"
          title="Update profit center"
          onClick={this.editAsProfitCenter}
        >
          <svg className="card-button-icon">
            <use xlinkHref="#edit" />
          </svg>
        </div>
      )
      : null;
    const removeAsUserButton = this.props.isUserInProfitCenter
      ? (
        <div
          className="card-button-background card-button-red"
          title="Remove from profit center"
          onClick={this.removeAsUser}
        >
          <svg className="card-button-icon">
            <use xlinkHref="#remove-circle" />
          </svg>
        </div>
      )
      : null;
    const detailList = this.props.sublist && this.props.sublist.map((subitem, i) => (
      <li
        key={i}
      >
        <span className="detail-item-user">
          <div className="detail-item-user-icon">
            <svg className="card-user-icon">
              <use xlinkHref={`#${this.props.sublistInfo.icon}`} />
            </svg>
          </div>
          <div className="detail-item-user-name">
            <h4 className="first-last">{subitem.primaryText}</h4>
            <span className="user-name">{subitem.secondaryText}</span>
          </div>
        </span>
      </li>
    ));
    const expansion = this.props.sublist && this.props.sublist.length
      ? (
        <div className={'card-expansion-container' + (this.state.expanded ? ' maximized' : '')}>
          <h4 className="card-expansion-category-label">{this.props.sublistInfo.title}</h4>
          <ul>
            {detailList}
          </ul>
          <div className="card-button-bottom-container">
            <div
              className="card-button-background card-button-expansion"
              onClick={this.toggleExpansion}
            >
              <svg className="card-button-icon">
                <use xlinkHref="#expand-card" />
              </svg>
            </div>
          </div>
        </div>
      )
      : null;
    const additionalClasses = [
      this.props.selected ? ' selected' : '',
      this.props.suspended ? ' suspended' : '',
    ].join('');
    const updateProfitCenterModal = this.props.isProfitCenter
      ? (
        <UpdateProfitCenterModal
          isOpen={this.state.updateProfitCenterModalOpen}
          onRequestClose={this.closeModal}
          profitCenterId={this.props.id}
        />
      )
      : null;
    return (
      <>
        <div
          className={`card-container ${this.indentClasses[this.props.indent] || ''}`}
          onClick={this.props.setSelected}
        >
          <div
            className={`card-body-container${additionalClasses}`}
          >
            <div className="card-body-main-container">
              <div className="card-body-primary-container">
                <h2 className="card-body-primary-text">
                  {this.props.activated ? this.props.primaryText : '(Unactivated)'}
                  {this.props.suspended ? ' (Suspended)' : ''}
                </h2>
                <p className="card-body-secondary-text">
                  {this.props.secondaryText}
                </p>
              </div>
              <div className="card-stats-container">
                {stats}
              </div>
              <div className="card-button-side-container">
                {deleteAsProfitCenterButton}
                {editAsProfitCenterButton}
                {removeAsUserButton}
                {resetButton}
              </div>
            </div>
            {expansion}
          </div>
        </div>
        {updateProfitCenterModal}
      </>
    );
  }

  private sendPasswordReset(event: React.MouseEvent<HTMLDivElement>) {
    event.stopPropagation();
    postData('Account/ForgotPassword', {
      Email: this.props.email,
    })
    .then(() => {
      alert('Password reset email sent.');
    });
  }

  private deleteAsProfitCenter(event: React.MouseEvent<HTMLDivElement>) {
    event.stopPropagation();
    postData('SystemAdmin/DeleteProfitCenter', {
      profitCenterId: this.props.id,
    })
    .then(() => {
      alert('Profit center deleted.');
    });
  }

  private editAsProfitCenter(event: React.MouseEvent<HTMLDivElement>) {
    event.stopPropagation();
    this.setState({
      updateProfitCenterModalOpen: true,
    });
  }

  private removeAsUser(event: React.MouseEvent<HTMLDivElement>) {
    event.stopPropagation();
    postData('SystemAdmin/RemoveUserFromProfitCenter', {
      userId: this.props.id,
      profitCenterId: this.props.isUserInProfitCenter,
    })
    .then(() => {
      alert('User removed from profit center.');
    });
  }

  private toggleExpansion(event: React.MouseEvent<HTMLDivElement>) {
    event.stopPropagation();
    this.setState((prevState) => ({
      expanded: !prevState.expanded,
    }));
  }

  private openUpdateModal() {
    this.setState({
      updateProfitCenterModalOpen: true,
    });
  }

  private closeModal() {
    this.setState({
      updateProfitCenterModalOpen: false,
    });
  }

  private handleUpdate() {
    postData('SystemAdmin/UpdateProfitCenter', this.props)
    .then(() => {
      alert('Profit center updated.');
      this.closeModal();
    });
  }
}
