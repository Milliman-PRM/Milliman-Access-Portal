import 'tooltipster';
import 'tooltipster/src/css/tooltipster.css';
import '../../../images/add.svg';
import '../../../images/delete.svg';
import '../../../images/edit.svg';
import '../../../images/group.svg';
import '../../../images/reports.svg';

import * as React from 'react';

import { UserInfo } from '../system-admin/interfaces';
import { CardProps } from './interfaces';

export class UserCard extends React.Component<CardProps<UserInfo>, {}> {
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
