import '../../../scss/react/shared-components/button-spinner.scss';

import * as React from 'react';

import '../../../images/icons/loading-spinner.svg';

interface ButtonSpinnerProps {
  version: 'circle' | 'bars';
  spinnerColor?: string;
}

export const ButtonSpinner: React.SFC<ButtonSpinnerProps> = ({ version, spinnerColor }) =>
  version === 'circle'
    ? <div className="spinner-small-r" />
    : (
      <svg className="loading-spinner" style={{ color: spinnerColor ? spinnerColor : 'white' }}>
        <use xlinkHref="#loading-spinner" />
      </svg>
    );
