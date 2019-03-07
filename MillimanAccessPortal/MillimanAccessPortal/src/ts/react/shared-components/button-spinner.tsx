import '../../../scss/react/shared-components/button-spinner.scss';

import * as React from 'react';

import '../../../images/icons/loading-spinner.svg';

interface ButtonSpinnerProps {
  version: 'circle' | 'bars';
}

export const ButtonSpinner: React.SFC<ButtonSpinnerProps> = ({ version }) =>
  version === 'circle'
    ? <div className="spinner-small-r" />
    : (
      <svg className="loading-spinner">
        <use xlinkHref="#loading-spinner" />
      </svg>
    );
