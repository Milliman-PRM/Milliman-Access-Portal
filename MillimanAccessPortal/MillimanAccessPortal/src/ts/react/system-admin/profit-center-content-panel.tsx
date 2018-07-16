import '../../../scss/react/shared-components/content-panel.scss';

import * as React from 'react';

import { ProfitCenterPanelProps, ProfitCenterPanelState } from './interfaces';

export class ProfitCenterContentPanel extends React.Component<ProfitCenterPanelProps, ProfitCenterPanelState> {
  public constructor(props) {
    super(props);
    this.state = {
      profitCenterList: []
    };
  }

  public render() {
    return (
      <div className="admin-panel-content-container">
        <ul className="admin-panel-content">
          <li onClick={() => this.props.makeProfitCenterSelection('1')}>Profit Center 1</li>
          <li onClick={() => this.props.makeProfitCenterSelection('2')}>Profit Center 2</li>
          <li onClick={() => this.props.makeProfitCenterSelection('3')}>Profit Center 3</li>
        </ul>
      </div>
    );
  }
}
