import '../../../scss/react/shared-components/content-container.scss';

import * as React from 'react';

import { ContentContainerProps } from './interfaces';

import 'tooltipster';
import 'tooltipster/src/css/tooltipster.css';

require('../../../images/cancel.svg');


export class ContentContainer extends React.Component<ContentContainerProps, {}> {

  public componentDidMount() {
    history.pushState({ content: this.props.contentURL }, null);
  }

  public render() {
    return (
      <div id="iframe-container">
        <div
          id="close-content-container"
          className='tooltip'
          title="Close"
          onClick={(event) => {
            event.stopPropagation();
            this.props.closeAction(null);
          }}>
          <svg>
            <use xlinkHref='#cancel' />
          </svg>
        </div>
        <iframe id="content-iframe" src={this.props.contentURL}></iframe>
      </div>
    );
  }
}
