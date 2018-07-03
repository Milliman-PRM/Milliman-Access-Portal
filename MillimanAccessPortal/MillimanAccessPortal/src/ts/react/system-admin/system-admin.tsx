import '../../../scss/react/system-admin/system-admin.scss';

import * as React from 'react';

import { ColumnSelector } from '../shared-components/column-selector';
import { SystemAdminState } from './interfaces';

export class SystemAdmin extends React.Component<{}, SystemAdminState> {
  public constructor(props) {
    super(props);
    this.state = {
      primaryColContent: 'Users',
      primaryColSelection: null,
      primaryColFilter: null,
      secondaryColContent: null,
      secondaryColSelection: null,
      secondaryColFilter: null,
      addUserDialog: false
    };
  }

  public selectColumn = (colContentSelection: string, primaryCol: boolean) => {
    if (primaryCol) {
      this.setState({
        primaryColContent: colContentSelection,
        secondaryColContent: null
      });
    } else {
      this.setState({ secondaryColContent: colContentSelection });
    }
  }

  private columnSelectionOptions = {
    'Users': ['Clients', 'Authorized Content'],
    'Clients': ['Users', 'Content Items'],
    'PCs': ['Authorized Users', 'Clients']
  }

  public render() {
    return (
      <div id="master-content-container">
        <div id="primary-content-panel" className="admin-panel-container flex-item-12-12 flex-item-for-tablet-up-4-12 flex-item-for-desktop-up-3-12">
          <ColumnSelector
            colContentOptions={Object.keys(this.columnSelectionOptions)}
            colContent={this.state.primaryColContent}
            colContentSelection={this.selectColumn}
            primaryColumn={true}
          />
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
