import '../../../../images/icons/add.svg';
import '../../../../images/icons/cancel.svg';
import '../../../../images/icons/checkmark.svg';
import '../../../../images/icons/delete.svg';
import '../../../../images/icons/edit.svg';
import '../../../../images/icons/email.svg';
import '../../../../images/icons/expand-card.svg';
import '../../../../images/icons/remove-circle.svg';
import '../../../../images/icons/upload.svg';

import * as React from 'react';

export interface CardButtonProps {
  color: 'red' | 'blue' | 'green';
  tooltip: string;
  onClick: () => void;
  icon: 'add' | 'cancel' | 'checkmark' | 'delete' | 'edit' | 'email' | 'expand-card' | 'remove-circle' | 'upload';
  additionalClasses: string[];
}

export default class CardButton extends React.Component<CardButtonProps> {
  public static defaultProps = {
    tooltip: '',
    additionalClasses: [] as string[],
  };

  public render() {
    const { color, tooltip, icon, additionalClasses } = this.props;
    return (
      <div
        className={`card-button-background card-button-${color}` + ['', ...additionalClasses].join(' ')}
        title={tooltip}
        onClick={this.onClick}
      >
        <svg className="card-button-icon">
          <use xlinkHref={`#${icon}`} />
        </svg>
      </div>
    );
  }

  private onClick = (event: React.MouseEvent<HTMLDivElement>) => {
    event.stopPropagation();
    this.props.onClick();
  }
}
