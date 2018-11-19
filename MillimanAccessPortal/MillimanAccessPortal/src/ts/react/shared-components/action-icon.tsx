import '../../../images/add.svg';
import '../../../images/collapse-cards.svg';
import '../../../images/expand-cards.svg';
import '../../../images/release-notes.svg';
import '../../../images/user.svg';
import '../../../images/userguide.svg';

import 'tooltipster';
import 'tooltipster/src/css/tooltipster.css';
import '../../../scss/react/shared-components/action-icon.scss';

import * as React from 'react';

export interface ActionIconProps {
  label: string;
  icon: 'add' | 'collapse-cards' | 'expand-cards' | 'release-notes' | 'user' | 'userguide';
  action: () => void;
  inline: boolean;
}

export class ActionIcon extends React.Component<ActionIconProps, {}> {
  public static defaultProps = {
    label: null,
    action: () => null,
    inline: true,
  };
  public render() {
    return this.props.action && (
      <div
        className={`action-icon-container${this.props.inline ? '-inline' : ''} tooltip`}
        title={this.props.label}
        onClick={this.action}
      >
        <svg className="action-icon">
          <use xlinkHref={`#${this.props.icon}`} />
        </svg>
      </div>
    );
  }

  private action = (event: React.MouseEvent<HTMLElement>) => {
    event.stopPropagation();
    this.props.action();
  }
}
