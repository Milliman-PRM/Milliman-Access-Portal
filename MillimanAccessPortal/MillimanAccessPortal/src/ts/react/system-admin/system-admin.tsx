import '../../../images/add.svg';
import '../../../scss/react/system-admin/system-admin.scss';

import * as React from 'react';

import { QueryFilter } from '../shared-components/interfaces';
import { ClientContentPanel } from './content-panel/client-content-panel';
import { ProfitCenterContentPanel } from './content-panel/profit-center-content-panel';
import { RootContentPanel } from './content-panel/root-content-panel';
import { UserContentPanel } from './content-panel/user-content-panel';
import { Column } from './interfaces';

export interface SystemAdminState {
  selectedPrimaryColumn: Column;
  selectedSecondaryColumn: Column;
  secondaryQueryFilter: QueryFilter;
  finalQueryFilter: QueryFilter;
}

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
  private controller: string = 'SystemAdmin';

  public constructor(props) {
    super(props);

    this.state = {
      finalQueryFilter: {},
      secondaryQueryFilter: {},
      selectedPrimaryColumn: Column.User,
      selectedSecondaryColumn: Column.Undefined,
    };

    this.setSecondaryQueryFilter = this.setSecondaryQueryFilter.bind(this);
    this.setFinalQueryFilter = this.setFinalQueryFilter.bind(this);
    this.setSelectedPrimaryColumn = this.setSelectedPrimaryColumn.bind(this);
    this.setSelectedSecondaryColumn = this.setSelectedSecondaryColumn.bind(this);
  }

  public render() {
    /*
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
              setQueryFilter={() => {}}
              queryFilter={{}}
              controller={this.controller}
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
    */

    return [
      (
        <RootContentPanel
          controller={this.controller}
          setQueryFilter={this.setSecondaryQueryFilter}
          queryFilter={{}}
          setSelectedColumn={this.setSelectedPrimaryColumn}
          selectedColumn={this.state.selectedPrimaryColumn}
        />
      ),
      this.renderSecondaryContentPanel(),
    ];
  }

  private renderSecondaryContentPanel(): JSX.Element {
    switch (this.state.selectedPrimaryColumn) {
      case Column.User:
        return (
          <UserContentPanel
            controller={this.controller}
            setQueryFilter={this.setFinalQueryFilter}
            queryFilter={this.state.secondaryQueryFilter}
            setSelectedColumn={this.setSelectedSecondaryColumn}
            selectedColumn={this.state.selectedSecondaryColumn}
          />
        );
      case Column.Client:
        return (
          <ClientContentPanel
            controller={this.controller}
            setQueryFilter={this.setFinalQueryFilter}
            queryFilter={this.state.secondaryQueryFilter}
            setSelectedColumn={this.setSelectedSecondaryColumn}
            selectedColumn={this.state.selectedSecondaryColumn}
          />
        );
      case Column.ProfitCenter:
        return (
          <ProfitCenterContentPanel
            controller={this.controller}
            setQueryFilter={this.setFinalQueryFilter}
            queryFilter={this.state.secondaryQueryFilter}
            setSelectedColumn={this.setSelectedSecondaryColumn}
            selectedColumn={this.state.selectedSecondaryColumn}
          />
        );
      default:
        return (
          <div />
        );
    }
  }

  private setSecondaryQueryFilter(queryFilter: QueryFilter) {
    this.setState({
      secondaryQueryFilter: queryFilter,
    });
  }

  private setFinalQueryFilter(queryFilter: QueryFilter) {
    this.setState({
      finalQueryFilter: queryFilter,
    });
  }

  private setSelectedPrimaryColumn(column: Column) {
    this.setState({
      selectedPrimaryColumn: column,
    });
  }

  private setSelectedSecondaryColumn(column: Column) {
    this.setState({
      selectedSecondaryColumn: column,
    });
  }
}
