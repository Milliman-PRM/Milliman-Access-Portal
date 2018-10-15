import '../../../images/add.svg';
import '../../../images/client-admin.svg';
import '../../../images/email.svg';
import '../../../images/expand-card.svg';
import '../../../images/group.svg';
import '../../../images/reports.svg';
import '../../../images/user.svg';
import '../../../scss/react/system-admin/system-admin.scss';

import * as React from 'react';

import { getData, postData } from '../../shared';
import { BasicNode, BasicTree } from '../../view-models/content-publishing';
import { ColumnIndicator } from '../shared-components/column-selector';
import { ContentPanel, ContentPanelAttributes } from '../shared-components/content-panel';
import { Entity } from '../shared-components/entity';
import { QueryFilter, RoleEnum } from '../shared-components/interfaces';
import { NavBar } from '../shared-components/navbar';
import {
  ClientInfo, Detail, EntityInfo, EntityInfoCollection, PrimaryDetail, ProfitCenterInfo,
  RootContentItemInfo, SecondaryDetail, UserInfo,
} from './interfaces';
import { PrimaryDetailPanel } from './primary-detail-panel';
import { SecondaryDetailPanel } from './secondary-detail-panel';

interface ToggleInfo {
  checked: boolean;
  disabled: boolean;
}
export interface SystemAdminState {
  data: {
    primaryEntities: EntityInfoCollection;
    secondaryEntities: EntityInfoCollection;
  };
  primaryDataSource: SystemAdminColumn;
  secondaryDataSource: SystemAdminColumn;
  primarySelectedCard: string;
  secondarySelectedCard: string;
  primaryContentPanel: {
    filterText: string;
    modalOpen: boolean;
  };
  secondaryContentPanel: {
    filterText: string;
    modalOpen: boolean;
  };
  primaryDetail: PrimaryDetail;
  secondaryDetail: SecondaryDetail;
  toggles: {
    systemAdmin: ToggleInfo;
    userSuspend: ToggleInfo;
    userClient: {
      [index: number]: ToggleInfo;
    };
    contentSuspend: ToggleInfo;
  };
}

export enum SystemAdminColumn {
  USER = 'user',
  CLIENT = 'client',
  PROFIT_CENTER = 'profitCenter',
  ROOT_CONTENT_ITEM = 'rootContentItem',
}

export class SystemAdmin extends React.Component<{}, SystemAdminState> {
  private controller: string = 'SystemAdmin';
  private readonly currentView: string = document
    .getElementsByTagName('body')[0].getAttribute('data-nav-location');

  public constructor(props) {
    super(props);

    this.state = {
      data: {
        primaryEntities: [],
        secondaryEntities: [],
      },
      primaryDataSource: null,
      secondaryDataSource: null,
      primarySelectedCard: null,
      secondarySelectedCard: null,
      primaryContentPanel: {
        filterText: '',
        modalOpen: false,
      },
      secondaryContentPanel: {
        filterText: '',
        modalOpen: false,
      },
      primaryDetail: null,
      secondaryDetail: null,
      toggles: {
        systemAdmin: {
          checked: false,
          disabled: true,
        },
        userSuspend: {
          checked: false,
          disabled: true,
        },
        userClient: {
          1: {
            checked: false,
            disabled: true,
          },
          3: {
            checked: false,
            disabled: true,
          },
          4: {
            checked: false,
            disabled: true,
          },
          5: {
            checked: false,
            disabled: true,
          },
        },
        contentSuspend: {
          checked: false,
          disabled: true,
        },
      },
    };

    this.setPrimaryDataSource = this.setPrimaryDataSource.bind(this);
    this.setSecondaryDataSource = this.setSecondaryDataSource.bind(this);
    this.setPrimarySelectedCard = this.setPrimarySelectedCard.bind(this);
    this.setSecondarySelectedCard = this.setSecondarySelectedCard.bind(this);
    this.fetchPrimaryEntities = this.fetchPrimaryEntities.bind(this);
    this.fetchSecondaryEntities = this.fetchSecondaryEntities.bind(this);
    this.cancelPublicationRequest = this.cancelPublicationRequest.bind(this);
    this.cancelReductionTask = this.cancelReductionTask.bind(this);
  }

