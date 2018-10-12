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
import { ContentPanel, ContentPanelAttributes } from '../shared-components/content-panel';
import { Entity } from '../shared-components/entity';
import { DataSource, RoleEnum, Structure } from '../shared-components/interfaces';
import { NavBar } from '../shared-components/navbar';
import {
  ClientInfo, Detail, EntityInfo, PrimaryDetail, ProfitCenterInfo, RootContentItemInfo,
  SecondaryDetail, UserInfo,
} from './interfaces';
import { PrimaryDetailPanel } from './primary-detail-panel';
import { SecondaryDetailPanel } from './secondary-detail-panel';

interface ToggleInfo {
  checked: boolean;
  disabled: boolean;
}
export interface SystemAdminState {
  data: {
    primaryEntities: EntityInfo[];
    secondaryEntities: EntityInfo[];
  };
  primaryDataSource: string;
  secondaryDataSource: string;
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

export class SystemAdmin extends React.Component<{}, SystemAdminState> {
  private controller: string = 'SystemAdmin';
  private readonly currentView: string = document
    .getElementsByTagName('body')[0].getAttribute('data-nav-location');
  private nullDataSource: DataSource<Entity> = {
    name: null,
    structure: 0,
    parentSources: [],
    displayName: '',
    infoAction: '',
    detailAction: '',
    createAction: null,
    processInfo: () => null,
    assignQueryFilter: () => null,
  };
  private dataSources: Array<DataSource<Entity>> = [
    {
      name: 'user',
      structure: Structure.List,
      parentSources: [
        null,
        {
          name: 'client',
          overrides: {
            createAction: 'AddUserToClient',
          },
        },
        {
          name: 'profitCenter',
          overrides: {
            createAction: 'AddUserToProfitCenter',
            displayName: 'Authorized Users',
          },
        },
      ],
      displayName: 'Users',
      sublistInfo: {
        title: 'Content Items',
        icon: 'reports',
        emptyText: 'This user does not have access to any reports.',
      },
      infoAction: 'Users',
      detailAction: 'UserDetail',
      createAction: 'CreateUser',
      processInfo: (response: UserInfo[]) => response.map((user) => ({
        id: user.Id,
        primaryText: `${user.LastName}, ${user.FirstName}`,
        secondaryText: user.UserName,
        primaryStat: user.ClientCount !== null && {
          name: 'Clients',
          value: user.ClientCount,
          icon: 'client-admin',
        },
        secondaryStat: user.RootContentItemCount !== null && {
          name: 'Reports',
          value: user.RootContentItemCount,
          icon: 'reports',
        },
        sublist: user.RootContentItems && user.RootContentItems.map((item) => ({
          id: item.Id,
          primaryText: item.Name,
        })),
        activated: user.Activated,
        email: user.Email,
        suspended: user.IsSuspended,
        isUserInProfitCenter: user.ProfitCenterId,
      })),
      assignQueryFilter: (userId: string) => ({ userId }),
    },
    {
      name: 'client',
      structure: Structure.Tree,
      parentSources: [
        null,
        'user',
        'profitCenter',
      ],
      displayName: 'Clients',
      infoAction: 'Clients',
      detailAction: 'ClientDetail',
      createAction: null,
      processInfo: (response: BasicTree<ClientInfo>) => {
        interface ClientDepth {
          client: ClientInfo;
          depth: number;
        }
        function traverse(node: BasicNode<ClientInfo>, list: ClientDepth[] = [], depth = 0): ClientDepth[] {
          if (node.Value !== null) {
            const clientDepth = {
              client: node.Value,
              depth,
            };
            list.push(clientDepth);
          }
          if (node.Children.length) {
            node.Children.forEach((child) => list = traverse(child, list, depth + 1));
          }
          return list;
        }
        const clientDepthList = traverse(response.Root);
        return clientDepthList.map((cd) => ({
          id: cd.client.Id,
          primaryText: cd.client.Name,
          secondaryText: cd.client.Code,
          primaryStat: cd.client.UserCount !== null && {
            name: 'Users',
            value: cd.client.UserCount,
            icon: 'user',
          },
          secondaryStat: cd.client.RootContentItemCount !== null && {
            name: 'Reports',
            value: cd.client.RootContentItemCount,
            icon: 'reports',
          },
          indent: cd.depth,
          readOnly: cd.client.ParentOnly,
        }));
      },
      assignQueryFilter: (clientId: string) => ({ clientId }),
    },
    {
      name: 'profitCenter',
      structure: Structure.List,
      parentSources: [
        null,
      ],
      displayName: 'Profit Center',
      infoAction: 'ProfitCenters',
      detailAction: 'ProfitCenterDetail',
      createAction: 'CreateProfitCenter',
      processInfo: (response: ProfitCenterInfo[]) => response.map((profitCenter) => ({
        id: profitCenter.Id,
        primaryText: profitCenter.Name,
        secondaryText: profitCenter.Office,
        primaryStat: {
          name: 'Authorized users',
          value: profitCenter.UserCount,
          icon: 'user',
        },
        secondaryStat: {
          name: 'Clients',
          value: profitCenter.ClientCount,
          icon: 'client-admin',
        },
        isProfitCenter: true,
      })),
      assignQueryFilter: (profitCenterId: string) => ({ profitCenterId }),
    },
    {
      name: 'rootContentItem',
      structure: Structure.List,
      parentSources: [
        {
          name: 'user',
          overrides: {
            displayName: 'Content',
          },
        },
        'client',
      ],
      displayName: 'Content Items',
      sublistInfo: {
        title: 'Members',
        icon: 'user',
        emptyText: 'No users have access to this report.',
      },
      infoAction: 'RootContentItems',
      detailAction: 'RootContentItemDetail',
      createAction: null,
      processInfo: (response: RootContentItemInfo[]) => response.map((item) => ({
        id: item.Id,
        primaryText: item.Name,
        secondaryText: item.ClientName,
        primaryStat: item.UserCount !== null && {
          name: 'Users',
          value: item.UserCount,
          icon: 'user',
        },
        secondaryStat: item.SelectionGroupCount !== null && {
          name: 'Selection Groups',
          value: item.SelectionGroupCount,
          icon: 'group',
        },
        sublist: item.Users && item.Users.map((user) => ({
          id: user.Id,
          primaryText: `${user.FirstName} ${user.LastName}`,
          secondaryText: user.UserName,
        })),
        suspended: item.IsSuspended,
      })),
      assignQueryFilter: (rootContentItemId: string) => ({ rootContentItemId }),
    },
  ];

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

