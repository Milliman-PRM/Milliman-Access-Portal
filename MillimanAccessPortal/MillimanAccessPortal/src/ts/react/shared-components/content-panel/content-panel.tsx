import '../../../../scss/react/shared-components/card-panel.scss';
import '../../../../scss/react/shared-components/content-panel.scss';

import * as React from 'react';

import { ColumnSpinner } from '../column-spinner';

export interface ContentPanelProps {
  loading?: boolean;
}

export class ContentPanel extends React.Component<ContentPanelProps, {}> {
  public render() {
    const { children, loading } = this.props;
    return (
      <PanelSectionContainer>
        {loading && <ColumnSpinner />}
        {children}
      </PanelSectionContainer>
    );
  }
}

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

export const ContentPanelSectionContent: React.SFC = (props) => (
  <div className="admin-panel-content-container">{props.children}</div>
);
