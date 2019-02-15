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
    const image = this.props.ImageURL && (
      <img
        className="content-card-image"
        src={this.props.ImageURL}
        alt={this.props.Name}
      />
    );
    const releaseNotes = this.props.ReleaseNotesURL
      ? (
        <a
          href={this.props.ReleaseNotesURL}
          target="_blank"
          className="secondary-button"
          onClick={this.selectReleaseNotes}
        >
          Release Notes
        </a>
      )
      : null;
    const userGuide = this.props.UserguideURL
      ? (
        <a
          href={this.props.UserguideURL}
          target="_blank"
          className="secondary-button"
          onClick={this.selectUserGuide}
        >
          User Guide
        </a>
      )
      : null;
    const newWindow = (this.props.ContentTypeEnum === ContentTypeEnum.FileDownload)
      ? (
        <a
          href={this.props.ContentURL}
          download={true}
          className="secondary-button"
        >
          Download
        </a>
      ) : (
        <a
          href={this.props.ContentURL}
          target="_blank"
          className="secondary-button"
        >
          Open in New Tab
        </a>
      );
    const contentLink = (this.props.ContentTypeEnum === ContentTypeEnum.FileDownload)
      ? (
        <a
          href={this.props.ContentURL}
          download={true}
          className="content-card-link content-card-download"
        />
      ) : (
        <a
          href={this.props.ContentURL}
          target="_blank"
          className="content-card-link"
          onClick={this.selectContent}
        />
        );
    return (
      <div className="content-card-container">
        <div className="content-card">
          <div className="content-card-header">
            <h2 className="content-card-title">{this.props.Name}</h2>
          </div>
          <div className={`content-card-body${this.props.Description ? '' : ' image-only'}`}>
            {image}
            {this.props.Description && <p className="content-card-description">{this.props.Description}</p>}
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
    this.props.selectContent(this.props.ContentURL, this.props.ContentTypeEnum);
  }

  private selectReleaseNotes(event: React.MouseEvent<HTMLElement>) {
    event.stopPropagation();
    event.preventDefault();
    this.props.selectContent(this.props.ReleaseNotesURL, ContentTypeEnum.Pdf);
  }

  private selectUserGuide(event: React.MouseEvent<HTMLElement>) {
    event.stopPropagation();
    event.preventDefault();
    this.props.selectContent(this.props.UserguideURL, ContentTypeEnum.Pdf);
  }
}
