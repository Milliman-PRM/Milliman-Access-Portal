import * as React from 'react';
import { Component } from 'react';

interface ContentCardProps {
  key: string,
  name: string,
  contentURL: string,
  description: string,
  thumbnailURL?: string,
  userguideURL?: string,
  releasenNotesURL?: string,
}

class ContentCard extends Component<ContentCardProps, {}> {
  constructor(props) {
    super(props);
  }

  render() {
    let userguideIcon = this.props.userguideURL ?
      (
        <a href={this.props.userguideURL}>
          <svg className="content-card-userguide-icon">
            <use xlinkHref="#navbar-user-guide" />
          </svg>
        </a>
      ) : null;
    let releasenNotesIcon = this.props.releasenNotesURL ?
      (
        <a href={this.props.releasenNotesURL}>
          <svg className="content-card-releasenotes-icon">
            <use xlinkHref="#navbar-user-guide" />
          </svg>
        </a>
      ) : null;
    let thumbnailImage = this.props.thumbnailURL ?
      (
        <img src={this.props.thumbnailURL} alt={this.props.name} />
      ) : null;

    return (
      <div className="content-card">
        <a href={this.props.contentURL}>
          <div className="content-card-header">
            <h2 className="content-card-title">{this.props.name}</h2>
            <div className="content-card-icons">
              {userguideIcon}
              {releasenNotesIcon}
            </div>
          </div>
          <div className="content-card-body">
            {thumbnailImage}
            <p className="content-description">{this.props.description}</p>
          </div>
        </a>
      </div>
    );
  }

}

export default ContentCard;
