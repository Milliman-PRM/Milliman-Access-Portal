import '../../../scss/react/shared-components/content-container.scss';

import * as React from 'react';

import { ContentContainerProps } from './interfaces';

export class ContentContainer extends React.Component<ContentContainerProps, {}> {
  public render() {
    return (
      <div id="iframe-container">
        <span id="close-content-container" onClick={() => this.props.closeAction(null)}>Close</span>
        <iframe id="content-iframe" src={this.props.contentURL}></iframe>
      </div>
    );
  }
}