  public componentDidMount() {
    this.setPrimaryDataSource('user');
  }

  public componentDidUpdate() {
    if (this.state.data.primaryEntities === null) {
      this.fetchPrimaryEntities();
    }
    if (this.state.primaryDetail === null && this.state.primarySelectedCard) {
      this.fetchPrimaryDetail();
      if (this.state.primaryDataSource === 'user') {
        this.fetchSystemAdmin();
        this.fetchSuspendUser();
      }
    }
    if (this.state.data.secondaryEntities === null && this.state.secondaryDataSource) {
      this.fetchSecondaryEntities();
    }
    if (this.state.secondaryDetail === null && this.state.secondarySelectedCard) {
      this.fetchSecondaryDetail();
      if ((this.state.primaryDataSource === 'client' && this.state.secondaryDataSource === 'user')
      || (this.state.primaryDataSource === 'user' && this.state.secondaryDataSource === 'client')) {
        this.fetchUserClient(RoleEnum.Admin);
        this.fetchUserClient(RoleEnum.ContentPublisher);
        this.fetchUserClient(RoleEnum.ContentAccessAdmin);
        this.fetchUserClient(RoleEnum.ContentUser);
      }
      if (this.state.secondaryDataSource === 'rootContentItem') {
        this.fetchSuspendContent();
      }
    }
  }

  public render() {
    const secondaryQueryFilter = this.getSecondaryQueryFilter();

    const secondaryColumnComponent = this.state.primarySelectedCard
      ? (
        <ContentPanel
          {...this.state.secondaryContentPanel}
          onFilterTextChange={this.handleSecondaryFilterKeyup}
          onModalOpen={this.handleSecondaryModalOpen}
          onModalClose={this.handleSecondaryModalClose}
          createAction={this.getCreateAction(this.state.secondaryDataSource, this.state.primaryDataSource)}
          columns={this.getColumns(this.state.primaryDataSource)}
          onColumnSelect={this.setSecondaryDataSource}
          selectedColumn={this.getColumns(this.state.primaryDataSource).filter((c) => c.id === this.state.secondaryDataSource)[0]}
          onCardSelect={this.setSecondarySelectedCard}
          selectedCard={this.state.secondarySelectedCard}
          queryFilter={secondaryQueryFilter}
          entities={this.state.data.secondaryEntities}
        />
      )
      : null;
    return (
      <>
        <NavBar
          currentView={this.currentView}
        />
        <ContentPanel
          {...this.state.primaryContentPanel}
          onFilterTextChange={this.handlePrimaryFilterKeyup}
          onModalOpen={this.handlePrimaryModalOpen}
          onModalClose={this.handlePrimaryModalClose}
          createAction={this.getCreateAction(this.state.primaryDataSource)}
          columns={this.getColumns()}
          onColumnSelect={this.setPrimaryDataSource}
          selectedColumn={this.getColumns().filter((c) => c.id === this.state.primaryDataSource)[0]}
          onCardSelect={this.setPrimarySelectedCard}
          selectedCard={this.state.primarySelectedCard}
          queryFilter={this.getPrimaryQueryFilter()}
          entities={this.state.data.primaryEntities}
        />
        {secondaryColumnComponent}
        <div
          className="admin-panel-container flex-item-12-12 flex-item-for-tablet-up-4-12 flex-item-for-desktop-up-6-12"
          style={{overflowY: 'auto'}}
        >
          <PrimaryDetailPanel
            selectedCard={this.state.primarySelectedCard}
            selectedColumn={this.state.primaryDataSource}
            queryFilter={secondaryQueryFilter}
            detail={this.state.primaryDetail}
            onPushSystemAdmin={this.pushSystemAdmin}
            checkedSystemAdmin={this.state.toggles.systemAdmin.checked}
            onPushSuspend={this.pushSuspendUser}
            checkedSuspended={this.state.toggles.userSuspend.checked}
          />
          <SecondaryDetailPanel
            selectedCard={this.state.secondarySelectedCard}
            primarySelectedColumn={this.state.primaryDataSource}
            secondarySelectedColumn={this.state.secondaryDataSource}
            queryFilter={this.getFinalQueryFilter()}
            detail={this.state.secondaryDetail}
            onCancelPublication={this.cancelPublicationRequest}
            onCancelReduction={this.cancelReductionTask}
            checkedClientAdmin={this.state.toggles.userClient[RoleEnum.Admin].checked}
            checkedContentPublisher={this.state.toggles.userClient[RoleEnum.ContentPublisher].checked}
            checkedAccessAdmin={this.state.toggles.userClient[RoleEnum.ContentAccessAdmin].checked}
            checkedContentUser={this.state.toggles.userClient[RoleEnum.ContentUser].checked}
            checkedSuspended={this.state.toggles.contentSuspend.checked}
            onPushUserClient={this.pushUserClient}
            onPushSuspend={this.pushSuspendContent}
          />
        </div>
      </>
    );
  }

