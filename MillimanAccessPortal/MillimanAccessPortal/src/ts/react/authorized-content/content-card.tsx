import '../../../images/release-notes.svg';
import '../../../images/userguide.svg';
import '../../../scss/react/authorized-content/content-card.scss';

import * as React from 'react';

import { ActionIcon } from '../shared-components/action-icon';
import { ContentCardFunctions, ContentItem } from './interfaces';

require('tooltipster');
require('tooltipster/src/css/tooltipster.css');

interface ContentCardProps extends ContentItem, ContentCardFunctions { }
export class ContentCard extends React.Component<ContentCardProps, {}> {

  public constructor(props) {
    super(props);

    this.selectContent = this.selectContent.bind(this);
    this.selectReleaseNotes = this.selectReleaseNotes.bind(this);
    this.selectUserGuide = this.selectUserGuide.bind(this);
  }

  public render() {
    const image = this.props.ImageURL && (
      <img
        className="content-card-image"
        src={this.props.ImageURL}
        alt={this.props.Name}
      />
    );
    const releaseNotes = this.props.ReleaseNotesURL
      ? (
        <ActionIcon
          action={this.selectReleaseNotes}
          title="View Release Notes"
          icon="release-notes"
        />
      )
      : null;
    const userGuide = this.props.UserguideURL
      ? (
        <ActionIcon
          action={this.selectUserGuide}
          title="View Userguide"
          icon="userguide"
        />
      )
      : null;
    return (
      <div className="content-card-container">
        <div className="content-card" onClick={this.selectContent}>
          <div className="content-card-header">
            <h2 className="content-card-title">{this.props.Name}</h2>
            <div className="content-card-icons">
              {releaseNotes}
              {userGuide}
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

  private selectContent(event: React.MouseEvent<HTMLElement>) {
    event.stopPropagation();
    this.props.selectContent(this.props.ContentURL);
  }

  private selectReleaseNotes(event: React.MouseEvent<HTMLElement>) {
    event.stopPropagation();
    this.props.selectContent(this.props.ReleaseNotesURL);
  }

  private selectUserGuide(event: React.MouseEvent<HTMLElement>) {
    event.stopPropagation();
    this.props.selectContent(this.props.UserguideURL);
  }

}
