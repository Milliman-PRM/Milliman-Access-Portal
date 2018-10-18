import * as React from 'react';

export enum CardButtonColor {
  RED = 'red',
  BLUE = 'blue',
  GREEN = 'green',
}

export interface CardButtonProps {
  color: CardButtonColor;
  tooltip?: string;
  onClick: (event: React.MouseEvent<HTMLDivElement>) => void;
  icon: string;
}

export default class CardButton extends React.Component<CardButtonProps> {
  public render() {
    const { color, tooltip, onClick, icon } = this.props;
    return (
      <div
        className={`card-button-background card-button-${color}`}
        title={tooltip}
        onClick={onClick}
      >
        <svg className="card-button-icon">
          <use xlinkHref={`#${icon}`} />
        </svg>
      </div>
    );
  }
}
