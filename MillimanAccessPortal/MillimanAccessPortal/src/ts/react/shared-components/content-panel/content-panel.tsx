import '../../../../scss/react/shared-components/card-panel.scss';

import * as React from 'react';

import { ColumnSpinner } from '../column-spinner';
import { ContentPanelSectionContent } from './content-panel-sections';
import { PanelSectionContainer } from './panel-sections';

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
