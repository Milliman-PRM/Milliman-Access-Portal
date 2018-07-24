import * as React from 'react';

import { Entity } from './interfaces';

export interface CardProps extends Entity {
  selected: boolean;
}

export class Card extends React.Component<CardProps, {}> {
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
                {this.props.PrimaryText}
              </h2>
            </div>
          </div>
        </div>
      </div>
    );
  }
}
