import * as React from 'react';

import { Entity } from './entity';

export interface CardProps extends Entity {
  selected: boolean;
}

export class Card extends React.Component<CardProps, {}> {
  public constructor(props) {
    super(props);
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
    return (
      <div className="card-container">
        <div
          className={`card-body-container${this.props.selected ? ' selected' : ''}`}
        >
          <div className="card-body-main-container">
            <div className="card-body-primary-container">
              <h2 className="card-body-primary-text">
                {this.props.primaryText}
              </h2>
              <p className="card-body-secondary-text">
                {this.props.secondaryText}
              </p>
            </div>
            <div className="card-stats-container">
              {stats}
            </div>
          </div>
          {expansion}
        </div>
      </div>
    );
  }
}
