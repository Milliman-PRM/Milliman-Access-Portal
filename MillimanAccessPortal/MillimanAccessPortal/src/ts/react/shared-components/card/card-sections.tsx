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
  editing: boolean;
  setText: (text: string) => void;
}
export class CardText extends React.Component<CardTextProps> {
  public static defaultProps = {
    subtitle: '',
    editing: false,
    setText: () => null,
  };
  public render() {
    const { text, subtext, editing, setText } = this.props;
    return (
      <div className="card-body-primary-container">
        <h2 className="card-body-primary-text">
          {
            editing
              ? <input
                value={text}
                onChange={(event) => setText(event.target.value)}
                onClick={(event) => event.stopPropagation()}
              />
              : text
          }
        </h2>
        <p className="card-body-secondary-text">{subtext}</p>
      </div>
    );
  }
}