  // callbacks for child components
  private setPrimaryDataSource(sourceName: string) {
    this.setState((prevState) => {
      if (sourceName === prevState.primaryDataSource) {
        return {};
      }

      return {
        ...prevState,
        data: {
          ...prevState.data,
          primaryEntities: null,
          secondaryEntities: null,
        },
        primaryDataSource: sourceName,
        secondaryDataSource: null,
        primarySelectedCard: null,
        secondarySelectedCard: null,
        primaryDetail: null,
        secondaryDetail: null,
      };
    });
  }

  private setSecondaryDataSource(sourceName: string) {
    this.setState((prevState) => {
      if (sourceName === prevState.secondaryDataSource) {
        return {};
      }

      return {
        ...prevState,
        data: {
          ...prevState.data,
          secondaryEntities: null,
        },
        secondaryDataSource: sourceName,
        secondarySelectedCard: null,
        secondaryDetail: null,
      };
    });
  }

  private setPrimarySelectedCard(id: string) {
    this.setState((prevState) => {
      const defaultSecondaryDataSource = this.getColumns(prevState.primaryDataSource)[0].id;
      return {
        data: {
          ...prevState.data,
          secondaryEntities: null,
        },
        primarySelectedCard: prevState.primarySelectedCard === id
          ? null
          : id,
        secondaryDataSource: prevState.secondaryDataSource || (defaultSecondaryDataSource as SystemAdminColumn),
        secondarySelectedCard: null,
        primaryDetail: null,
        secondaryDetail: null,
      };
    });
  }

  private setSecondarySelectedCard(cardId: string) {
    this.setState((prevState) => ({
      secondarySelectedCard: prevState.secondarySelectedCard === cardId
        ? null
        : cardId,
      secondaryDetail: null,
    }));
  }

  // utility methods
  private getColumns(parent: SystemAdminColumn = null): ColumnIndicator[] {
    switch (parent) {
      case null:
        return [
          {
            id: SystemAdminColumn.USER,
            name: 'Users',
          },
          {
            id: SystemAdminColumn.CLIENT,
            name: 'Clients',
          },
          {
            id: SystemAdminColumn.PROFIT_CENTER,
            name: 'Profit Centers',
          },
        ];
      case SystemAdminColumn.USER:
        return [
          {
            id: SystemAdminColumn.CLIENT,
            name: 'Clients',
          },
          {
            id: SystemAdminColumn.ROOT_CONTENT_ITEM,
            name: 'Content',
          },
        ];
      case SystemAdminColumn.CLIENT:
        return [
          {
            id: SystemAdminColumn.USER,
            name: 'Users',
          },
          {
            id: SystemAdminColumn.ROOT_CONTENT_ITEM,
            name: 'Content Items',
          },
        ];
      case SystemAdminColumn.PROFIT_CENTER:
        return [
          {
            id: SystemAdminColumn.USER,
            name: 'Authorized Users',
          },
          {
            id: SystemAdminColumn.CLIENT,
            name: 'Clients',
          },
        ];
      default:
        return [];
    }
  }

