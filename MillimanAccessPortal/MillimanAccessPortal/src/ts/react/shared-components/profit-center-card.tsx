import * as React from 'react';

import { ProfitCenterInfo } from '../system-admin/interfaces';
import { CardProps } from './card';

export class ProfitCenterCard extends React.Component<CardProps<ProfitCenterInfo>, {}> {
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
