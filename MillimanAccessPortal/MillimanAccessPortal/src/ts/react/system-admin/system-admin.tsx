import '../../../images/add.svg';
import '../../../scss/react/system-admin/system-admin.scss';

import * as React from 'react';

import { ActionIcon } from '../shared-components/action-icon';
import { ColumnSelector } from '../shared-components/column-selector';
import { Filter } from '../shared-components/filter';
import { SelectionOption } from '../shared-components/interfaces';
import { ClientContentPanel } from './client-content-panel';
import {
  ClientInfo, ProfitCenterInfo, QueryFilter, RootContentItemInfo, SystemAdminState, UserInfo,
} from './interfaces';
import { ProfitCenterContentPanel } from './profit-center-content-panel';
import { RootContentItemContentPanel } from './root-content-item-content-panel';
import { UserContentPanel } from './user-content-panel';

export class SystemAdmin extends React.Component<{}, SystemAdminState> {
  // tslint:disable:object-literal-sort-keys
  private structure = {
    Users: {
      displayValue: 'Users',
      secColElements: {
        Clients: {
          displayValue: 'Clients',
        },
        AuthContent: {
          displayValue: 'Authorized Content',
        },
      },
    },
    Clients: {
      displayValue: 'Clients',
      secColElements: {
        Users: {
          displayValue: 'Users',
        },
        Content: {
          displayValue: 'Content Items',
        },
      },
    },
    PC: {
      displayValue: 'Profit Centers',
      secColElements: {
        AuthUsers: {
          displayValue: 'Authorized Users',
        },
        Clients: {
          displayValue: 'Clients',
        },
      },
    },
  };
  // tslint:enable:object-literal-sort-keys

  public constructor(props) {
    super(props);
    this.state = {
      addUserDialog: false,
      clientData: [],
      primaryColContent: 'Users',
      primaryColContentLabel: 'Users',
      primaryColFilter: null,
      primaryColSelection: null,
      profitCenterData: [],
      rootContentItemData: [],
      secondaryColContent: 'Clients',
      secondaryColContentLabel: 'Clients',
      secondaryColFilter: null,
      secondaryColSelection: null,
      userData: [],
    };

    this.setUserData = this.setUserData.bind(this);
    this.setClientData = this.setClientData.bind(this);
    this.setProfitCenterData = this.setProfitCenterData.bind(this);
    this.setRootContentItemData = this.setRootContentItemData.bind(this);
  }