  private getDataAction(column: SystemAdminColumn) {
    switch (column) {
      case SystemAdminColumn.USER:
        return 'Users';
      case SystemAdminColumn.CLIENT:
        return 'Clients';
      case SystemAdminColumn.PROFIT_CENTER:
        return 'ProfitCenters';
      case SystemAdminColumn.ROOT_CONTENT_ITEM:
        return 'RootContentItems';
      default:
        throw new Error(`'${column}' is not a valid column.`);
    }
  }

  private getDetailAction(column: SystemAdminColumn) {
    switch (column) {
      case SystemAdminColumn.USER:
        return 'UserDetail';
      case SystemAdminColumn.CLIENT:
        return 'ClientDetail';
      case SystemAdminColumn.PROFIT_CENTER:
        return 'ProfitCenterDetail';
      case SystemAdminColumn.ROOT_CONTENT_ITEM:
        return 'RootContentItemDetail';
      default:
        throw new Error(`'${column}' is not a valid column.`);
    }
  }

  private getCreateAction(column: SystemAdminColumn, context: SystemAdminColumn = null) {
    switch (column) {
      case SystemAdminColumn.USER:
        switch (context) {
          case SystemAdminColumn.CLIENT:
            return 'AddUserToClient';
          case SystemAdminColumn.PROFIT_CENTER:
            return 'AddUserToProfitCenter';
          case null:
            return 'CreateUser';
          case SystemAdminColumn.USER:
          case SystemAdminColumn.ROOT_CONTENT_ITEM:
            return null;
          default:
            throw new Error(`'${column}' is not a valid column in context of column ${context}.`);
        }
      case SystemAdminColumn.PROFIT_CENTER:
        return 'CreateProfitCenter';
      case SystemAdminColumn.CLIENT:
      case SystemAdminColumn.ROOT_CONTENT_ITEM:
      case null:
        return null;
      default:
        throw new Error(`'${column}' is not a valid column.`);
    }
  }

  private assignQueryFilter(column: SystemAdminColumn, queryFilter: QueryFilter, id: string): QueryFilter {
    switch (column) {
      case SystemAdminColumn.USER:
        return {...queryFilter, userId: id};
      case SystemAdminColumn.CLIENT:
        return {...queryFilter, clientId: id};
      case SystemAdminColumn.PROFIT_CENTER:
        return {...queryFilter, profitCenterId: id};
      case SystemAdminColumn.ROOT_CONTENT_ITEM:
        return {...queryFilter, userId: id};
      default:
        return queryFilter;
    }
  }

  private getPrimaryQueryFilter = () => ({});
  private getSecondaryQueryFilter = () => this.assignQueryFilter(
    this.state.primaryDataSource,
    this.getPrimaryQueryFilter(),
    this.state.primarySelectedCard,
  )
  private getFinalQueryFilter = () => this.assignQueryFilter(
    this.state.secondaryDataSource,
    this.getSecondaryQueryFilter(),
    this.state.secondarySelectedCard,
  )

  // more callbacks
  private fetchPrimaryEntities() {
    getData(`/SystemAdmin/${this.getDataAction(this.state.primaryDataSource)}`, {})
    .then((response) => {
      this.setState((prevState) => ({
        data: {
          ...prevState.data,
          primaryEntities: response,
        },
      }));
    });
  }

