import 'tooltipster';
import 'tooltipster/src/css/tooltipster.css';

import '../../../images/add.svg';
import '../../../images/delete.svg';
import '../../../images/edit.svg';
import '../../../images/group.svg';
import '../../../images/reports.svg';

import * as React from 'react';

import { ClientSummary } from '../../view-models/content-publishing';
import { CardProps } from './interfaces';

export class ClientCard extends React.Component<CardProps<ClientSummary>, {}> {
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
              <p className="card-body-secondary-text">
                {this.props.data.Code}
              </p>
            </div>
            <div className="card-stats-container">
              <div className="card-stat-container" title="Eligible users">
                <svg className="card-stat-icon">
                  <use xlinkHref="#group" />
                </svg>
                <h4 className="card-stat-value">
                  {this.props.data.EligibleUserCount}
                </h4>
              </div>
              <div className="card-stat-container" title="Content items">
                <svg className="card-stat-icon">
                  <use xlinkHref="#reports" />
                </svg>
                <h4 className="card-stat-value">
                  {this.props.data.RootContentItemCount}
                </h4>
              </div>
            </div>
            <div className="card-button-side-container">
              <div
                className="card-button-background card-button-red tooltip"
                title="Delete client"
              >
                <svg className="card-button-icon">
                  <use xlinkHref="#delete" />
                </svg>
                <div className="card-button-clickable" />
              </div>
              <div
                className="card-button-background card-button-blue tooltip"
                title="Edit client details"
              >
                <svg className="card-button-icon">
                  <use xlinkHref="#edit" />
                </svg>
                <div className="card-button-clickable" />
              </div>
              <div
                className="card-button-background card-button-green tooltip"
                title="Add sub-client"
              >
                <svg className="card-button-icon">
                  <use xlinkHref="#add" />
                </svg>
                <div className="card-button-clickable" />
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }
}
