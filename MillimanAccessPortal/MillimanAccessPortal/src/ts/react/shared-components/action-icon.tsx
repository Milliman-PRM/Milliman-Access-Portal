import '../../../images/icons/add-circle.svg';
import '../../../images/icons/add-file.svg';
import '../../../images/icons/add-folder.svg';
import '../../../images/icons/add-group.svg';
import '../../../images/icons/add-user.svg';
import '../../../images/icons/add.svg';
import '../../../images/icons/cancel-circle.svg';
import '../../../images/icons/cancel.svg';
import '../../../images/icons/check-circle.svg';
import '../../../images/icons/checkmark.svg';
import '../../../images/icons/collapse-card.svg';
import '../../../images/icons/collapse-cards.svg';
import '../../../images/icons/delete.svg';
import '../../../images/icons/download.svg';
import '../../../images/icons/edit.svg';
import '../../../images/icons/email.svg';
import '../../../images/icons/error.svg';
import '../../../images/icons/expand-card.svg';
import '../../../images/icons/expand-cards.svg';
import '../../../images/icons/information.svg';
import '../../../images/icons/move-file.svg';
import '../../../images/icons/move-folder.svg';
import '../../../images/icons/reload.svg';
import '../../../images/icons/remove-circle.svg';
import '../../../images/icons/sort-alphabetically-asc.svg';
import '../../../images/icons/sort-alphabetically-desc.svg';
import '../../../images/icons/sort-date-asc.svg';
import '../../../images/icons/sort-date-desc.svg';
import '../../../images/icons/upload.svg';
import '../../../images/icons/user.svg';
import '../../../images/icons/userguide.svg';

import 'tooltipster';
import 'tooltipster/src/css/tooltipster.css';
import '../../../scss/react/shared-components/action-icon.scss';

import * as React from 'react';

export interface ActionIconProps {
  label: string;
  icon: 'add-circle' | 'add-file' | 'add-folder' | 'add-group' | 'add-user' | 'add' | 'cancel' | 'cancel-circle' |
  'check-circle' | 'checkmark' | 'collapse-card' | 'collapse-cards' | 'delete' | 'download' | 'edit' | 'email' |
  'error' | 'expand-card' | 'expand-cards' | 'information' | 'move-file' | 'move-folder' | 'reload' | 'remove-circle' |
  'sort-alphabetically-asc' | 'sort-alphabetically-desc' | 'sort-date-asc' | 'sort-date-desc' | 'upload' | 'user' |
  'userguide';
  action: () => void;
  inline: boolean;
  disabled?: boolean;
  cursor?: boolean;
}

export class ActionIcon extends React.Component<ActionIconProps, {}> {
  public static defaultProps = {
    label: null as string,
    action: (): null => null,
    inline: true,
    disabled: false,
    cursor: true,
  };
  public render() {
    const { inline, disabled, label, icon, action, cursor } = this.props;
    return action && (
      <div
        className={`action-icon-container${inline ? '-inline' : ''}${disabled ? ' disabled' : ''}
                    tooltip ${!cursor ? ' no-cursor' : ''}`}
        title={label}
        onClick={this.action}
      >
        <svg className="action-icon">
          <use xlinkHref={`#${icon}`} />
        </svg>
      </div>
    );
  }

  private action = (event: React.MouseEvent<HTMLElement>) => {
    event.stopPropagation();
    if (!this.props.disabled) {
      this.props.action();
    }
  }
}

export const ActionIconButtonContainer: React.SFC<{ color: 'blue' | 'green' | 'red'; }> = (props) => (
  <div className={`action-icon-button-container ${props.color}`}>
    {props.children}
  </div>
);
