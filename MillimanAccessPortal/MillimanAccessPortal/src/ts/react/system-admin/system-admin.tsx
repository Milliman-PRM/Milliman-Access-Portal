import '../../../scss/react/system-admin/system-admin.scss';

import * as React from 'react';

import { ColumnSelector } from '../shared-components/column-selector';
import { Filter } from '../shared-components/filter';
import { ActionIcon } from '../shared-components/action-icon';
import { UserContentPanel } from './user-content-panel';

import { SystemAdminState } from './interfaces';
import { SelectionOption } from '../shared-components/interfaces';

import '../../../images/add.svg';

export class SystemAdmin extends React.Component<{}, SystemAdminState> {
  public constructor(props) {
    super(props);
    this.state = {
      primaryColContent: 'Users',
      primaryColContentLabel: 'Users',
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
          displayValue: 'Clients',
          panel: 'ClientPanel',
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

  public selectPrimaryColumn = (colContentSelection: SelectionOption) => {
    if (colContentSelection.value !== this.state.primaryColContent) {
      this.setState({
        primaryColContent: colContentSelection.value,
        primaryColContentLabel: colContentSelection.label, 
        secondaryColContent: null,
        primaryColFilter: null,
        secondaryColFilter: null
      });
    }
  }

  public updatePrimaryColumnFilter = (filterString: string) => {
    this.setState({ primaryColFilter: filterString });
  }

  public addUser = () => {
    console.log('Add User');
  }

  public addPC = () => {
    console.log('Add Profit Center');
  }

  public render() {

    // Define the primary column options
    const primaryColOptions = Object.keys(this.state.structure).map((property) => {
      return { value: property, label: this.state.structure[property].displayValue };
    });

    return (
      <div id="master-content-container">
        <div id="primary-content-panel" className="admin-panel-container flex-item-12-12 flex-item-for-tablet-up-4-12 flex-item-for-desktop-up-3-12">
          <ColumnSelector
            colContentOptions={primaryColOptions}
            colContent={this.state.primaryColContent}
            colContentSelection={this.selectPrimaryColumn}
          />
          <div className="admin-panel-toolbar">
            <Filter
              filterText={this.state.primaryColFilter}
              updateFilterString={this.updatePrimaryColumnFilter}
              placeholderText={`Filter ${this.state.primaryColContentLabel}`}
            />
            <div className="admin-panel-action-icons-container">
              {
                (this.state.primaryColContent === 'Users') ? (
                  <ActionIcon title="Add User" action={this.addUser} icon="add" />
                ) : null
              }
              {
                (this.state.primaryColContent === 'PC') ? (
                  <ActionIcon title="Add Profit Center" action={this.addPC} icon="add" />
                ) : null
              }
            </div>
          </div>
          {
            (this.state.primaryColContent === 'Users') ? (
              <UserContentPanel selectedUser={this.state.structure[this.state.primaryColContent].selectedInstance} />
            ) : null
          }
        </div>
      </div>
    );
  }
}
