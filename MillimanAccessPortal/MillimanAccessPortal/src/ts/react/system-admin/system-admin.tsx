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
import { BasicNode } from '../../view-models/content-publishing';
import { CardAttributes } from '../shared-components/card';
import { ColumnIndicator } from '../shared-components/column-selector';
import { ContentPanel } from '../shared-components/content-panel';
import { Guid, QueryFilter, RoleEnum } from '../shared-components/interfaces';
import { NavBar } from '../shared-components/navbar';
import {
  ClientInfo, ClientInfoWithDepth, EntityInfo, EntityInfoCollection, isClientInfoTree,
  isRootContentItemDetail, isUserClientRoles, isUserDetail, PrimaryDetail, PrimaryDetailData,
  SecondaryDetail, SecondaryDetailData, UserClientRoles,
} from './interfaces';
import { PrimaryDetailPanel } from './primary-detail-panel';
import { SecondaryDetailPanel } from './secondary-detail-panel';

export interface ContentPanelAttributes {
  selected: {
    column: SystemAdminColumn;
    card: string;
  };
  cards: {
    [id: string]: CardAttributes;
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
    primaryDetail: PrimaryDetailData;
    secondaryDetail: SecondaryDetailData;
  };
  primaryPanel: ContentPanelAttributes;
  secondaryPanel: ContentPanelAttributes;
}

export enum SystemAdminColumn {
  USER = 'user',
  CLIENT = 'client',
  PROFIT_CENTER = 'profitCenter',
  ROOT_CONTENT_ITEM = 'rootContentItem',
}

