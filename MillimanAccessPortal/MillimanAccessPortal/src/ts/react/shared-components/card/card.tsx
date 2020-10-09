import * as React from 'react';

import { PublicationWithQueueDetails, ReductionWithQueueDetails } from '../../models';
import { CardStatus } from './card-status';

export interface CardAttributes {
  disabled?: boolean;
  readonly?: boolean;
  expanded?: boolean;
  editing?: boolean;
  insertCard?: boolean;
  profitCenterModalOpen?: boolean;
}

export interface CardProps {
  onSelect: () => void;
  activated: boolean;
  disabled: boolean;
  readonly: boolean;
  selected: boolean;
  suspended: boolean;
  inactive: boolean;
  locked: boolean;
  insertCard: boolean;
  indentation: number;
  status: PublicationWithQueueDetails | ReductionWithQueueDetails;
}

export class Card extends React.Component<CardProps> {
  public static defaultProps = {
    activated: true,
    disabled: false,
    readonly: false,
    suspended: false,
    inactive: false,
    locked: false,
    insertCard: false,
    indentation: 1,
    status: null as PublicationWithQueueDetails | ReductionWithQueueDetails,
  };

  private indentClasses: { [indent: number]: string; } = {
    1: 'card-100',
    2: 'card-90',
    3: 'card-80',
  };

  public render() {
    const { indentation, disabled, readonly, selected, suspended,
      inactive, locked, insertCard, onSelect, status, children } = this.props;

    const cardClass = 'card-container'
      + (indentation ? ` ${this.indentClasses[indentation] || this.indentClasses[1]}` : '')
      + (disabled ? ' card-disabled' : '')
      + (readonly ? ' card-readonly' : '')
      + (insertCard ? ' insert-card' : '');
    const cardBodyClass = 'card-body-container'
      + (selected ? ' selected' : '')
      + (locked ? ' locked' : suspended ? ' suspended' : inactive ? ' inactive' : '');

    return (
      <div className={cardClass} onClick={disabled ? () => null : onSelect}>
        <div className={cardBodyClass}>
          {children}
        </div>
        {status && <CardStatus status={status} />}
      </div>
    );
  }
}
