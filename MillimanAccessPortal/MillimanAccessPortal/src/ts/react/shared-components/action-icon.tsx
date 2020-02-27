import '../../../images/icons/add-group.svg';
import '../../../images/icons/add-user.svg';
import '../../../images/icons/add.svg';
import '../../../images/icons/cancel.svg';
import '../../../images/icons/checkmark.svg';
import '../../../images/icons/collapse-cards.svg';
import '../../../images/icons/delete.svg';
import '../../../images/icons/edit.svg';
import '../../../images/icons/email.svg';
import '../../../images/icons/expand-card.svg';
import '../../../images/icons/expand-cards.svg';
import '../../../images/icons/remove-circle.svg';
import '../../../images/icons/upload.svg';
import '../../../images/icons/user.svg';
import '../../../images/icons/userguide.svg';

import 'tooltipster';
import 'tooltipster/src/css/tooltipster.css';
import '../../../scss/react/shared-components/action-icon.scss';

import * as React from 'react';

export interface ActionIconProps {
  label: string;
  icon: 'add-group' | 'add-user' | 'add' | 'cancel' | 'checkmark' | 'collapse-cards' | 'delete' | 'edit'
    | 'email' | 'expand-card' | 'expand-cards' | 'remove-circle' | 'upload' | 'user' | 'userguide';
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
