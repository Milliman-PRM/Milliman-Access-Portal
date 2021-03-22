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
    const newWindow = (
      <a
        href={this.props.contentURL}
        target="_blank"
        className="secondary-button"
        onClick={this.props.contentTypeEnum === ContentTypeEnum.FileDownload ? this.selectContent : null}
      >
        {this.props.contentTypeEnum === ContentTypeEnum.FileDownload
          ? 'Download'
          : 'Open in New Tab'
        }
      </a>
    );
    const contentLink = (
      <a
        href={this.props.contentURL}
        className="content-card-link"
        onClick={this.selectContent}
      />
      );

      let caption = "";
      if (this.props.contentTypeEnum === ContentTypeEnum.PowerBi && this.props.typeSpecificDetailObject != null && this.props.typeSpecificDetailObject.editableEnabled) {
          caption = "Editing capabilities have been enable for this Power BI document.Saving the document will update it for all the users";
      }
  

    return (
      <div className="content-card-container">
        <div className="content-card">
          <div className="content-card-header">
            <h2 className="content-card-title">{this.props.name}</h2>
          </div>
          <div className={`content-card-body${this.props.description ? '' : ' image-only'}`}>
            {image}
            {this.props.description && <p className="content-card-description">{this.props.description}</p>}
            {caption}
          </div>
          <div className="secondary-actions">
            {releaseNotes}
            {userGuide}
            {newWindow}
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
