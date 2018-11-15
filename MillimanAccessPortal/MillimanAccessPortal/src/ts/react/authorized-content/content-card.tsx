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
    const image = this.props.imageURL && (
      <img
        className="content-card-image"
        src={this.props.imageURL}
        alt={this.props.name}
      />
    );
    const releaseNotes = this.props.releaseNotesURL
      ? (
        <ActionIcon
          action={this.selectReleaseNotes}
          label="View Release Notes"
          icon="release-notes"
          inline={false}
        />
      )
      : null;
    const userGuide = this.props.userguideURL
      ? (
        <ActionIcon
          action={this.selectUserGuide}
          label="View Userguide"
          icon="userguide"
          inline={false}
        />
      )
      : null;
    return (
      <div className="content-card-container">
        <div className="content-card" onClick={this.selectContent}>
          <div className="content-card-header">
            <h2 className="content-card-title">{this.props.name}</h2>
            <div className="content-card-icons">
              {releaseNotes}
              {userGuide}
            </div>
          </div>
          <div className="content-card-body">
            {image}
            <p className="content-card-description">
              {this.props.description}
            </p>
          </div>
        </div>
      </div>
    );
  }

  private selectContent() {
    this.props.selectContent(this.props.contentURL);
  }

  private selectReleaseNotes() {
    this.props.selectContent(this.props.releaseNotesURL);
  }

  private selectUserGuide() {
    this.props.selectContent(this.props.userguideURL);
  }

}
