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
      <div id="master-content-container">
        <div id="primary-content-panel" className="admin-panel-container flex-item-12-12 flex-item-for-tablet-up-4-12 flex-item-for-desktop-up-3-12">
          <div className="admin-panel-toolbar">
            <input className="admin-panel-searchbar-tree" type="search" placeholder="Filter Clients" />
            <div className="admin-panel-action-icons-container">
              <svg className="action-icon-add action-icon tooltip">
                <use xlinkHref="#add"></use>
              </svg>
            </div>
          </div>
          <div className="admin-panel-content-container">
            <ul className="admin-panel-content">
            </ul>
          </div>
        </div>
      </div>
    );
  }
}
