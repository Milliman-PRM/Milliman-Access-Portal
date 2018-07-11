import '../../../scss/react/system-admin/system-admin.scss';

import * as React from 'react';

import { ColumnSelector } from '../shared-components/column-selector';
import { Filter } from '../shared-components/filter';
import { SystemAdminState } from './interfaces';

export class SystemAdmin extends React.Component<{}, SystemAdminState> {
  public constructor(props) {
    super(props);
    this.state = {
      primaryColContent: 'Users',
      primaryColFilter: null,
      secondaryColContent: null,
      secondaryColFilter: null,
      addUserDialog: false,
      structure: {
        Users: {
          displayValue: 'Users',
          panel: 'UserPanel',
          selectedInstance: null,
          secColElements: {
            Clients: {
              displayValue: 'Clients',
              panel: 'ClientsPanel',
              selectedInstance: null
            },
            AuthContent: {
              displayValue: 'Authorized Content',
              panel: 'AuthContentPanel',
              selectedInstance: null
            }
          }
        },
        Clients: {
          displayValue: 'Users',
          panel: 'UserPanel',
          selectedInstance: null,
          secColElements: {
            Users: {
              displayValue: 'Users',
              panel: 'UsersPanel',
              selectedInstance: null
            },
            Content: {
              displayValue: 'Content Items',
              panel: 'ContentPanel',
              selectedInstance: null
            }
          }
        },
        PC: {
          displayValue: 'Profit Centers',
          panel: 'ProfitCentersPanel',
          selectedInstance: null,
          secColElements: {
            AuthUsers: {
              displayValue: 'Authorized Users',
              panel: 'AuthUsersPanel',
              selectedInstance: null
            },
            Clients: {
              displayValue: 'Clients',
              panel: 'ClientsPanel',
              selectedInstance: null
            }
          }
        }
      }
      
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

  public updatePrimaryColumnFilter = (filterString: string) => {
    this.setState({ primaryColFilter: filterString });
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
            <Filter
              updateFilterString={this.updatePrimaryColumnFilter}
              placeholderText={`Filter ${this.state.primaryColContent}`}
            />
            <div className="admin-panel-action-icons-container">
              <svg className="action-icon-add action-icon tooltip">
                <use xlinkHref="#add"></use>
              </svg>
            </div>
          </div>
          <h2>{this.state.primaryColFilter}</h2>
          <div className="admin-panel-content-container">
            <ul className="admin-panel-content">
            </ul>
          </div>
        </div>
      </div>
    );
  }
}