  private fetchSecondaryEntities() {
    getData(`/SystemAdmin/${this.getDataAction(this.state.secondaryDataSource)}`, this.getSecondaryQueryFilter())
    .then((response) => {
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          secondaryEntities: response,
        },
      }));
    });
  }

  private fetchPrimaryDetail() {
    if (!this.state.primarySelectedCard) {
      return;
    }

    getData(`/SystemAdmin/${this.getDetailAction(this.state.primaryDataSource)}`, this.getSecondaryQueryFilter())
    .then((response) => this.setState({
      primaryDetail: response,
    }));
  }

  private fetchSecondaryDetail() {
    if (!this.state.secondarySelectedCard) {
      return;
    }
    getData(`/SystemAdmin/${this.getDetailAction(this.state.secondaryDataSource)}`, this.getFinalQueryFilter())
    .then((response) => this.setState({
      secondaryDetail: response,
    }));
  }

  private cancelPublicationRequest(event: React.MouseEvent<HTMLAnchorElement>) {
    event.preventDefault();
    postData(
      '/SystemAdmin/CancelPublication',
      { rootContentItemId: this.getFinalQueryFilter().rootContentItemId },
    ).then(() => {
      alert('Publication canceled.');
      this.setState((prevState) => ({
        ...prevState,
        secondaryDetail: null,
      }));
    });
  }

  private cancelReductionTask(event: React.MouseEvent<HTMLAnchorElement>, id: string) {
    event.preventDefault();
    postData(
      '/SystemAdmin/CancelReduction',
      { selectionGroupId: id },
    ).then(() => {
      alert('Reduction canceled.');
      this.setState((prevState) => ({
        ...prevState,
        secondaryDetail: null,
      }));
    });
  }

  private fetchSystemAdmin = () => {
    getData('/SystemAdmin/SystemRole', Object.assign({}, this.getSecondaryQueryFilter(), { role: RoleEnum.Admin }))
    .then((response: boolean) => {
      this.setState((prevState) => ({
        ...prevState,
        toggles: {
          ...prevState.toggles,
          systemAdmin: {
            checked: response,
            disabled: false,
          },
        },
      }));
    });
  }

  private pushSystemAdmin = () => {
    if (this.state.toggles.systemAdmin.disabled) {
      return;
    }

    this.setState((prevState) => ({
      ...prevState,
      toggles: {
        ...prevState.toggles,
        systemAdmin: {
          ...prevState.toggles.systemAdmin,
          disabled: true,
        },
      },
    }));

    postData('/SystemAdmin/SystemRole', Object.assign({}, this.getSecondaryQueryFilter(), { role: RoleEnum.Admin }, {
      value: !this.state.toggles.systemAdmin.checked,
    }))
    .then((response: boolean) => {
      this.setState((prevState) => ({
        ...prevState,
        toggles: {
          ...prevState.toggles,
          systemAdmin: {
            checked: response,
            disabled: false,
          },
        },
      }));
    });
  }

  private fetchSuspendUser = () => {
    getData('/SystemAdmin/UserSuspendedStatus', Object.assign({}, this.getSecondaryQueryFilter()))
    .then((response: boolean) => {
      this.setState((prevState) => ({
        ...prevState,
        toggles: {
          ...prevState.toggles,
          userSuspend: {
            checked: response,
            disabled: false,
          },
        },
      }));
    });
  }

  private pushSuspendUser = () => {
    if (this.state.toggles.userSuspend.disabled) {
      return;
    }

    this.setState((prevState) => ({
      ...prevState,
      toggles: {
        ...prevState.toggles,
        userSuspend: {
          ...prevState.toggles.userSuspend,
          disabled: true,
        },
      },
    }));

    postData('/SystemAdmin/UserSuspendedStatus', Object.assign({}, this.getSecondaryQueryFilter(), {
      value: !this.state.toggles.userSuspend.checked,
    }))
    .then((response: boolean) => {
      this.setState((prevState) => ({
        ...prevState,
        toggles: {
          ...prevState.toggles,
          userSuspend: {
            checked: response,
            disabled: false,
          },
        },
      }));
    });
  }

  private fetchUserClient = (role: RoleEnum) => {
    getData('/SystemAdmin/UserClientRoleAssignment', Object.assign({}, this.getFinalQueryFilter(), { role }))
    .then((response: boolean) => {
      this.setState((prevState) => {
        const userClient = {...prevState.toggles.userClient};
        userClient[role] = {
          checked: response,
          disabled: false,
        };
        return {
          ...prevState,
          toggles: {
            ...prevState.toggles,
            userClient,
          },
        };
      });
    });
  }

  private pushUserClient = (_, role: RoleEnum) => {
    if (this.state.toggles.userClient[role].disabled) {
      return;
    }

    this.setState((prevState) => {
      const userClient = {...prevState.toggles.userClient};
      userClient[role] = {
        ...userClient[role],
        disabled: true,
      };
      return {
        ...prevState,
        toggles: {
          ...prevState.toggles,
          userClient,
        },
      };
    });

    postData('/SystemAdmin/UserClientRoleAssignment', Object.assign({}, this.getFinalQueryFilter(), { role }, {
      value: !this.state.toggles.userClient[role].checked,
    }))
    .then((response: boolean) => {
      this.setState((prevState) => {
        const userClient = {...prevState.toggles.userClient};
        userClient[role] = {
          checked: response,
          disabled: false,
        };
        return {
          ...prevState,
          toggles: {
            ...prevState.toggles,
            userClient,
          },
        };
      });
    });
  }

  private fetchSuspendContent = () => {
    getData('/SystemAdmin/ContentSuspendedStatus', Object.assign({}, this.getFinalQueryFilter()))
    .then((response: boolean) => {
      this.setState((prevState) => ({
        ...prevState,
        toggles: {
          ...prevState.toggles,
          contentSuspend: {
            checked: response,
            disabled: false,
          },
        },
      }));
    });
  }

  private pushSuspendContent = () => {
    if (this.state.toggles.contentSuspend.disabled) {
      return;
    }

    this.setState((prevState) => ({
      ...prevState,
      toggles: {
        ...prevState.toggles,
        contentSuspend: {
          ...prevState.toggles.contentSuspend,
          disabled: true,
        },
      },
    }));

    postData('/SystemAdmin/ContentSuspendedStatus', Object.assign({}, this.getFinalQueryFilter(), {
      value: !this.state.toggles.contentSuspend.checked,
    }))
    .then((response: boolean) => {
      this.setState((prevState) => ({
        ...prevState,
        toggles: {
          ...prevState.toggles,
          contentSuspend: {
            checked: response,
            disabled: false,
          },
        },
      }));
    });
  }

  private handlePrimaryFilterKeyup = (filterText: string) => {
    this.setState((prevState) => ({
      primaryContentPanel: {
        ...prevState.primaryContentPanel,
        filterText,
      },
    }));
  }

  private handleSecondaryFilterKeyup = (filterText: string) => {
    this.setState((prevState) => ({
      secondaryContentPanel: {
        ...prevState.secondaryContentPanel,
        filterText,
      },
    }));
  }

  private handlePrimaryModalOpen = () => {
    this.setState((prevState) => ({
      primaryContentPanel: {
        ...prevState.primaryContentPanel,
        modalOpen: true,
      },
    }));
  }

  private handlePrimaryModalClose = () => {
    this.setState((prevState) => ({
      primaryContentPanel: {
        ...prevState.primaryContentPanel,
        modalOpen: false,
      },
    }));
  }

  private handleSecondaryModalOpen = () => {
    this.setState((prevState) => ({
      secondaryContentPanel: {
        ...prevState.secondaryContentPanel,
        modalOpen: true,
      },
    }));
  }

  private handleSecondaryModalClose = () => {
    this.setState((prevState) => ({
      secondaryContentPanel: {
        ...prevState.secondaryContentPanel,
        modalOpen: false,
      },
    }));
  }
}
