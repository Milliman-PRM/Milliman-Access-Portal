import * as React from 'react';
import { Component } from 'react';
import '../../../scss/react/shared-components/action-icon.scss';
import { ActionIconProps } from './interfaces';
import 'tooltipster';

import 'tooltipster/src/css/tooltipster.css';


export class ActionIcon extends Component<ActionIconProps, {}> {
  public render() {
    return this.props.action && (
      <div className="action-icon-container tooltip" title={this.props.title} onClick={(event) => { event.stopPropagation(); this.props.action() }}>
        <svg className="action-icon">
          <use xlinkHref={`#${this.props.icon}`} />
        </svg>
      </div>
    );
  }
}
