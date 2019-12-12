import '../../../scss/react/authorized-content/authorized-content.scss';
import '../../../scss/react/authorized-content/content-wrapper.scss';

import * as React from 'react';
import * as ReactDOM from 'react-dom';

import { ContentContainer, contentTypeMap } from '../shared-components/content-container';

document.addEventListener('DOMContentLoaded', () => {
  const contentURL = document.getElementById('content-url').innerText;
  const contentType = document.getElementById('content-type').innerText;
  ReactDOM.render(
    <ContentContainer
      contentURL={contentURL}
      contentType={contentTypeMap[contentType]}
    />,
    document.getElementById('content-wrapper-inner'),
  );
});
