import '../../../scss/react/shared-components/content-panel.scss';

import * as React from 'react';

import { ClientPanelProps, ClientPanelState } from './interfaces';

export class ClientContentPanel extends React.Component<ClientPanelProps, ClientPanelState> {
  public constructor(props) {
    super(props);
    this.state = {
      clientList: []
    };
  }

  public render() {
    return (
      <div className="admin-panel-content-container">
        <ul className="admin-panel-content">
          <li onClick={() => this.props.makeClientSelection('1')}>Client 1</li>
          <li onClick={() => this.props.makeClientSelection('2')}>Client 2</li>
          <li onClick={() => this.props.makeClientSelection('3')}>Client 3</li>
        </ul>
      </div>
    );
  }
}
