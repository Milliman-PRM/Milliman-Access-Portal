import '../../../scss/react/system-admin/system-admin.scss';

import * as React from 'react';

import { SystemAdminState } from './interfaces';

export class SystemAdmin extends React.Component<{}, SystemAdminState> {
  public constructor(props) {
    super(props);
    this.state = {
      primaryColContent: 'User',
      primaryColSelection: null,
      primaryColFilter: null,
      secondaryColContent: null,
      secondaryColSelection: null,
      secondaryColFilter: null,
      addUserDialog: false
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
