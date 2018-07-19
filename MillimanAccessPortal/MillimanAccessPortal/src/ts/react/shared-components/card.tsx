import * as React from 'react';

import { Entity } from './interfaces';

export interface CardProps<T> {
  entity: T;
  selected: boolean;
}

export class Card<T extends Entity> extends React.Component<CardProps<T>, {}> {
  public constructor(props) {
    super(props);
  }

  public render() {
    return (
      <div className="card-container">
        <div
          className={`card-body-container${this.props.selected ? ' selected' : ''}`}
        >
          <div className="card-body-main-container">
            <div className="card-body-primary-container">
              <h2 className="card-body-primary-text">
                {this.props.entity.Name}
              </h2>
            </div>
          </div>
        </div>
      </div>
    );
  }
}
