import * as React from 'react';

import { PublicationWithQueueDetails, ReductionWithQueueDetails } from '../../models';
import { CardStatus } from './card-status';

export interface CardAttributes {
  disabled?: boolean;
  expanded?: boolean;
  editing?: boolean;
  profitCenterModalOpen?: boolean;
}

export interface CardProps {
  onSelect: () => void;
  activated: boolean;
  disabled: boolean;
  selected: boolean;
  suspended: boolean;
  inactive: boolean;
  locked: boolean;
  indentation: number;
  status: PublicationWithQueueDetails | ReductionWithQueueDetails;
  bannerMessage?: {
    level: 'message' | 'informational' | 'error';
    message: JSX.Element;
  };
}

export class Card extends React.Component<CardProps> {
  public static defaultProps = {
    activated: true,
    disabled: false,
    suspended: false,
    inactive: false,
    locked: false,
    indentation: 1,
    status: null as PublicationWithQueueDetails | ReductionWithQueueDetails,
  };

  private indentClasses: { [indent: number]: string; } = {
    1: 'card-100',
    2: 'card-90',
    3: 'card-80',
  };

  public render() {
    const {
      indentation, disabled, selected, suspended, inactive, locked, onSelect, status, bannerMessage, children,
    } = this.props;

    const cardClass = 'card-container'
      + (indentation ? ` ${this.indentClasses[indentation] || this.indentClasses[1]}` : '')
      + (disabled ? ' card-disabled' : '');
    const cardBodyClass = 'card-body-container'
      + (selected ? ' selected' : '')
      + (locked ? ' locked' : suspended ? ' suspended' : inactive ? ' inactive' : '');

    return (
      <div className={cardClass} onClick={disabled ? () => null : onSelect}>
        <div className={cardBodyClass}>
          {children}
        </div>
        {status && <CardStatus status={status} />}
        {!status && bannerMessage &&
          <div className={`card-status-container status-${bannerMessage.level}`}>
            {bannerMessage.message}
          </div>
        }
      </div>
    );
  }
}
