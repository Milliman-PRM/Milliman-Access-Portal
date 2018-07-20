import 'tooltipster';
import 'tooltipster/src/css/tooltipster.css';
import '../../../scss/react/shared-components/action-icon.scss';

import * as React from 'react';

import { ActionIconProps } from './interfaces';

export class ActionIcon extends React.Component<ActionIconProps, {}> {
  public render() {
    return this.props.action && (
      <div
        className="action-icon-container tooltip"
        title={this.props.title}
        onClick={this.props.action}
      >
        <svg className="action-icon">
          <use xlinkHref={`#${this.props.icon}`} />
        </svg>
      </div>
    );
  }
}
