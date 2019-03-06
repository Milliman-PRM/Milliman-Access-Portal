import '../../../scss/react/authorized-content/content-card.scss';

import * as React from 'react';

import { ContentTypeEnum } from '../../view-models/content-publishing';
import { ContentCardFunctions, ContentItem } from './interfaces';

interface ContentCardProps extends ContentItem, ContentCardFunctions { }
export class ContentCard extends React.Component<ContentCardProps, {}> {

  public constructor(props: ContentCardProps) {
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
          className="secondary-button"
          onClick={this.selectReleaseNotes}
        >
          Release Notes
        </a>
      )
      : null;
    const userGuide = this.props.userguideURL
      ? (
        <a
          href={this.props.userguideURL}
          target="_blank"
          className="secondary-button"
          onClick={this.selectUserGuide}
        >
          User Guide
        </a>
      )
      : null;
    const newWindow = (this.props.contentTypeEnum === ContentTypeEnum.FileDownload)
      ? (
        <a
          href={this.props.contentURL}
          download={true}
          className="secondary-button"
        >
          Download
        </a>
      ) : (
        <a
          href={this.props.contentURL}
          target="_blank"
          className="secondary-button"
        >
          Open in New Tab
        </a>
      );
    const contentLink = (this.props.contentTypeEnum === ContentTypeEnum.FileDownload)
      ? (
        <a
          href={this.props.contentURL}
          download={true}
          className="content-card-link content-card-download"
        />
      ) : (
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
          </div>
          <div className={`content-card-body${this.props.description ? '' : ' image-only'}`}>
            {image}
            {this.props.description && <p className="content-card-description">{this.props.description}</p>}
          </div>
          <div className="secondary-actions">
            {newWindow}
            {releaseNotes}
            {userGuide}
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
