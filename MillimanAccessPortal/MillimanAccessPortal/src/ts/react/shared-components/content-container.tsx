import '../../../scss/react/shared-components/content-container.scss';

import 'tooltipster';
import 'tooltipster/src/css/tooltipster.css';
import '../../../images/cancel.svg';

import * as React from 'react';

import { ContentContainerProps } from './interfaces';

export class ContentContainer extends React.Component<ContentContainerProps, {}> {

  public constructor(props) {
    super(props);

    this.close = this.close.bind(this);
  }

  public componentDidMount() {
    history.pushState({ content: this.props.contentURL }, null);
  }

  public render() {
    return (
      <div id="iframe-container">
        <div
          id="close-content-container"
          className="tooltip"
          title="Close"
          onClick={this.close}
        >
          <svg>
            <use xlinkHref="#cancel" />
          </svg>
        </div>
        <iframe id="content-iframe" src={this.props.contentURL} />
      </div>
    );
  }

  private close(event: React.MouseEvent<HTMLElement>) {
    event.preventDefault();
    this.props.closeAction(null);
  }
}
