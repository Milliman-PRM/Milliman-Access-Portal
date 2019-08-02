import * as React from 'react';

export const ContentPanelSectionContent: React.SFC = (props) => (
  <div className="admin-panel-content-container">{props.children}</div>
);

export const ContentPanelSectionAction: React.SFC = (props) => (
  <div className="admin-panel-action-container">{props.children}</div>
);
