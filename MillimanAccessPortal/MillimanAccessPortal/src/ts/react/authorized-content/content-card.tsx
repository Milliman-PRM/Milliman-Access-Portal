import '../../../scss/react/authorized-content/content-card.scss';

import * as React from 'react';

import { ActionIcon } from '../shared-components/action-icon';
import { ContentItem, ContentCardFunctions } from './interfaces';

require('tooltipster');
require('tooltipster/src/css/tooltipster.css');

require('../../../images/userguide.svg');
require('../../../images/release-notes.svg');

interface ContentCardProps extends ContentItem, ContentCardFunctions { }
export class ContentCard extends React.Component<ContentCardProps, {}> {
  public render() {
    return (
        <div className='content-card-container'>
          <div className='content-card' onClick={() => this.props.selectContent(this.props.ContentURL)}>
            <div className='content-card-header'>
              <h2 className='content-card-title'>{this.props.Name}</h2>
              <div className='content-card-icons'>
              {this.props.ReleaseNotesURL &&
                <ActionIcon
                  action={() => this.props.selectContent(this.props.ReleaseNotesURL)}
                  title='View Release Notes'
                  icon='release-notes'
                />
              }
              {this.props.ReleaseNotesURL &&
                <ActionIcon
                  action={() => this.props.selectContent(this.props.UserguideURL)}
                  title='View Userguide'
                  icon='userguide'
                />
              }
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
