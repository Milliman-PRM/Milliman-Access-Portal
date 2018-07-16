import '../../../scss/react/authorized-content/content-card.scss';

import * as React from 'react';

import { ActionIcon } from '../shared-components/action-icon';
import { ContentItem, Filterable } from './interfaces';

require('tooltipster');
require('tooltipster/src/css/tooltipster.css');

require('../../../images/userguide.svg');
require('../../../images/release-notes.svg');

export class ContentCard extends React.Component<ContentItem, {}> {
  public constructor(props) {
    super(props);

    this.navigateToContent = this.navigateToContent.bind(this);
    this.navigateToReleaseNotes = this.navigateToReleaseNotes.bind(this);
    this.navigateToUserguide = this.navigateToUserguide.bind(this);
  }

  public navigateToContent(event: React.MouseEvent<HTMLElement>) {
    event.stopPropagation();
    window.location.href = this.props.ContentURL;
  }

  public navigateToReleaseNotes(event: React.MouseEvent<HTMLElement>) {
    event.stopPropagation();
    window.location.href = this.props.ReleaseNotesURL;
  }

  public navigateToUserguide(event: React.MouseEvent<HTMLElement>) {
    event.stopPropagation();
    window.location.href = this.props.UserguideURL;
  }

  public render() {
    const image = this.props.ImageURL && (
      <img
        className="content-card-image"
        src={this.props.ImageURL}
        alt={this.props.Name}
      />
    );
    return (
        <div className="content-card-container">
          <div className="content-card" onClick={this.navigateToContent}>
            <div className="content-card-header">
              <h2 className="content-card-title">{this.props.Name}</h2>
              <div className="content-card-icons">
                <ActionIcon
                  action={this.navigateToReleaseNotes}
                  title="View Release Notes"
                  icon="release-notes"
                />
                <ActionIcon
                  action={this.navigateToUserguide}
                  title="View Userguide"
                  icon="userguide"
                />
              </div>
            </div>
            <div className="content-card-body">
              {image}
              <p className="content-card-description">
                {this.props.Description}
              </p>
            </div>
          </div>
        </div>
      );
  }
}
