import * as React from 'react';

export const PanelSectionContainer: React.SFC = (props) => (
  <div className="admin-panel-container admin-panel-container flex-item-12-12 flex-item-for-tablet-up-6-12">
    <div className="admin-panel-list">
      {props.children}
    </div>
  </div>
);

export const PanelSectionToolbar: React.SFC = (props) => (
  <div className="admin-panel-toolbar">{props.children}</div>
);

export const PanelSectionToolbarButtons: React.SFC = (props) => (
  <div className="admin-panel-action-icons-container">{props.children}</div>
);
