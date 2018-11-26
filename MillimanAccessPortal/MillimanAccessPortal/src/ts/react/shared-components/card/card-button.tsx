import '../../../../images/add.svg';
import '../../../../images/cancel.svg';
import '../../../../images/checkmark.svg';
import '../../../../images/delete.svg';
import '../../../../images/edit.svg';
import '../../../../images/email.svg';
import '../../../../images/expand-card.svg';
import '../../../../images/remove-circle.svg';

import * as React from 'react';

export interface CardButtonProps {
  color: 'red' | 'blue' | 'green';
  tooltip: string;
  onClick: () => void;
  icon: 'add' | 'cancel' | 'checkmark' | 'delete' | 'edit' | 'email' | 'expand-card' | 'remove-circle';
  additionalClasses: string[];
}

export default class CardButton extends React.Component<CardButtonProps> {
  public static defaultProps = {
    tooltip: '',
    additionalClasses: [],
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
