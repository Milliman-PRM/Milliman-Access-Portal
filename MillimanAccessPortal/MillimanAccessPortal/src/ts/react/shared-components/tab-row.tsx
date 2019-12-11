import '../../../scss/react/shared-components/tab-row.scss';

import * as React from 'react';

export interface Tab {
  id: string;
  label: string;
}

export interface TabRowProps {
  tabs: Tab[];
  onTabSelect: (id: string) => void;
  selectedTab: string;
  fullWidth: boolean;
}

export class TabRow extends React.Component<TabRowProps> {
  public render() {
    const { fullWidth } = this.props;
    return (
      <div className={`tab-row${fullWidth ? ' full-width' : ' min-width'}`}>
        {this.renderTabs()}
      </div>
    );
  }

  private renderTabs() {
    const { tabs, onTabSelect, selectedTab } = this.props;
    return tabs.map((tab) => (
      <div
        key={tab.id}
        className={`tab${tab.id === selectedTab ? ' selected' : ''}`}
        onClick={() => {
          if (tab.id !== selectedTab) {
            onTabSelect(tab.id);
          }
        }}
      >
        {tab.label}
      </div>
    ));
  }
}
