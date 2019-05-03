import * as React from 'react';

export const CardPanelSectionContent: React.SFC = (props) => (
  <div className="form-content-container">{props.children}</div>
);

export const CardPanelSectionAction: React.SFC = (props) => (
  <div className="admin-panel-action-container">{props.children}</div>
);
