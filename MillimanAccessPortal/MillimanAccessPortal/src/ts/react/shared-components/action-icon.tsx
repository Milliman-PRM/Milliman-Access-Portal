import '../../../images/icons/add.svg';
import '../../../images/icons/cancel.svg';
import '../../../images/icons/collapse-cards.svg';
import '../../../images/icons/expand-cards.svg';
import '../../../images/icons/release-notes.svg';
import '../../../images/icons/user.svg';
import '../../../images/icons/userguide.svg';

import 'tooltipster';
import 'tooltipster/src/css/tooltipster.css';
import '../../../scss/react/shared-components/action-icon.scss';

import * as React from 'react';

export interface ActionIconProps {
  label: string;
  icon: 'add' | 'cancel' | 'collapse-cards' | 'expand-cards' | 'release-notes' | 'user' | 'userguide';
  action: () => void;
  inline: boolean;
}

export class ActionIcon extends React.Component<ActionIconProps, {}> {
  public static defaultProps = {
    label: null as string,
    action: (): null => null,
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
