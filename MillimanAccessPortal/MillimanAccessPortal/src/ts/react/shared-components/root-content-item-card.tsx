import 'tooltipster';
import 'tooltipster/src/css/tooltipster.css';

import * as React from 'react';

import { RootContentItemInfo } from '../system-admin/interfaces';
import { CardProps } from './interfaces';

export class RootContentItemCard extends React.Component<CardProps<RootContentItemInfo>, {}> {
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
                {this.props.data.Name}
              </h2>
            </div>
          </div>
        </div>
      </div>
    );
  }
}
