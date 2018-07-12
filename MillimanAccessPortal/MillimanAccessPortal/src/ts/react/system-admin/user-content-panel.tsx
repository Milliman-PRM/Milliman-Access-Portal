import '../../../scss/react/system-admin/system-admin.scss';

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
          <li>User 1</li>
          <li>User 2</li>
        </ul>
      </div>
    );
  }
}
