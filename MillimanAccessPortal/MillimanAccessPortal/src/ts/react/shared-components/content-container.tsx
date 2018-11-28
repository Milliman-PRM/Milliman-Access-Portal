import '../../../scss/react/shared-components/content-container.scss';

import 'tooltipster';
import 'tooltipster/src/css/tooltipster.css';
import '../../../images/cancel.svg';

import * as React from 'react';

import { ContentContainerProps } from './interfaces';
import { ContentTypeEnum } from '../../view-models/content-publishing';

export class ContentContainer extends React.Component<ContentContainerProps, {}> {

  public constructor(props) {
    super(props);

    this.close = this.close.bind(this);
  }

  public componentDidMount() {
    history.pushState({ content: this.props.contentURL }, null);
  }

  public render() {
    let sandboxValues;
    
    switch (this.props.contentType) {
      case ContentTypeEnum.Pdf:
        sandboxValues = null;
        break;
      case ContentTypeEnum.Html:
        sandboxValues = 'allow-scripts allow-popups allow-forms';
        break;
      case ContentTypeEnum.Qlikview:
        sandboxValues = 'allow-same-origin allow-scripts allow-popups allow-forms';
        break;
      default:
        sandboxValues = '';

    }
        
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
        <iframe id="content-iframe" {...(sandboxValues !== null) ? {sandbox: sandboxValues} : {}} src={this.props.contentURL}></iframe>
      </div>
    );
  }

  private close(event: React.MouseEvent<HTMLElement>) {
    event.preventDefault();
    this.props.closeAction(null);
  }
}
