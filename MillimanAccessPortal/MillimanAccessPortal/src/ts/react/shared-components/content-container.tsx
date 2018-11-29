import '../../../scss/react/shared-components/content-container.scss';
import 'tooltipster';
import 'tooltipster/src/css/tooltipster.css';
import '../../../images/cancel.svg';

import * as React from 'react';

import { ContentTypeEnum } from '../../view-models/content-publishing';
import { ContentContainerProps } from './interfaces';

export class ContentContainer extends React.Component<ContentContainerProps, {}> {

  public constructor(props) {
    super(props);
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

    const frame = this.props.contentType === ContentTypeEnum.Pdf
      ? <object data={this.props.contentURL} type="application/pdf" />
      : <iframe src={this.props.contentURL} sandbox={sandboxValues} />;

    return (
      <div id="iframe-container">
        {frame}
      </div>
    );
  }
}
