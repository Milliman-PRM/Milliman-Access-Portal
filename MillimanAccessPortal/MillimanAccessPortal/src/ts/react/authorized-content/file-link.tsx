import * as React from 'react';
import { Component } from 'react';
import '../../../scss/react/authorized-content/content-card.scss';
import { HostedFile } from './interfaces';
import 'tooltipster';

import 'tooltipster/src/css/tooltipster.css';


export class FileLink extends Component<HostedFile, {}> {
  public render() {
    return this.props.link && (
      <a href={this.props.link} className="tooltip" title={this.props.title}>
        <svg className="content-card-icon">
          <use xlinkHref="#navbar-user-guide" />
        </svg>
      </a>
    );
  }
}
