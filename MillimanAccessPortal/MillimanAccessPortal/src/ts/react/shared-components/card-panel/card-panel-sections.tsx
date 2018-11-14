import * as React from 'react';

export const CardPanelSectionContainer: React.SFC = (props) => (
  <div className="admin-panel-container flex-item-12-12 flex-item-for-tablet-up-4-12 flex-item-for-desktop-up-3-12">
    <div className="admin-panel-list">
      {props.children}
    </div>
  </div>
);

export const CardPanelSectionToolbar: React.SFC = (props) => (
  <div className="admin-panel-toolbar">{props.children}</div>
);

export const CardPanelSectionToolbarButtons: React.SFC = (props) => (
  <div className="admin-panel-action-icons-container">{props.children}</div>
);

export const CardPanelSectionContent: React.SFC = (props) => (
  <div className="admin-panel-content-container">{props.children}</div>
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
