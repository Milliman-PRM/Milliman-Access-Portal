import { ajax } from 'jquery';
import * as React from 'react';

import { Entity } from './entity';

export interface CardProps extends Entity {
  selected: boolean;
  resetButton?: boolean;
}

export class Card extends React.Component<CardProps, {}> {
  private indentClasses: { [indent: number]: string; } = {
    1: 'card-100',
    2: 'card-90',
    3: 'card-80',
  };

  public constructor(props) {
    super(props);

    this.sendPasswordReset = this.sendPasswordReset.bind(this);
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
          onClick={this.sendPasswordReset}
        >
          <svg className="card-button-icon">
            <use xlinkHref="#email" />
          </svg>
        </div>
      )
      : null;
    const detailList = this.props.detailList && this.props.detailList.map((detail, i) => (
      <li
        key={i}
      >
        <div>{detail}</div>
      </li>
    ));
    const expansion = (
      <div className="card-expansion-container">
        <ul>
          {detailList}
        </ul>
      </div>
    );
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

  private sendPasswordReset() {
    ajax({
      data: {
        userId: this.props.id,
      },
      method: 'POST',
      url: '',
    }).done((response) => {
      console.log('Password reset email sent!');
    }).fail((response) => {
      throw new Error(response.getResponseHeader('Warning') || 'Unknown error');
    });
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