    // pass available data sources as props based on defined data sources
    const primaryDataSources = this.getDataSources(null);
    const primaryDataSource = this.getDataSourceByName(primaryDataSources, this.state.primaryDataSource);
    const secondaryDataSources = this.getDataSources(this.state.primaryDataSource);
    const secondaryDataSource = this.getDataSourceByName(secondaryDataSources, this.state.secondaryDataSource);

    const secondaryQueryFilter = Object.assign(
      {}, primaryDataSource.assignQueryFilter(this.state.primarySelectedCard));
    const finalQueryFilter = Object.assign(
      {},
      primaryDataSource.assignQueryFilter(this.state.primarySelectedCard),
      secondaryDataSource.assignQueryFilter(this.state.secondarySelectedCard),
    );

    const secondaryColumnComponent = this.state.primarySelectedCard
      ? (
        <ContentPanel
          {...this.state.secondaryContentPanel}
          onFilterTextChange={this.handleSecondaryFilterKeyup}
          onModalOpen={this.handleSecondaryModalOpen}
          onModalClose={this.handleSecondaryModalClose}
          controller={this.controller}
          dataSources={secondaryDataSources}
          setSelectedDataSource={this.setSecondaryDataSource}
          selectedDataSource={secondaryDataSource}
          setSelectedCard={this.setSecondarySelectedCard}
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
          controller={this.controller}
          dataSources={primaryDataSources}
          setSelectedDataSource={this.setPrimaryDataSource}
          selectedDataSource={primaryDataSource}
          setSelectedCard={this.setPrimarySelectedCard}
          selectedCard={this.state.primarySelectedCard}
          queryFilter={{}}
          entities={this.state.data.primaryEntities}
        />
        {secondaryColumnComponent}
        <div
          className="admin-panel-container flex-item-12-12 flex-item-for-tablet-up-4-12 flex-item-for-desktop-up-6-12"
          style={{overflowY: 'auto'}}
        >
          <PrimaryDetailPanel
            controller={this.controller}
            selectedDataSource={primaryDataSource}
            selectedCard={this.state.primarySelectedCard}
            queryFilter={secondaryQueryFilter}
            detail={this.state.primaryDetail}
            onPushSystemAdmin={this.pushSystemAdmin}
            checkedSystemAdmin={this.state.toggles.systemAdmin.checked}
            onPushSuspend={this.pushSuspendUser}
            checkedSuspended={this.state.toggles.userSuspend.checked}
          />
          <SecondaryDetailPanel
            controller={this.controller}
            primarySelectedDataSource={primaryDataSource}
            secondarySelectedDataSource={secondaryDataSource}
            selectedCard={this.state.secondarySelectedCard}
            queryFilter={finalQueryFilter}
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

  private setPrimarySelectedCard(cardId: string) {
    this.setState((prevState) => {
      const defaultSecondaryDataSource = this.getDataSources(prevState.primaryDataSource)[0].name;
      return {
        data: {
          ...prevState.data,
          secondaryEntities: null,
        },
        primarySelectedCard: prevState.primarySelectedCard === cardId
          ? null
          : cardId,
        secondaryDataSource: prevState.secondaryDataSource || defaultSecondaryDataSource,
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
  private getDataSources(parentName: string): Array<DataSource<Entity>> {
    return this.dataSources
      // strip non-matching parent sources
      .map((dataSource) => {
        const filteredDataSource = {...dataSource};
        filteredDataSource.parentSources = filteredDataSource.parentSources.filter((parentSource) =>
          parentSource === null || typeof parentSource === 'string'
            ? parentSource === parentName
            : parentSource.name === parentName);
        return filteredDataSource;
      })
      // filter out data sources without the parent source
      .filter((dataSource) => dataSource.parentSources.length)
      // apply overrides if present
      .map((dataSource) => {
        const parentSource = dataSource.parentSources[0];
        if (parentSource !== null && typeof parentSource !== 'string') {
          Object.assign(dataSource, parentSource.overrides);
        }
        return dataSource;
      });
  }

  private getDataSourceByName(dataSources: Array<DataSource<Entity>>, name: string): DataSource<Entity> {
    return dataSources.filter((dataSource) => dataSource.name === name)[0]
      || this.nullDataSource;
  }

  // more callbacks
  private fetchPrimaryEntities() {
    const dataSource = this.getDataSourceByName(this.getDataSources(null), this.state.primaryDataSource);
    getData(`/SystemAdmin/${dataSource.infoAction}`, {})
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
    const dataSource = this.getDataSourceByName(
      this.getDataSources(this.state.primaryDataSource),
      this.state.secondaryDataSource);
    const queryFilter = this.getDataSourceByName(this.getDataSources(null), this.state.primaryDataSource)
      .assignQueryFilter(this.state.primarySelectedCard);

    getData(`/SystemAdmin/${dataSource.infoAction}`, queryFilter)
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

    const dataSource = this.getDataSourceByName(this.getDataSources(null), this.state.primaryDataSource);
    const queryFilter = dataSource.assignQueryFilter(this.state.primarySelectedCard);
    getData(`/SystemAdmin/${dataSource.detailAction}`, queryFilter)
    .then((response) => this.setState({
      primaryDetail: response,
    }));
  }

  private fetchSecondaryDetail() {
    if (!this.state.secondarySelectedCard) {
      return;
    }

    const primaryDataSource = this.getDataSourceByName(this.getDataSources(null), this.state.primaryDataSource);
    const dataSource = this.getDataSourceByName(
      this.getDataSources(this.state.primaryDataSource),
      this.state.secondaryDataSource);
    const queryFilter = {
      ...primaryDataSource.assignQueryFilter(this.state.primarySelectedCard),
      ...dataSource.assignQueryFilter(this.state.secondarySelectedCard),
    };
    getData(`/SystemAdmin/${dataSource.detailAction}`, queryFilter)
    .then((response) => this.setState({
      secondaryDetail: response,
    }));
  }

  private cancelPublicationRequest(event: React.MouseEvent<HTMLAnchorElement>) {
    event.preventDefault();
    const primaryDataSource = this.getDataSourceByName(this.getDataSources(null), this.state.primaryDataSource);
    const dataSource = this.getDataSourceByName(
      this.getDataSources(this.state.primaryDataSource),
      this.state.secondaryDataSource);
    const queryFilter = {
      ...primaryDataSource.assignQueryFilter(this.state.primarySelectedCard),
      ...dataSource.assignQueryFilter(this.state.secondarySelectedCard),
    };
    postData(
      '/SystemAdmin/CancelPublication',
      { rootContentItemId: queryFilter.rootContentItemId },
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
    const dataSource = this.getDataSourceByName(this.getDataSources(null), this.state.primaryDataSource);
    const queryFilter = dataSource.assignQueryFilter(this.state.primarySelectedCard);
    getData('/SystemAdmin/SystemRole', Object.assign({}, queryFilter, { role: RoleEnum.Admin }))
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
    const dataSource = this.getDataSourceByName(this.getDataSources(null), this.state.primaryDataSource);
    const queryFilter = dataSource.assignQueryFilter(this.state.primarySelectedCard);
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

    postData('/SystemAdmin/SystemRole', Object.assign({}, queryFilter, { role: RoleEnum.Admin }, {
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
    const dataSource = this.getDataSourceByName(this.getDataSources(null), this.state.primaryDataSource);
    const queryFilter = dataSource.assignQueryFilter(this.state.primarySelectedCard);
    getData('/SystemAdmin/UserSuspendedStatus', Object.assign({}, queryFilter))
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
    const dataSource = this.getDataSourceByName(this.getDataSources(null), this.state.primaryDataSource);
    const queryFilter = dataSource.assignQueryFilter(this.state.primarySelectedCard);
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

    postData('/SystemAdmin/UserSuspendedStatus', Object.assign({}, queryFilter, {
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
    const primaryDataSource = this.getDataSourceByName(this.getDataSources(null), this.state.primaryDataSource);
    const dataSource = this.getDataSourceByName(
      this.getDataSources(this.state.primaryDataSource),
      this.state.secondaryDataSource);
    const queryFilter = {
      ...primaryDataSource.assignQueryFilter(this.state.primarySelectedCard),
      ...dataSource.assignQueryFilter(this.state.secondarySelectedCard),
    };
    getData('/SystemAdmin/UserClientRoleAssignment', Object.assign({}, queryFilter, { role }))
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
    const primaryDataSource = this.getDataSourceByName(this.getDataSources(null), this.state.primaryDataSource);
    const dataSource = this.getDataSourceByName(
      this.getDataSources(this.state.primaryDataSource),
      this.state.secondaryDataSource);
    const queryFilter = {
      ...primaryDataSource.assignQueryFilter(this.state.primarySelectedCard),
      ...dataSource.assignQueryFilter(this.state.secondarySelectedCard),
    };
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

    postData('/SystemAdmin/UserClientRoleAssignment', Object.assign({}, queryFilter, { role }, {
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
    const primaryDataSource = this.getDataSourceByName(this.getDataSources(null), this.state.primaryDataSource);
    const dataSource = this.getDataSourceByName(
      this.getDataSources(this.state.primaryDataSource),
      this.state.secondaryDataSource);
    const queryFilter = {
      ...primaryDataSource.assignQueryFilter(this.state.primarySelectedCard),
      ...dataSource.assignQueryFilter(this.state.secondarySelectedCard),
    };
    getData('/SystemAdmin/ContentSuspendedStatus', Object.assign({}, queryFilter))
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
    const primaryDataSource = this.getDataSourceByName(this.getDataSources(null), this.state.primaryDataSource);
    const dataSource = this.getDataSourceByName(
      this.getDataSources(this.state.primaryDataSource),
      this.state.secondaryDataSource);
    const queryFilter = {
      ...primaryDataSource.assignQueryFilter(this.state.primarySelectedCard),
      ...dataSource.assignQueryFilter(this.state.secondarySelectedCard),
    };
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

    postData('/SystemAdmin/ContentSuspendedStatus', Object.assign({}, queryFilter, {
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
