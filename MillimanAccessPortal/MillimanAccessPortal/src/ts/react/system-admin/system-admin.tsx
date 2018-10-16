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
  ClientInfo, Detail, EntityInfo, EntityInfoCollection, isRootContentItemDetail, isUserClientRoles,
  isUserDetail, PrimaryDetail, ProfitCenterInfo, RootContentItemInfo, SecondaryDetail,
  UserClientRoles, UserInfo,
} from './interfaces';
import { PrimaryDetailPanel } from './primary-detail-panel';
import { SecondaryDetailPanel } from './secondary-detail-panel';

interface ToggleInfo {
  checked: boolean;
  disabled: boolean;
}
interface CardState {
  expanded: boolean;
}
interface ContentPanelState {
  selected: {
    column: SystemAdminColumn;
    card: string;
  };
  cards: {
    [id: string]: CardState;
  };
  filter: {
    text: string;
  };
  createModal: {
    open: boolean;
  };
}
export interface SystemAdminState {
  data: {
    primaryEntities: EntityInfoCollection;
    secondaryEntities: EntityInfoCollection;
    primaryDetail: PrimaryDetail;
    secondaryDetail: SecondaryDetail;
  };
  primaryPanel: ContentPanelState;
  secondaryPanel: ContentPanelState;
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
        primaryEntities: null,
        secondaryEntities: null,
        primaryDetail: null,
        secondaryDetail: null,
      },
      primaryPanel: {
        selected: {
          column: SystemAdminColumn.USER,
          card: null,
        },
        cards: null,
        filter: {
          text: '',
        },
        createModal: {
          open: false,
        },
      },
      secondaryPanel: {
        selected: {
          column: null,
          card: null,
        },
        cards: null,
        filter: {
          text: '',
        },
        createModal: {
          open: false,
        },
      },
    };
  }

  public componentDidMount() {
    this.handlePrimaryColumnSelected(SystemAdminColumn.USER);
  }

  public componentDidUpdate() {
    if (this.state.data.primaryEntities === null) {
      this.fetchPrimaryEntities();
    }
    if (this.state.data.primaryDetail === null && this.state.primaryPanel.selected.card) {
      this.fetchPrimaryDetail();
      if (this.state.primaryPanel.selected.column === SystemAdminColumn.USER) {
        this.fetchSystemAdmin();
        this.fetchSuspendUser();
      }
    }
    if (this.state.data.secondaryEntities === null && this.state.secondaryPanel.selected.column) {
      this.fetchSecondaryEntities();
    }
    if (this.state.data.secondaryDetail === null && this.state.secondaryPanel.selected.card) {
      this.fetchSecondaryDetail();
      if ((this.state.primaryPanel.selected.column === SystemAdminColumn.CLIENT
          && this.state.secondaryPanel.selected.column === SystemAdminColumn.USER)
        || (this.state.primaryPanel.selected.column === SystemAdminColumn.USER
          && this.state.secondaryPanel.selected.column === SystemAdminColumn.CLIENT)) {
        this.fetchUserClient(RoleEnum.Admin);
        this.fetchUserClient(RoleEnum.ContentPublisher);
        this.fetchUserClient(RoleEnum.ContentAccessAdmin);
        this.fetchUserClient(RoleEnum.ContentUser);
      }
      if (this.state.secondaryPanel.selected.column === SystemAdminColumn.ROOT_CONTENT_ITEM) {
        this.fetchSuspendContent();
      }
    }
  }

  public render() {
    const secondaryQueryFilter = this.getSecondaryQueryFilter();
    const secondaryColumnComponent = this.state.secondaryPanel.selected.column
      ? (
        <ContentPanel
          filterText={this.state.secondaryPanel.filter.text}
          modalOpen={this.state.secondaryPanel.createModal.open}
          onFilterTextChange={this.handleSecondaryFilterKeyup}
          onModalOpen={this.handleSecondaryModalOpen}
          onModalClose={this.handleSecondaryModalClose}
          createAction={this.getCreateAction(this.state.secondaryPanel.selected.column, this.state.primaryPanel.selected.column)}
          columns={this.getColumns(this.state.primaryPanel.selected.column)}
          onColumnSelect={this.handleSecondaryColumnSelected}
          selectedColumn={this.getColumns(this.state.primaryPanel.selected.column).filter((c) => c.id === this.state.secondaryPanel.selected.column)[0]}
          onCardSelect={this.handleSecondaryCardSelected}
          selectedCard={this.state.secondaryPanel.selected.card}
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
          filterText={this.state.primaryPanel.filter.text}
          modalOpen={this.state.primaryPanel.createModal.open}
          onFilterTextChange={this.handlePrimaryFilterKeyup}
          onModalOpen={this.handlePrimaryModalOpen}
          onModalClose={this.handlePrimaryModalClose}
          createAction={this.getCreateAction(this.state.primaryPanel.selected.column)}
          columns={this.getColumns()}
          onColumnSelect={this.handlePrimaryColumnSelected}
          selectedColumn={this.getColumns().filter((c) => c.id === this.state.primaryPanel.selected.column)[0]}
          onCardSelect={this.handlePrimaryCardSelected}
          selectedCard={this.state.primaryPanel.selected.card}
          queryFilter={this.getPrimaryQueryFilter()}
          entities={this.state.data.primaryEntities}
        />
        {secondaryColumnComponent}
        <div
          className="admin-panel-container flex-item-12-12 flex-item-for-tablet-up-4-12 flex-item-for-desktop-up-6-12"
          style={{overflowY: 'auto'}}
        >
          <PrimaryDetailPanel
            selectedCard={this.state.primaryPanel.selected.card}
            selectedColumn={this.state.primaryPanel.selected.column}
            queryFilter={secondaryQueryFilter}
            detail={this.state.data.primaryDetail}
            onPushSystemAdmin={this.pushSystemAdmin}
            checkedSystemAdmin={isUserDetail(this.state.data.primaryDetail) && this.state.data.primaryDetail.IsSystemAdmin}
            onPushSuspend={this.pushSuspendUser}
            checkedSuspended={isUserDetail(this.state.data.primaryDetail) && this.state.data.primaryDetail.IsSuspended}
          />
          <SecondaryDetailPanel
            selectedCard={this.state.secondaryPanel.selected.card}
            primarySelectedColumn={this.state.primaryPanel.selected.column}
            secondarySelectedColumn={this.state.secondaryPanel.selected.column}
            queryFilter={this.getFinalQueryFilter()}
            detail={this.state.data.secondaryDetail}
            onCancelPublication={this.handlePublicationCanceled}
            onCancelReduction={this.handleReductionCanceled}
            checkedClientAdmin={isUserClientRoles(this.state.data.secondaryDetail) && this.state.data.secondaryDetail.IsClientAdmin}
            checkedContentPublisher={isUserClientRoles(this.state.data.secondaryDetail) && this.state.data.secondaryDetail.IsContentPublisher}
            checkedAccessAdmin={isUserClientRoles(this.state.data.secondaryDetail) && this.state.data.secondaryDetail.IsAccessAdmin}
            checkedContentUser={isUserClientRoles(this.state.data.secondaryDetail) && this.state.data.secondaryDetail.IsContentUser}
            checkedSuspended={isRootContentItemDetail(this.state.data.secondaryDetail) && this.state.data.secondaryDetail.IsSuspended}
            onPushUserClient={this.pushUserClient}
            onPushSuspend={this.pushSuspendContent}
          />
        </div>
      </>
    );
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
        return {...queryFilter, rootContentItemId: id};
      default:
        return queryFilter;
    }
  }

  private getPrimaryQueryFilter = () => ({});
  private getSecondaryQueryFilter = () => this.assignQueryFilter(
    this.state.primaryPanel.selected.column,
    this.getPrimaryQueryFilter(),
    this.state.primaryPanel.selected.card,
  )
  private getFinalQueryFilter = () => this.assignQueryFilter(
    this.state.secondaryPanel.selected.column,
    this.getSecondaryQueryFilter(),
    this.state.secondaryPanel.selected.card,
  )

  // callbacks for child components
  private handlePrimaryColumnSelected = (column: SystemAdminColumn) => {
    this.setState((prevState) => {
      if (column === prevState.primaryPanel.selected.column) {
        return prevState;
      }
      return {
        data: {
          primaryEntities: null,
          secondaryEntities: null,
          primaryDetail: null,
          secondaryDetail: null,
        },
        primaryPanel: {
          selected: {
            column,
            card: null,
          },
          cards: null,
          filter: {
            text: '',
          },
          createModal: {
            open: false,
          },
        },
        secondaryPanel: {
          selected: {
            column: null,
            card: null,
          },
          cards: null,
          filter: {
            text: '',
          },
          createModal: {
            open: false,
          },
        },
      };
    });
  }

  private handleSecondaryColumnSelected = (column: SystemAdminColumn) => {
    this.setState((prevState) => {
      if (column === prevState.secondaryPanel.selected.column) {
        return prevState;
      }
      return {
        ...prevState,
        data: {
          ...prevState.data,
          secondaryEntities: null,
          secondaryDetail: null,
        },
        secondaryPanel: {
          selected: {
            column,
            card: null,
          },
          cards: null,
          filter: {
            text: '',
          },
          createModal: {
            open: false,
          },
        },
      };
    });
  }

  private handlePrimaryCardSelected = (id: string) => {
    this.setState((prevState) => {
      const defaultColumn = this.getColumns(prevState.primaryPanel.selected.column)[0].id as SystemAdminColumn;
      return {
        data: {
          ...prevState.data,
          secondaryEntities: null,
          primaryDetail: null,
          secondaryDetail: null,
        },
        primaryPanel: {
          ...prevState.primaryPanel,
          selected: {
            ...prevState.primaryPanel.selected,
            card: prevState.primaryPanel.selected.card === id ? null : id,
          },
        },
        secondaryPanel: {
          ...prevState.secondaryPanel,
          selected: {
            column: prevState.secondaryPanel.selected.column || (defaultColumn as SystemAdminColumn),
            card: null,
          },
        },
      };
    });
  }

  private handleSecondaryCardSelected = (id: string) => {
    this.setState((prevState) => ({
      ...prevState,
      data: {
        ...prevState.data,
        secondaryDetail: null,
      },
      secondaryPanel: {
        ...prevState.secondaryPanel,
        selected: {
          ...prevState.secondaryPanel.selected,
          card: prevState.secondaryPanel.selected.card === id ? null : id,
        },
      },
    }));
  }

  private fetchPrimaryEntities = () => {
    getData(`/SystemAdmin/${this.getDataAction(this.state.primaryPanel.selected.column)}`, {})
    .then((response) => {
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          primaryEntities: response,
        },
      }));
    });
  }

  private fetchSecondaryEntities() {
    getData(`/SystemAdmin/${this.getDataAction(this.state.secondaryPanel.selected.column)}`, this.getSecondaryQueryFilter())
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

  private fetchPrimaryDetail = () => {
    if (!this.state.primaryPanel.selected.card) {
      return;
    }

    getData(`/SystemAdmin/${this.getDetailAction(this.state.primaryPanel.selected.column)}`, this.getSecondaryQueryFilter())
    .then((response) => {
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          primaryDetail: response,
        },
      }));
    });
  }

  private fetchSecondaryDetail = () => {
    if (!this.state.secondaryPanel.selected.card) {
      return;
    }
    getData(`/SystemAdmin/${this.getDetailAction(this.state.secondaryPanel.selected.column)}`, this.getFinalQueryFilter())
    .then((response) => {
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          secondaryDetail: response,
        },
      }));
    });
  }

  private handlePublicationCanceled = (event: React.MouseEvent<HTMLAnchorElement>) => {
    event.preventDefault();
    postData(
      '/SystemAdmin/CancelPublication',
      { rootContentItemId: this.getFinalQueryFilter().rootContentItemId },
    ).then(() => {
      alert('Publication canceled.');
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          secondaryDetail: null,
        },
      }));
    });
  }

  private handleReductionCanceled = (event: React.MouseEvent<HTMLAnchorElement>, id: string) => {
    event.preventDefault();
    postData(
      '/SystemAdmin/CancelReduction',
      { selectionGroupId: id },
    ).then(() => {
      alert('Reduction canceled.');
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          secondaryDetail: null,
        },
      }));
    });
  }

  private fetchSystemAdmin = () => {
    getData('/SystemAdmin/SystemRole', Object.assign({}, this.getSecondaryQueryFilter(), { role: RoleEnum.Admin }))
    .then((response: boolean) => {
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          primaryDetail: {
            ...prevState.data.primaryDetail,
            IsSystemAdmin: response,
          },
        },
      }));
    });
  }

  private pushSystemAdmin = () => {
    if (!isUserDetail(this.state.data.primaryDetail)) {
      return;
    }

    postData('/SystemAdmin/SystemRole', Object.assign({}, this.getSecondaryQueryFilter(), { role: RoleEnum.Admin }, {
      value: !this.state.data.primaryDetail.IsSystemAdmin,
    }))
    .then((response: boolean) => {
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          primaryDetail: {
            ...prevState.data.primaryDetail,
            IsSystemAdmin: response,
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
        data: {
          ...prevState.data,
          primaryDetail: {
            ...prevState.data.primaryDetail,
            IsSuspended: response,
          },
        },
      }));
    });
  }

  private pushSuspendUser = () => {
    if (!isUserDetail(this.state.data.primaryDetail)) {
      return;
    }

    postData('/SystemAdmin/UserSuspendedStatus', Object.assign({}, this.getSecondaryQueryFilter(), {
      value: !this.state.data.primaryDetail.IsSuspended,
    }))
    .then((response: boolean) => {
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          primaryDetail: {
            ...prevState.data.primaryDetail,
            IsSuspended: response,
          },
        },
      }));
    });
  }

  private fetchUserClient = (role: RoleEnum) => {
    getData('/SystemAdmin/UserClientRoleAssignment', Object.assign({}, this.getFinalQueryFilter(), { role }))
    .then((response: boolean) => {
      let roleAssignment: Partial<UserClientRoles> = {};
      switch (role) {
        case RoleEnum.Admin:
          roleAssignment = { IsClientAdmin: response };
        case RoleEnum.ContentPublisher:
          roleAssignment = { IsContentPublisher: response };
        case RoleEnum.ContentAccessAdmin:
          roleAssignment = { IsAccessAdmin: response };
        case RoleEnum.ContentUser:
          roleAssignment = { IsContentUser: response };
      }
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          secondaryDetail: {
            ...prevState.data.secondaryDetail,
            ...roleAssignment,
          },
        },
      }));
    });
  }

  private pushUserClient = (_, role: RoleEnum) => {
    if (!isUserClientRoles(this.state.data.secondaryDetail)) {
      return;
    }

    let prevValue = false;
    switch (role) {
      case RoleEnum.Admin:
        prevValue = this.state.data.secondaryDetail.IsClientAdmin;
      case RoleEnum.ContentPublisher:
        prevValue = this.state.data.secondaryDetail.IsContentPublisher;
      case RoleEnum.ContentAccessAdmin:
        prevValue = this.state.data.secondaryDetail.IsAccessAdmin;
      case RoleEnum.ContentUser:
        prevValue = this.state.data.secondaryDetail.IsContentUser;
    }
    postData('/SystemAdmin/UserClientRoleAssignment', Object.assign({}, this.getFinalQueryFilter(), { role }, {
      value: !prevValue,
    }))
    .then((response: boolean) => {
      let roleAssignment: Partial<UserClientRoles> = {};
      switch (role) {
        case RoleEnum.Admin:
          roleAssignment = { IsClientAdmin: response };
        case RoleEnum.ContentPublisher:
          roleAssignment = { IsContentPublisher: response };
        case RoleEnum.ContentAccessAdmin:
          roleAssignment = { IsAccessAdmin: response };
        case RoleEnum.ContentUser:
          roleAssignment = { IsContentUser: response };
      }
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          secondaryDetail: {
            ...prevState.data.secondaryDetail,
            ...roleAssignment,
          },
        },
      }));
    });
  }

  private fetchSuspendContent = () => {
    getData('/SystemAdmin/ContentSuspendedStatus', Object.assign({}, this.getFinalQueryFilter()))
    .then((response: boolean) => {
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          secondaryDetail: {
            ...prevState.data.secondaryDetail,
            IsSuspended: response,
          },
        },
      }));
    });
  }

  private pushSuspendContent = () => {
    if (!isRootContentItemDetail(this.state.data.secondaryDetail)) {
      return;
    }

    postData('/SystemAdmin/ContentSuspendedStatus', Object.assign({}, this.getFinalQueryFilter(), {
      value: !this.state.data.secondaryDetail.IsSuspended,
    }))
    .then((response: boolean) => {
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          secondaryDetail: {
            ...prevState.data.secondaryDetail,
            IsSuspended: response,
          },
        },
      }));
    });
  }

  private handlePrimaryFilterKeyup = (text: string) => {
    this.setState((prevState) => ({
      ...prevState,
      primaryPanel: {
        ...prevState.primaryPanel,
        filter: { text },
      },
    }));
  }

  private handleSecondaryFilterKeyup = (text: string) => {
    this.setState((prevState) => ({
      ...prevState,
      secondaryPanel: {
        ...prevState.secondaryPanel,
        filter: { text },
      },
    }));
  }

  private handlePrimaryModalOpen = () => {
    this.setState((prevState) => ({
      ...prevState,
      primaryPanel: {
        ...prevState.primaryPanel,
        createModal: {
          open: true,
        },
      },
    }));
  }

  private handlePrimaryModalClose = () => {
    this.setState((prevState) => ({
      ...prevState,
      primaryPanel: {
        ...prevState.primaryPanel,
        createModal: {
          open: false,
        },
      },
    }));
  }

  private handleSecondaryModalOpen = () => {
    this.setState((prevState) => ({
      ...prevState,
      secondaryPanel: {
        ...prevState.secondaryPanel,
        createModal: {
          open: true,
        },
      },
    }));
  }

  private handleSecondaryModalClose = () => {
    this.setState((prevState) => ({
      ...prevState,
      secondaryPanel: {
        ...prevState.secondaryPanel,
        createModal: {
          open: false,
        },
      },
    }));
  }
}
