import '../../../scss/react/shared-components/content-container.scss';

import * as React from 'react';

import { ContentContainerProps } from './interfaces';

export class ActionIcon extends React.Component<ContentContainerProps, {}> {
  public render() {
    return (
      <div id="iframe-container">
        <iframe id="content-iframe" src={this.props.contentURL}></iframe>
      </div>
    );
  }
}
