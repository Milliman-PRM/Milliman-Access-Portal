import * as React from 'react';

export const CardSectionMain: React.SFC = (props) => (
  <div className="card-body-main-container">{props.children}</div>
);

export const CardSectionStats: React.SFC = (props) => (
  <div className="card-stats-container">{props.children}</div>
);

export const CardSectionButtons: React.SFC = (props) => (
  <div className="card-button-side-container">{props.children}</div>
);

export interface CardTextProps {
  text: string;
  subtext: string;
}
export class CardText extends React.Component<CardTextProps> {
  public static defaultProps = {
    subtitle: '',
  };
  public render() {
    const { text, subtext } = this.props;
    return (
      <div className="card-body-primary-container">
        <h2 className="card-body-primary-text">{text}</h2>
        <p className="card-body-secondary-text">{subtext}</p>
      </div>
    );
  }
}
