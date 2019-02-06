import '../../../scss/react/shared-components/button-spinner.scss';

import * as React from 'react';

import '../../../images/icons/loading-spinner.svg';

export class ButtonSpinner extends React.Component {
  public render() {
    return <svg className="loading-spinner"><use xlinkHref="#loading-spinner" /></svg>;
  }
}
