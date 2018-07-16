import '../../../scss/react/shared-components/content-panel.scss';

import * as React from 'react';

import { UserPanelProps, UserPanelState } from './interfaces';

export class UserContentPanel extends React.Component<UserPanelProps, UserPanelState> {
  public constructor(props) {
    super(props);
    this.state = {
      userList: []
    };
  }

  public render() {
    return (
      <div className="admin-panel-content-container">
        <ul className="admin-panel-content">
          <li onClick={() => this.props.makeUserSelection('1')}>User 1</li>
          <li onClick={() => this.props.makeUserSelection('2')}>User 2</li>
          <li onClick={() => this.props.makeUserSelection('3')}>User 3</li>
        </ul>
      </div>
    );
  }
}