  public selectPrimaryColumn = (colContentSelection: SelectionOption) => {
    if (colContentSelection.value !== this.state.primaryColContent) {
      const newSecondaryColContent = Object.keys(this.structure[colContentSelection.value].secColElements)[0];
      this.setState({
        primaryColContent: colContentSelection.value,
        primaryColContentLabel: colContentSelection.label,
        primaryColFilter: null,
        primaryColSelection: null,
        secondaryColContent: newSecondaryColContent,
        secondaryColContentLabel: this
          .structure[colContentSelection.value]
          .secColElements[newSecondaryColContent]
          .displayValue,
        secondaryColFilter: null,
        secondaryColSelection: null,
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

  public makePrimaryColumnSelection = (id: number) => {
    this.setState((prevState) => ({
      primaryColSelection: prevState.primaryColSelection === id
        ? null
        : id,
    }));
  }

  public makeSecondaryColumnSelection = (id: number) => {
    this.setState((prevState) => ({
      secondaryColSelection: prevState.secondaryColSelection === id
        ? null
        : id,
    }));
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

  public setClientData(data: ClientInfo[]) {
    this.setState({
      clientData: data,
    });
  }

  public setProfitCenterData(data: ProfitCenterInfo[]) {
    this.setState({
      profitCenterData: data,
    });
  }

  public setRootContentItemData(data: RootContentItemInfo[]) {
    this.setState({
      rootContentItemData: data,
    });
  }

  public render() {

    // Define the primary column options
    const primaryColOptions = Object.keys(this.structure).map((property) => {
      return {
        label: this.structure[property].displayValue,
        value: property,
      };
    });

    const secondaryColOptions = Object.keys(
      this.structure[this.state.primaryColContent].secColElements,
    ).map((property) => ({
      label: this
        .structure[this.state.primaryColContent]
        .secColElements[property]
        .displayValue,
      value: property,
    }));

    const addIcon = (() => {
      switch (this.state.primaryColContent) {
        case 'Users':
          return (
            <ActionIcon
              title="Add User"
              action={this.addUser}
              icon="add"
            />
          );
        case 'PC':
          return (
            <ActionIcon
              title="Add Profit Center"
              action={this.addPC}
              icon="add"
            />
          );
        default:
          return null;
      }
    })();

    const primaryContent = (() => {
      switch (this.state.primaryColContent) {
        case 'Users':
          return (
            <UserContentPanel
              selected={this.state.primaryColSelection}
              select={this.makePrimaryColumnSelection}
              data={this.state.userData}
              onFetch={this.setUserData}
              queryFilter={{}}
            />
          );
        case 'Clients':
          return (
            <ClientContentPanel
              selected={this.state.primaryColSelection}
              select={this.makePrimaryColumnSelection}
              data={this.state.clientData}
              onFetch={this.setClientData}
              queryFilter={{}}
            />
          );
        case 'PC':
          return (
            <ProfitCenterContentPanel
              selected={this.state.primaryColSelection}
              select={this.makePrimaryColumnSelection}
              data={this.state.profitCenterData}
              onFetch={this.setProfitCenterData}
              queryFilter={{}}
            />
          );
        default:
          return null;
      }
    })();

    const secondaryAddIcon = (() => {
      if (this.state.primaryColContent === 'Clients' && this.state.secondaryColContent === 'Users') {
        return (
          <ActionIcon title="Add User" action={this.addUser} icon="add" />
        );
      }
      if (this.state.primaryColContent === 'PC' && this.state.secondaryColContent === 'AuthUsers') {
        return (
          <ActionIcon title="Add Profit Center" action={this.addPC} icon="add" />
        );
      }
      return null;
    })();

    const secondaryContent = (() => {
      switch (this.state.secondaryColContent) {
        case 'Clients':
          const queryFilter: QueryFilter = this.state.primaryColContent === 'Users'
            ? { userId: this.state.primaryColSelection }
            : { profitCenterId: this.state.primaryColSelection };
          return (
            <ClientContentPanel
              selected={this.state.secondaryColSelection}
              select={this.makeSecondaryColumnSelection}
              data={this.state.clientData}
              onFetch={this.setClientData}
              queryFilter={queryFilter}
            />
          );
        case 'AuthContent':
          return (
            <RootContentItemContentPanel
              selected={this.state.secondaryColSelection}
              select={this.makeSecondaryColumnSelection}
              data={this.state.rootContentItemData}
              onFetch={this.setRootContentItemData}
              queryFilter={{ userId: this.state.primaryColSelection }}
            />
          );
        case 'Users':
          return (
            <UserContentPanel
              selected={this.state.secondaryColSelection}
              select={this.makeSecondaryColumnSelection}
              data={this.state.userData}
              onFetch={this.setUserData}
              queryFilter={{ clientId: this.state.primaryColSelection }}
            />
          );
        case 'Content':
          return (
            <RootContentItemContentPanel
              selected={this.state.secondaryColSelection}
              select={this.makeSecondaryColumnSelection}
              data={this.state.rootContentItemData}
              onFetch={this.setRootContentItemData}
              queryFilter={{ clientId: this.state.primaryColSelection }}
            />
          );
        case 'AuthUsers':
          return (
            <UserContentPanel
              selected={this.state.secondaryColSelection}
              select={this.makeSecondaryColumnSelection}
              data={this.state.userData}
              onFetch={this.setUserData}
              queryFilter={{ profitCenterId: this.state.primaryColSelection }}
            />
          );
        default:
          return null;
      }
    })();

    const secondaryContentPanel = this.state.primaryColSelection
      ? (
        <div
          id="secondary-content-panel"
          className="admin-panel-container flex-item-12-12 flex-item-for-tablet-up-4-12 flex-item-for-desktop-up-3-12"
        >
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
              {secondaryAddIcon}
            </div>
          </div>
          {secondaryContent}
        </div>
      )
      : null;

    return (
      <div id="master-content-container">
        <div
          id="primary-content-panel"
          className="admin-panel-container flex-item-12-12 flex-item-for-tablet-up-4-12 flex-item-for-desktop-up-3-12"
        >
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
              {addIcon}
            </div>
          </div>
          {primaryContent}
        </div>
        {secondaryContentPanel}
      </div>
    );
  }
}
