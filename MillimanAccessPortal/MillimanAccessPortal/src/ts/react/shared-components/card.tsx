import * as React from 'react';
import { postData } from '../../shared';

import { Entity } from './entity';

export interface CardProps extends Entity {
  selected: boolean;
  resetButton?: boolean;
  sublistInfo: {
    title: string;
    icon: string;
    emptyText: string;
  };
}

interface CardState {
  expanded: boolean;
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
    };

    this.sendPasswordReset = this.sendPasswordReset.bind(this);
    this.toggleExpansion = this.toggleExpansion.bind(this);
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
    const sideButtons = this.props.resetButton
      ? (
        <div
          className="card-button-background card-button-blue"
          title="Send password reset email"
          onClick={this.sendPasswordReset}
        >
          <svg className="card-button-icon">
            <use xlinkHref="#email" />
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
    return (
      <div className={`card-container ${this.indentClasses[this.props.indent] || ''}`}>
        <div
          className={`card-body-container${additionalClasses}`}
        >
          <div className="card-body-main-container">
            <div className="card-body-primary-container">
              <h2 className="card-body-primary-text">
                {this.props.primaryText + (this.props.suspended ? ' (Suspended)' : '')}
              </h2>
              <p className="card-body-secondary-text">
                {this.props.secondaryText}
              </p>
            </div>
            <div className="card-stats-container">
              {stats}
            </div>
            <div className="card-button-side-container">
              {sideButtons}
            </div>
          </div>
          {expansion}
        </div>
      </div>
    );
  }

  private sendPasswordReset(event: React.MouseEvent<HTMLDivElement>) {
    event.stopPropagation();
    postData('Account/ForgotPassword', {
      Email: this.props.email,
    })
    .then((response) => console.log('Password reset email sent!'));
  }

  private toggleExpansion(event: React.MouseEvent<HTMLDivElement>) {
    event.stopPropagation();
    this.setState((prevState) => ({
      expanded: !prevState.expanded,
    }));
  }
}

interface WithActivatedProps {
  activated: boolean;
}

export function withActivated(Component: React.ComponentType<CardProps>) {
  return class extends React.Component<CardProps & WithActivatedProps> {
    public render() {
      const { activated } = this.props as WithActivatedProps;
      const props = this.props as CardProps;
      return activated
        ? <Component {...props} />
        : (
          <Component
            {...props}
            primaryText={'(Unactivated)'}
          />
        );
    }
  };
}
