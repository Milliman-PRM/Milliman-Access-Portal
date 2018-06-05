import * as React from 'react';
import { Component } from 'react';
import '../../../scss/react/authorized-content/content-card.scss';

require('tooltipster');
require('tooltipster/src/css/tooltipster.css');

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
    let releasenNotesIcon = this.props.releasenNotesURL ?
      (
        <a href={this.props.releasenNotesURL} className="tooltip" title="View Release Notes">
          <svg className="content-card-icon">
            <use xlinkHref="#navbar-user-guide" />
          </svg>
        </a>
      ) : null;
    let userguideIcon = this.props.userguideURL ?
      (
        <a href={this.props.userguideURL} className="tooltip" title="View Userguide">
          <svg className="content-card-icon">
            <use xlinkHref="#navbar-user-guide" />
          </svg>
        </a>
      ) : null;
    let thumbnailImage = this.props.thumbnailURL ?
      (
        <img className="content-card-image" src={this.props.thumbnailURL} alt={this.props.name} />
      ) : null;

    return (
      <div className="content-card-container">
        <div className="content-card" onClick={() => window.location.href = this.props.contentURL}>
          <div className="content-card-header">
            <h1 className="content-card-title">{this.props.name}</h1>
            <div className="content-card-icons">
              {releasenNotesIcon}
              {userguideIcon}
            </div>
          </div>
          <div className="content-card-body">
            {thumbnailImage}
            <p className="content-card-description">{this.props.description}</p>
          </div>
        </div>
      </div>
    );
  }

}

export default ContentCard;