export class SystemAdmin extends React.Component<{}, SystemAdminState> {
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
    const { primaryEntities, secondaryEntities, primaryDetail, secondaryDetail } = this.state.data;
    const { card: primaryCard } = this.state.primaryPanel.selected;
    const { column: secondaryColumn, card: secondaryCard } = this.state.secondaryPanel.selected;
    if (primaryEntities === null) {
      this.fetchPrimaryEntities();
    }
    if (primaryCard) {
      if (primaryDetail === null) {
        this.fetchPrimaryDetail();
      } else {
        if (isUserDetail(primaryDetail) && primaryDetail.isSuspended === null) {
          this.fetchSystemAdmin();
          this.fetchSuspendUser();
        }
      }
    }
    if (secondaryEntities === null && secondaryColumn) {
      this.fetchSecondaryEntities();
    }
    if (secondaryCard) {
      if (secondaryDetail === null) {
        this.fetchSecondaryDetail();
      } else {
        if (isUserClientRoles(secondaryDetail) && secondaryDetail.isClientAdmin === null) {
          this.fetchUserClient(RoleEnum.Admin);
          this.fetchUserClient(RoleEnum.ContentPublisher);
          this.fetchUserClient(RoleEnum.ContentAccessAdmin);
          this.fetchUserClient(RoleEnum.ContentUser);
        }
        if (isRootContentItemDetail(secondaryDetail) && secondaryDetail.isSuspended === null) {
          this.fetchSuspendContent();
        }
      }
    }
  }

  public render() {
    const { primaryEntities, secondaryEntities, primaryDetail, secondaryDetail } = this.state.data;
    const { column: primaryColumn, card: primaryCard } = this.state.primaryPanel.selected;
    const { column: secondaryColumn, card: secondaryCard } = this.state.secondaryPanel.selected;

    const secondaryQueryFilter = this.getSecondaryQueryFilter();
    const secondaryColumnComponent = secondaryColumn
      ? (
        <ContentPanel
          panelHeader={{
            columns: this.getColumns(primaryColumn),
            onColumnSelect: this.handleSecondaryColumnSelected,
            selectedColumn: this.getColumns(primaryColumn).filter((c) => c.id === secondaryColumn)[0],
          }}
          filterText={this.state.secondaryPanel.filter.text}
          modalOpen={this.state.secondaryPanel.createModal.open}
          onFilterTextChange={this.handleSecondaryFilterKeyup}
          onModalOpen={this.handleSecondaryModalOpen}
          onModalClose={this.handleSecondaryModalClose}
          createAction={this.getCreateAction(secondaryColumn, primaryColumn)}
          onExpandedToggled={this.handleSecondaryExpandedToggled}
          cards={this.state.secondaryPanel.cards}
          onCardSelect={this.handleSecondaryCardSelected}
          selectedCard={secondaryCard}
          queryFilter={secondaryQueryFilter}
          entities={secondaryEntities}
          onProfitCenterModalOpen={this.handleProfitCenterModalOpen}
          onProfitCenterModalClose={this.handleProfitCenterModalClose}
          onSendReset={this.handleSendReset}
          onProfitCenterDelete={this.handleProfitCenterDelete}
          onProfitCenterUserRemove={this.handleProfitCenterUserRemove}
          onClientUserRemove={this.handleClientUserRemove}
        />
      )
      : null;
    return (
      <>
        <NavBar
          currentView={this.currentView}
        />
        <ContentPanel
          panelHeader={{
            columns: this.getColumns(),
            onColumnSelect: this.handlePrimaryColumnSelected,
            selectedColumn: this.getColumns().filter((c) => c.id === primaryColumn)[0],
          }}
          filterText={this.state.primaryPanel.filter.text}
          modalOpen={this.state.primaryPanel.createModal.open}
          onFilterTextChange={this.handlePrimaryFilterKeyup}
          onModalOpen={this.handlePrimaryModalOpen}
          onModalClose={this.handlePrimaryModalClose}
          createAction={this.getCreateAction(primaryColumn)}
          onExpandedToggled={this.handlePrimaryExpandedToggled}
          cards={this.state.primaryPanel.cards}
          onCardSelect={this.handlePrimaryCardSelected}
          selectedCard={primaryCard}
          queryFilter={this.getPrimaryQueryFilter()}
          entities={primaryEntities}
          onProfitCenterModalOpen={this.handleProfitCenterModalOpen}
          onProfitCenterModalClose={this.handleProfitCenterModalClose}
          onSendReset={this.handleSendReset}
          onProfitCenterDelete={this.handleProfitCenterDelete}
          onProfitCenterUserRemove={this.handleProfitCenterUserRemove}
          onClientUserRemove={this.handleClientUserRemove}
        />
        {secondaryColumnComponent}
        <div
          className="admin-panel-container flex-item-12-12 flex-item-for-tablet-up-4-12 flex-item-for-desktop-up-6-12"
          style={{overflowY: 'auto'}}
        >
          <PrimaryDetailPanel
            selectedCard={primaryCard}
            selectedColumn={primaryColumn}
            queryFilter={secondaryQueryFilter}
            detail={primaryDetail}
            onPushSystemAdmin={this.pushSystemAdmin}
            checkedSystemAdmin={isUserDetail(primaryDetail) && primaryDetail.isSystemAdmin}
            onPushSuspend={this.pushSuspendUser}
            checkedSuspended={isUserDetail(primaryDetail) && primaryDetail.isSuspended}
          />
          <SecondaryDetailPanel
            selectedCard={secondaryCard}
            primarySelectedColumn={primaryColumn}
            secondarySelectedColumn={secondaryColumn}
            queryFilter={this.getFinalQueryFilter()}
            detail={secondaryDetail}
            onCancelPublication={this.handlePublicationCanceled}
            onCancelReduction={this.handleReductionCanceled}
            checkedClientAdmin={isUserClientRoles(secondaryDetail) && secondaryDetail.isClientAdmin}
            checkedContentPublisher={isUserClientRoles(secondaryDetail) && secondaryDetail.isContentPublisher}
            checkedAccessAdmin={isUserClientRoles(secondaryDetail) && secondaryDetail.isAccessAdmin}
            checkedContentUser={isUserClientRoles(secondaryDetail) && secondaryDetail.isContentUser}
            checkedSuspended={isRootContentItemDetail(secondaryDetail) && secondaryDetail.isSuspended}
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

  private assignQueryFilter(column: SystemAdminColumn, queryFilter: QueryFilter, id: Guid): QueryFilter {
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

  private handlePrimaryCardSelected = (id: Guid) => {
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

  private handleSecondaryCardSelected = (id: Guid) => {
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
    const { column } = this.state.primaryPanel.selected;
    getData(`/SystemAdmin/${this.getDataAction(column)}`, {})
    .then((response: EntityInfoCollection) => {
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          primaryEntities: response,
        },
        primaryPanel: {
          ...prevState.primaryPanel,
          cards: this.initializeCardAttributes(response),
        },
      }));
    });
  }

  private fetchSecondaryEntities() {
    const { column } = this.state.secondaryPanel.selected;
    getData(`/SystemAdmin/${this.getDataAction(column)}`, this.getSecondaryQueryFilter())
    .then((response) => {
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          secondaryEntities: response,
        },
        secondaryPanel: {
          ...prevState.secondaryPanel,
          cards: this.initializeCardAttributes(response),
        },
      }));
    });
  }

  private fetchPrimaryDetail = () => {
    const { column, card } = this.state.primaryPanel.selected;
    if (!card) {
      return;
    }

    getData(`/SystemAdmin/${this.getDetailAction(column)}`, this.getSecondaryQueryFilter())
    .then((response: PrimaryDetail) => {
      let responseWithDefaults: PrimaryDetailData;
      if (isUserDetail(response)) {
        responseWithDefaults = {
          ...response,
          isSystemAdmin: null,
          isSuspended: null,
        };
      } else {
        responseWithDefaults = response;
      }
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          primaryDetail: responseWithDefaults,
        },
      }));
    });
  }

  private fetchSecondaryDetail = () => {
    const { column, card } = this.state.secondaryPanel.selected;
    if (!card) {
      return;
    }
    getData(`/SystemAdmin/${this.getDetailAction(column)}`, this.getFinalQueryFilter())
    .then((response: SecondaryDetail) => {
      let responseWithDefaults: SecondaryDetailData;
      if (isUserClientRoles(response)) {
        responseWithDefaults = {
          ...response,
          isClientAdmin: null,
          isContentPublisher: null,
          isAccessAdmin: null,
          isContentUser: null,
        };
      } else if (isRootContentItemDetail(response)) {
        responseWithDefaults = {
          ...response,
          isSuspended: null,
        };
      } else {
        responseWithDefaults = response;
      }
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          secondaryDetail: responseWithDefaults,
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

  private handleReductionCanceled = (event: React.MouseEvent<HTMLAnchorElement>, id: Guid) => {
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
    getData('/SystemAdmin/SystemRole', {
      ...this.getSecondaryQueryFilter(),
      role: RoleEnum.Admin,
    }).then((response: boolean) => {
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          primaryDetail: {
            ...prevState.data.primaryDetail,
            isSystemAdmin: response,
          },
        },
      }));
    });
  }

  private pushSystemAdmin = () => {
    const { primaryDetail } = this.state.data;
    if (!isUserDetail(primaryDetail)) {
      return;
    }

    postData('/SystemAdmin/SystemRole', {
      ...this.getSecondaryQueryFilter(),
      role: RoleEnum.Admin,
      value: !primaryDetail.isSystemAdmin,
    }).then((response: boolean) => {
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          primaryDetail: {
            ...prevState.data.primaryDetail,
            isSystemAdmin: response,
          },
        },
      }));
    });
  }

  private fetchSuspendUser = () => {
    getData('/SystemAdmin/UserSuspendedStatus', {
      ...this.getSecondaryQueryFilter(),
    }).then((response: boolean) => {
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          primaryDetail: {
            ...prevState.data.primaryDetail,
            isSuspended: response,
          },
        },
      }));
    });
  }

  private pushSuspendUser = () => {
    const { primaryDetail } = this.state.data;
    if (!isUserDetail(primaryDetail)) {
      return;
    }

    postData('/SystemAdmin/UserSuspendedStatus', {
      ...this.getSecondaryQueryFilter(),
      value: !primaryDetail.isSuspended,
    }).then((response: boolean) => {
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          primaryDetail: {
            ...prevState.data.primaryDetail,
            isSuspended: response,
          },
        },
      }));
    });
  }

  private fetchUserClient = (role: RoleEnum) => {
    getData('/SystemAdmin/UserClientRoleAssignment', {
      ...this.getFinalQueryFilter(),
      role,
    }).then((response: boolean) => {
      let roleAssignment: Partial<UserClientRoles> = {};
      if (role === RoleEnum.Admin) {
        roleAssignment = { isClientAdmin: response };
      } else if (role === RoleEnum.ContentPublisher) {
        roleAssignment = { isContentPublisher: response };
      } else if (role === RoleEnum.ContentAccessAdmin) {
        roleAssignment = { isAccessAdmin: response };
      } else if (role === RoleEnum.ContentUser) {
        roleAssignment = { isContentUser: response };
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
    const { secondaryDetail } = this.state.data;
    if (!isUserClientRoles(secondaryDetail)) {
      return;
    }

    let prevValue = false;
    if (role === RoleEnum.Admin) {
      prevValue = secondaryDetail.isClientAdmin;
    } else if (role === RoleEnum.ContentPublisher) {
      prevValue = secondaryDetail.isContentPublisher;
    } else if (role === RoleEnum.ContentAccessAdmin) {
      prevValue = secondaryDetail.isAccessAdmin;
    } else if (role === RoleEnum.ContentUser) {
      prevValue = secondaryDetail.isContentUser;
    }
    postData('/SystemAdmin/UserClientRoleAssignment', {
      ...this.getFinalQueryFilter(),
      role,
      value: !prevValue,
    }).then((response: boolean) => {
      let roleAssignment: Partial<UserClientRoles> = {};
      if (role === RoleEnum.Admin) {
        roleAssignment = { isClientAdmin: response };
      } else if (role === RoleEnum.ContentPublisher) {
        roleAssignment = { isContentPublisher: response };
      } else if (role === RoleEnum.ContentAccessAdmin) {
        roleAssignment = { isAccessAdmin: response };
      } else if (role === RoleEnum.ContentUser) {
        roleAssignment = { isContentUser: response };
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
    getData('/SystemAdmin/ContentSuspendedStatus', {
      ...this.getFinalQueryFilter(),
    }).then((response: boolean) => {
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          secondaryDetail: {
            ...prevState.data.secondaryDetail,
            isSuspended: response,
          },
        },
      }));
    });
  }

  private pushSuspendContent = () => {
    const { secondaryDetail } = this.state.data;
    if (!isRootContentItemDetail(secondaryDetail)) {
      return;
    }

    postData('/SystemAdmin/ContentSuspendedStatus', {
      ...this.getFinalQueryFilter(),
      value: !secondaryDetail.isSuspended,
    }).then((response: boolean) => {
      this.setState((prevState) => ({
        ...prevState,
        data: {
          ...prevState.data,
          secondaryDetail: {
            ...prevState.data.secondaryDetail,
            isSuspended: response,
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

  private handlePrimaryExpandedToggled = (id: Guid) => {
    this.setState((prevState) => ({
      ...prevState,
      primaryPanel: {
        ...prevState.primaryPanel,
        cards: {
          ...prevState.primaryPanel.cards,
          [id]: {
            ...prevState.secondaryPanel.cards[id],
            expanded: !prevState.primaryPanel.cards[id].expanded,
          },
        },
      },
    }));
  }

  private handleSecondaryExpandedToggled = (id: Guid) => {
    this.setState((prevState) => ({
      ...prevState,
      secondaryPanel: {
        ...prevState.secondaryPanel,
        cards: {
          ...prevState.secondaryPanel.cards,
          [id]: {
            ...prevState.secondaryPanel.cards[id],
            expanded: !prevState.secondaryPanel.cards[id].expanded,
          },
        },
      },
    }));
  }

  private handleProfitCenterModalOpen = (id: Guid) => {
    this.setState((prevState) => ({
      ...prevState,
      primaryPanel: {
        ...prevState.primaryPanel,
        cards: {
          ...prevState.primaryPanel.cards,
          [id]: {
            ...prevState.primaryPanel.cards[id],
            profitCenterModalOpen: true,
          },
        },
      },
    }));
  }

  private handleProfitCenterModalClose = (id: Guid) => {
    this.setState((prevState) => ({
      ...prevState,
      primaryPanel: {
        ...prevState.primaryPanel,
        cards: {
          ...prevState.primaryPanel.cards,
          [id]: {
            ...prevState.primaryPanel.cards[id],
            profitCenterModalOpen: false,
          },
        },
      },
    }));
  }

  private handleSendReset = (email: string) => {
    postData('Account/ForgotPassword', { Email: email }, true)
    .then(() => {
      alert('Password reset email sent.');
    });
  }

  private handleProfitCenterDelete = (id: Guid) => {
    postData('SystemAdmin/DeleteProfitCenter', { profitCenterId: id }, true)
    .then(() => {
      alert('Profit center deleted.');
    });
  }

  private handleProfitCenterUserRemove = (userId: Guid, profitCenterId: Guid) => {
    postData('SystemAdmin/RemoveUserFromProfitCenter', { userId, profitCenterId }, true)
    .then(() => {
      alert('User removed from profit center.');
    });
  }

  private handleClientUserRemove = (userId: Guid, clientId: Guid) => {
    postData('SystemAdmin/RemoveUserFromClient', { userId, clientId }, true)
    .then(() => {
      alert('User removed from client.');
    });
  }

  private initializeCardAttributes(entities: EntityInfoCollection): { [id: string]: CardAttributes } {
    let entityInfo: EntityInfo[];
    if (isClientInfoTree(entities)) {
      // flatten basic tree into an array
      const traverse = (node: BasicNode<ClientInfo>, list: ClientInfoWithDepth[] = [], depth = 0) => {
        if (node.value !== null) {
          const clientDepth = {
            ...node.value,
            depth,
          };
          list.push(clientDepth);
        }
        if (node.children.length) {
          node.children.forEach((child) => list = traverse(child, list, depth + 1));
        }
        return list;
      };
      entityInfo = traverse(entities.root);
    } else {
      entityInfo = entities;
    }
    const cards: {
      [id: string]: CardAttributes;
    } = {};
    entityInfo.forEach((entity) => {
        cards[entity.id] = {
          expanded: false,
          profitCenterModalOpen: false,
        };
    });
    return cards;
  }
}
