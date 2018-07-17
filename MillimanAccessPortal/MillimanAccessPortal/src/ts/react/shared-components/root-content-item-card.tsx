import 'tooltipster';
import 'tooltipster/src/css/tooltipster.css';
import '../../../scss/map.scss';

import * as React from 'react';

import { RootContentItemSummary } from '../../view-models/content-publishing';
import { Card } from './card';

export class RootContentItemCard extends Card<RootContentItemSummary> {
  public constructor(props) {
    super(props);
  }

  public render() {
    return (
      <div className="card-container">
        <div className="card-body-container">
          <div className="card-body-main-container">
            <div className="card-body-primary-container">
              <h2 className="card-body-primary-text">
                {this.props.data.ContentName}
              </h2>
              <p className="card-body-secondary-text">
                {this.props.data.ContentTypeName}
              </p>
            </div>
            <div className="card-stats-container">
              <div className="card-stat-container" title="Selection Groups">
                <svg className="card-stat-icon">
                  <use xlinkHref="this.props.icon" />
                </svg>
                <h4 className="card-stat-value">this.props.statValue</h4>
              </div>
            </div>
            <div className="card-button-side-container">
              <div className="card-button-background tooltip" title="">
                <svg className="card-button-icon">
                  <use xlinkHref="" />
                </svg>
                <div className="card-button-clickable" />
              </div>
              <div className="card-button-background tooltip" title="">
                <svg className="card-button-icon">
                  <use xlinkHref="" />
                </svg>
                <div className="card-button-clickable" />
              </div>
              <div className="card-button-background tooltip" title="">
                <svg className="card-button-icon">
                  <use xlinkHref="" />
                </svg>
                <div className="card-button-clickable" />
              </div>
              <div className="card-button-background tooltip" title="">
                <svg className="card-button-icon">
                  <use xlinkHref="" />
                </svg>
                <div className="card-button-clickable" />
              </div>
            </div>
          </div>
        </div>
        <div className="card-status-container status-0">
          <span>
            <strong>
              {this.props.data.PublicationDetails.StatusName}
            </strong>
            <em>
              {this.props.data.PublicationDetails.User.FirstName}
            </em>
          </span>
        </div>
      </div>
    );
  }
}
