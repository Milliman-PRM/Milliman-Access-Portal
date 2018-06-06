import * as React from 'react';
import { Component } from 'react';
import '../../../scss/react/authorized-content/content-card.scss';
import { ContentItem, Filterable } from './interfaces';
import { FileLink } from './file-link';

require('tooltipster');
require('tooltipster/src/css/tooltipster.css');


interface ContentCardProps extends ContentItem, Filterable { }
export class ContentCard extends Component<ContentCardProps, {}> {
  public render() {
    return (this.props.name.toLowerCase().indexOf(this.props.filterString) !== -1) &&
    (
      <div className="content-card-container">
        <div className="content-card" onClick={() => window.location.href = this.props.contentURL}>
          <div className="content-card-header">
            <h2 className="content-card-title">{this.props.name}</h2>
            <div className="content-card-icons">
              <FileLink link={this.props.releaseNotesURL} title="View Release Notes" />
              <FileLink link={this.props.userguideURL} title="View Userguide" />
            </div>
          </div>
          <div className="content-card-body">
            {this.props.imageURL &&
              <img
                className="content-card-image"
                src={this.props.imageURL}
                alt={this.props.name}
                />
            }
            <p className="content-card-description">
              {this.props.description}
            </p>
          </div>
        </div>
      </div>
    );
  }
}
