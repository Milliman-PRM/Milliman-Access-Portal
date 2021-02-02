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
  textSuffix: string;
  subtext: string;
  subtextIsWarning?: boolean;
  editing: boolean;
  isNewChild: boolean;
  setText: (text: string) => void;
}
export class CardText extends React.Component<CardTextProps> {
  public static defaultProps = {
    textSuffix: '',
    subtitle: '',
    subtextIsWarning: false,
    editing: false,
    isNewChild: false,
    setText: (): null => null,
  };
  public render() {
    const { text, textSuffix, subtext, editing, isNewChild, setText, subtextIsWarning } = this.props;
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
              : text + (textSuffix ? ` ${textSuffix}` : '')
          }
          {isNewChild ?
            <svg className="new-child-icon">
              <use href="#expand-card" />
            </svg> : null
          }
        </h2>
        {
          subtext &&
          subtext.trim() !== '' &&
          <p className="card-body-secondary-text">
            <span className={subtextIsWarning && 'warning'}>{subtext}</span>
          </p>
        }
      </div>
    );
  }
}
