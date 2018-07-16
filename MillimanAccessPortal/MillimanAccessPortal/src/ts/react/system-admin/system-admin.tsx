import '../../../images/add.svg';
import '../../../scss/react/system-admin/system-admin.scss';

import * as React from 'react';

import { ActionIcon } from '../shared-components/action-icon';
import { ColumnSelector } from '../shared-components/column-selector';
import { Filter } from '../shared-components/filter';
import { SelectionOption } from '../shared-components/interfaces';
import { ClientContentPanel } from './client-content-panel';
import { SystemAdminState } from './interfaces';
import { ProfitCenterContentPanel } from './profit-center-content-panel';
import { UserContentPanel } from './user-content-panel';
import { UserInfo } from '../../view-models/content-publishing';

export class SystemAdmin extends React.Component<{}, SystemAdminState> {
  public constructor(props) {
    super(props);
    this.state = {
      primaryColContent: 'Users',
      primaryColContentLabel: 'Users',
      primaryColSelection: null,
      primaryColFilter: null,
      secondaryColContent: 'Clients',
      secondaryColContentLabel: 'Clients',
      secondaryColSelection: null,
      secondaryColFilter: null,
      addUserDialog: false,
      userData: [],
    };

    this.setUserData = this.setUserData.bind(this);
  }

  private structure = {
    Users: {
      displayValue: 'Users',
      secColElements: {
        Clients: {
          displayValue: 'Clients',
        },
        AuthContent: {
          displayValue: 'Authorized Content',
        }
      }
    },
    Clients: {
      displayValue: 'Clients',
      secColElements: {
        Users: {
          displayValue: 'Users',
        },
        Content: {
          displayValue: 'Content Items',
        }
      }
    },
    PC: {
      displayValue: 'Profit Centers',
      secColElements: {
        AuthUsers: {
          displayValue: 'Authorized Users',
        },
        Clients: {
          displayValue: 'Clients',
        }
      }
    }
  }

  public selectPrimaryColumn = (colContentSelection: SelectionOption) => {
    if (colContentSelection.value !== this.state.primaryColContent) {
      const newSecondaryColContent = Object.keys(this.structure[colContentSelection.value].secColElements)[0];
      this.setState({
        primaryColContent: colContentSelection.value,
        primaryColContentLabel: colContentSelection.label,
        primaryColSelection: null,
        secondaryColContent: newSecondaryColContent,
        secondaryColContentLabel: this.structure[colContentSelection.value].secColElements[newSecondaryColContent].displayValue,
        secondaryColSelection: null,
        primaryColFilter: null,
        secondaryColFilter: null
      });
    }
  }

  public selectSecondaryColumn = (colContentSelection: SelectionOption) => {
    if (colContentSelection.value !== this.state.secondaryColContent) {
      this.setState({
        secondaryColContent: colContentSelection.value,
        secondaryColContentLabel: colContentSelection.label,
      });
    }
  }

  public updatePrimaryColumnFilter = (filterString: string) => {
    this.setState({ primaryColFilter: filterString });
  }

  public updateSecondaryColumnFilter = (filterString: string) => {
    this.setState({ secondaryColFilter: filterString });
  }

  public makePrimaryColumnSelection = (id: string) => {
    this.setState({ primaryColSelection: id });
  }

  public makeSecondaryColumnSelection = (id: string) => {
    this.setState({ secondaryColSelection: id });
  }

  public addUser = () => {
    console.log('Add User');
  }

  public addPC = () => {
    console.log('Add Profit Center');
  }

  public setUserData(data: UserInfo[]) {
    this.setState({
      userData: data,
    });
  }

  public render() {

    // Define the primary column options
    const primaryColOptions = Object.keys(this.structure).map((property) => {
      return { value: property, label: this.structure[property].displayValue };
    });

    const secondaryColOptions = Object.keys(this.structure[this.state.primaryColContent].secColElements).map((property) => {
      return { value: property, label: this.structure[this.state.primaryColContent].secColElements[property].displayValue };
    })

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
              <UserContentPanel
                selectedUser={this.state.primaryColSelection}
                makeUserSelection={this.makePrimaryColumnSelection}
                users={this.state.userData}
                onFetch={this.setUserData}
              />
            ) : null
          }
          {
            (this.state.primaryColContent === 'Clients') ? (
              <ClientContentPanel
                selectedClient={this.state.primaryColSelection}
                makeClientSelection={this.makePrimaryColumnSelection}
              />
            ) : null
          }
          {
            (this.state.primaryColContent === 'PC') ? (
              <ProfitCenterContentPanel
                selectedProfitCenter={this.state.primaryColSelection}
                makeProfitCenterSelection={this.makePrimaryColumnSelection}
              />
            ) : null
          }
        </div>
        {
          (this.state.primaryColSelection) ? (
            <div id="primary-content-panel" className="admin-panel-container flex-item-12-12 flex-item-for-tablet-up-4-12 flex-item-for-desktop-up-3-12">
              <ColumnSelector
                colContentOptions={secondaryColOptions}
                colContent={this.state.secondaryColContent}
                colContentSelection={this.selectSecondaryColumn}
              />
              <div className="admin-panel-toolbar">
                <Filter
                  filterText={this.state.secondaryColFilter}
                  updateFilterString={this.updateSecondaryColumnFilter}
                  placeholderText={`Filter ${this.state.secondaryColContentLabel}`}
                />
                <div className="admin-panel-action-icons-container">
                  {
                    (this.state.primaryColContent === 'Clients' && this.state.secondaryColContent === 'Users') ? (
                      <ActionIcon title="Add User" action={this.addUser} icon="add" />
                    ) : null
                  }
                  {
                    (this.state.primaryColContent === 'PC' && this.state.secondaryColContent === 'AuthUsers') ? (
                      <ActionIcon title="Add Profit Center" action={this.addPC} icon="add" />
                    ) : null
                  }
                </div>
              </div>
            </div>
          ) : null
        }
      </div>
    );
  }
}
