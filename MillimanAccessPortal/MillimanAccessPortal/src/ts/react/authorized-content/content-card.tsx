import '../../../scss/react/authorized-content/content-card.scss';

import * as React from 'react';

import { ActionIcon } from '../shared-components/action-icon';
import { ContentItem, Filterable } from './interfaces';

require('tooltipster');
require('tooltipster/src/css/tooltipster.css');

export class ContentCard extends React.Component<ContentItem, {}> {
  public render() {
    return (
        <div className='content-card-container'>
          <div className='content-card' onClick={() => window.location.href = this.props.ContentURL}>
            <div className='content-card-header'>
              <h2 className='content-card-title'>{this.props.Name}</h2>
              <div className='content-card-icons'>
                <ActionIcon
                  action={() => window.location.href = this.props.ReleaseNotesURL}
                  title='View Release Notes'
                  icon='navbar-user-guide'
                  />
                <ActionIcon
                  action={() => window.location.href = this.props.UserguideURL}
                  title='View Userguide'
                  icon='navbar-user-guide'
                  />
              </div>
            </div>
            <div className='content-card-body'>
              {this.props.ImageURL &&
                <img
                  className='content-card-image'
                  src={this.props.ImageURL}
                  alt={this.props.Name}
                />
              }
              <p className='content-card-description'>
                {this.props.Description}
              </p>
            </div>
          </div>
        </div>
      );
  }
}
