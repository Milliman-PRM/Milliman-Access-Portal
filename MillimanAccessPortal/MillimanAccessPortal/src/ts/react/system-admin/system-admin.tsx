import '../../../scss/react/system-admin/system-admin.scss';

import * as React from 'react';

import { SystemAdminState } from './interfaces';

export class SystemAdmin extends React.Component<{}, SystemAdminState> {
  public constructor(props) {
    super(props);
    this.state = {
      primaryColumnSelection: 'Users',
    };
  }

  public render() {
    return (
      <div>
        <h1>System Admin</h1>
      </div>
    );
  }
}
