import '../../../scss/map_modules/_form.scss';
import '../../../scss/react/shared-components/content-container.scss';

import '../../../images/icons/cancel.svg';

import * as React from 'react';

import { ContentTypeEnum } from '../../view-models/content-publishing';
import { ColumnSpinner } from './column-spinner';
import { ContentContainerProps } from './interfaces';

export const contentTypeMap: { [name: string]: ContentTypeEnum } = {
  Qlikview: ContentTypeEnum.Qlikview,
  Html: ContentTypeEnum.Html,
  Pdf: ContentTypeEnum.Pdf,
  FileDownload: ContentTypeEnum.FileDownload,
  PowerBi: ContentTypeEnum.PowerBi,
};

interface ContentContainerState {
  isLoading: boolean;
}

export class ContentContainer extends React.Component<ContentContainerProps, ContentContainerState> {

  public constructor(props: ContentContainerProps) {
    super(props);
    this.state = {
      isLoading: true,
    };
  }

  public closeWindow() {
    if (window.location === window.parent.location) {
      window.close();
    } else {
      // IE doesn't support popstate on URL hashchange so we need to treat it differently here.
      const isIE = navigator.userAgent.indexOf('MSIE ') > -1 || navigator.userAgent.indexOf('Trident/') > -1;
      if (!isIE) {
        window.parent.history.back();
      } else {
        window.parent.location.reload();
      }
    }
  }

  public componentDidMount() {
    if (this.props.contentType === ContentTypeEnum.FileDownload) {
      window.location.href = this.props.contentURL;
    }
  }

  public render() {
    if (this.props.contentType === ContentTypeEnum.FileDownload) {
      return (
        <div id="message-container" className="form-section-container">
          <h2 className="primary-message">Your download should begin shortly...</h2>
          <h3 className="secondary-message">This window can be closed once your download has completed</h3>
          <button id="download-close-button" className="blue-button" onClick={this.closeWindow}>CLOSE WINDOW</button>
        </div>
      );
    }

    let sandboxValues;
    switch (this.props.contentType) {
      case ContentTypeEnum.Pdf:
        sandboxValues = null;
        break;
      case ContentTypeEnum.Html:
        sandboxValues =
          'allow-scripts allow-popups allow-modals allow-forms allow-popups-to-escape-sandbox allow-downloads';
        break;
      case ContentTypeEnum.Qlikview:
        sandboxValues = null;
        break;
      case ContentTypeEnum.PowerBi:
        sandboxValues = 'allow-scripts allow-popups allow-modals allow-forms allow-same-origin allow-downloads';
        break;
      default:
        sandboxValues = '';
    }

    const frame = this.props.contentType === ContentTypeEnum.Pdf ? (
      <object
        data={this.props.contentURL}
        type="application/pdf"
        onLoad={() => this.setState({ isLoading: false })}
      />
    ) : (
      <iframe
        src={this.props.contentURL}
        sandbox={sandboxValues}
        onLoad={() => this.setState({ isLoading: false })}
      />
    );

    return (
      <div className="iframe-container">
        {this.state.isLoading && <ColumnSpinner />}
        {this.props.children}
        {frame}
      </div>
    );
  }
}
