import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { ContentTypeEnum } from '../../view-models/content-publishing';
import { ContentContainer } from '../shared-components/content-container';

import '../../../scss/react/authorized-content/authorized-content.scss';
import '../../../scss/react/authorized-content/content-wrapper.scss';

document.addEventListener('DOMContentLoaded', () => {
  const contentURL = document.getElementById('content-url').innerText;
  const contentType = document.getElementById('content-type').innerText;
  const contentTypeMap: { [name: string]: ContentTypeEnum } = {
    Qlikview: ContentTypeEnum.Qlikview,
    Html: ContentTypeEnum.Html,
    Pdf: ContentTypeEnum.Pdf,
    FileDownload: ContentTypeEnum.FileDownload,
    PowerBi: ContentTypeEnum.PowerBI,
  };
  ReactDOM.render(
    <ContentContainer
      contentURL={contentURL}
      contentType={contentTypeMap[contentType]}
    />,
    document.getElementById('content-wrapper-inner'),
  );
});
