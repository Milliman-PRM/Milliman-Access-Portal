import '../../../images/download.svg';
import '../../../images/release-notes.svg';
import '../../../images/userguide.svg';
import '../../../scss/react/authorized-content/content-card.scss';

import * as React from 'react';

import { ContentTypeEnum } from '../../view-models/content-publishing';
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
        <a
          href={this.props.releaseNotesURL}
          target="_blank"
          className="action-icon-link"
          onClick={this.selectReleaseNotes}
        >
          <ActionIcon
            action={() => false}
            label="View Release Notes"
            icon="release-notes"
          />
        </a>
      )
      : null;
    const userGuide = this.props.userguideURL
      ? (
        <a
          href={this.props.userguideURL}
          target="_blank"
          className="action-icon-link"
          onClick={this.selectUserGuide}
        >
          <ActionIcon
            action={() => false}
            label="View Userguide"
            icon="userguide"
          />
        </a>
      )
      : null;
    const contentLink = (this.props.contentTypeEnum === ContentTypeEnum.FileDownload)
      ? (
        <a
          href={this.props.contentURL}
          download={true}
          className="content-card-link content-card-download"
        >
          <div className="content-card-download-indicator">
            <svg className="content-card-download-icon">
              <use xlinkHref="#download" />
            </svg>
          </div>
        </a>
      )
      : (
        <a
          href={this.props.contentURL}
          target="_blank"
          className="content-card-link"
          onClick={this.selectContent}
        />
      );
    return (
      <div className="content-card-container">
        <div className="content-card">
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
          {contentLink}
        </div>
      </div>
    );
  }

  private selectContent(event: React.MouseEvent<HTMLElement>) {
    event.stopPropagation();
    event.preventDefault();
    this.props.selectContent(this.props.contentURL, this.props.contentTypeEnum);
  }

  private selectReleaseNotes(event: React.MouseEvent<HTMLElement>) {
    event.stopPropagation();
    event.preventDefault();
    this.props.selectContent(this.props.releaseNotesURL, ContentTypeEnum.Pdf);
  }

  private selectUserGuide(event: React.MouseEvent<HTMLElement>) {
    event.stopPropagation();
    event.preventDefault();
    this.props.selectContent(this.props.userguideURL, ContentTypeEnum.Pdf);
  }
}
