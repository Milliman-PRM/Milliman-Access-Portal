import { isEqual } from 'lodash';
import * as React from 'react';

import { getData } from '../../shared';
import { Entity } from '../shared-components/entity';
import { ImmediateToggle } from '../shared-components/immediate-toggle';
import { DataSource, QueryFilter, RoleEnum } from '../shared-components/interfaces';
import {
  ClientDetailForProfitCenter, ClientDetailForUser, NestedList, RootContentItemDetailForClient,
  RootContentItemDetailForUser, SecondaryDetail, UserDetailForClient, UserDetailForProfitCenter,
} from './interfaces';

interface SecondaryDetailPanelProps {
  controller: string;
  primarySelectedDataSource: DataSource<Entity>;
  secondarySelectedDataSource: DataSource<Entity>;
  selectedCard: number;
  queryFilter: QueryFilter;
}

interface SecondaryDetailPanelState {
  detail: SecondaryDetail;
  prevQuery: {
    dataSource: string;
    entityId: number;
  };
}

export class SecondaryDetailPanel extends React.Component<SecondaryDetailPanelProps, SecondaryDetailPanelState> {

  // see https://github.com/reactjs/rfcs/issues/26#issuecomment-365744134
  public static getDerivedStateFromProps(
    nextProps: SecondaryDetailPanelProps, prevState: SecondaryDetailPanelState,
  ): Partial<SecondaryDetailPanelState> {
    const nextQuery = {
      dataSource: nextProps.secondarySelectedDataSource.name,
      entityId: nextProps.selectedCard,
    };

    if (!isEqual(nextQuery, prevState.prevQuery)) {
      return {
        prevQuery: nextQuery,
        detail: null,
      };
    }

    return null;
  }

  private get url() {
    return this.props.selectedCard
      && `${this.props.controller}/${this.props.secondarySelectedDataSource.detailAction}/`;
  }

  public constructor(props) {
    super(props);

    this.state = {
      detail: null,
      prevQuery: null,
    };
  }

  public componentDidMount() {
    this.fetch();
  }

  public componentDidUpdate() {
    if (this.state.detail === null) {
      this.fetch();
    }
  }

  public render() {
    // populate detail panel
    const secondaryDetail = (() => {
      if (!this.state.detail) {
        return null;
      }
      switch (this.props.primarySelectedDataSource.name) {
        case 'user':
          switch (this.props.secondarySelectedDataSource.name) {
            case 'client':
              const clientDetailForUser = this.state.detail as ClientDetailForUser;
              return (
                <div>
                  <div className="detail-column-container">
                    <div className="detail-column flex-item-for-desktop-up-6-12">
                      <div className="detail-section">
                        <h3 className="detail-section-title">Client Details</h3>
                        <div className="detail-container">
                          <span className="detail-label">Client Name</span>
                          <span className="detail-value">{clientDetailForUser.ClientName}</span>
                        </div>
                        <div className="detail-container">
                          <span className="detail-label">Client Code</span>
                          <span className="detail-value">{clientDetailForUser.ClientCode}</span>
                        </div>
                      </div>
                    </div>
                    <div className="detail-column flex-item-for-desktop-up-6-12">
                      <div className="detail-section">
                        <h3 className="detail-section-title">Client/User Roles</h3>
                        <div className="detail-container">
                          <ImmediateToggle
                            controller={this.props.controller}
                            action={'UserClientRoles'}
                            queryFilter={this.props.queryFilter}
                            label={'Client Admin'}
                            data={{ role: RoleEnum.Admin }}
                          />
                        </div>
                        <div className="detail-container">
                          <ImmediateToggle
                            controller={this.props.controller}
                            action={'UserClientRoles'}
                            queryFilter={this.props.queryFilter}
                            label={'Content Access Admin'}
                            data={{ role: RoleEnum.ContentAccessAdmin }}
                          />
                        </div>
                        <div className="detail-container">
                          <ImmediateToggle
                            controller={this.props.controller}
                            action={'UserClientRoles'}
                            queryFilter={this.props.queryFilter}
                            label={'Content Publisher'}
                            data={{ role: RoleEnum.ContentPublisher }}
                          />
                        </div>
                        <div className="detail-container">
                          <ImmediateToggle
                            controller={this.props.controller}
                            action={'UserClientRoles'}
                            queryFilter={this.props.queryFilter}
                            label={'Content Eligible'}
                            data={{ role: RoleEnum.ContentUser }}
                          />
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              );
            case 'rootContentItem':
              const rootContentItemDetailForUser = this.state.detail as RootContentItemDetailForUser;
              return (
                <div>
                  <div className="detail-column-container">
                    <div className="detail-column flex-item-for-desktop-up-6-12">
                      <div className="detail-section">
                        <h3 className="detail-section-title">Authorized Content Details</h3>
                        <div className="detail-container">
                          <span className="detail-label">Content Name</span>
                          <span className="detail-value">{rootContentItemDetailForUser.ContentName}</span>
                        </div>
                        <div className="detail-container">
                          <span className="detail-label">Content Type</span>
                          <span className="detail-value">{rootContentItemDetailForUser.ContentType}</span>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              );
            default:
              return null;
          }
        case 'client':
          switch (this.props.secondarySelectedDataSource.name) {
            case 'user':
              const userDetailForClient = this.state.detail as UserDetailForClient;
              return (
                <div>
                  <div className="detail-column-container">
                    <div className="detail-column flex-item-for-desktop-up-6-12">
                      <div className="detail-section">
                        <h3 className="detail-section-title">User Details</h3>
                        <div className="detail-container">
                          <span className="detail-label">First Name</span>
                          <span className="detail-value">{userDetailForClient.FirstName}</span>
                        </div>
                        <div className="detail-container">
                          <span className="detail-label">Last Name</span>
                          <span className="detail-value">{userDetailForClient.LastName}</span>
                        </div>
                        <div className="detail-container">
                          <span className="detail-label">Company</span>
                          <span className="detail-value">{userDetailForClient.Employer}</span>
                        </div>
                        <div className="detail-container">
                          <span className="detail-label">Username</span>
                          <span className="detail-value">{userDetailForClient.UserName}</span>
                        </div>
                        <div className="detail-container">
                          <span className="detail-label">Email</span>
                          <span className="detail-value">{userDetailForClient.Email}</span>
                        </div>
                        <div className="detail-container">
                          <span className="detail-label">Phone</span>
                          <span className="detail-value">{userDetailForClient.Phone}</span>
                        </div>
                      </div>
                    </div>
                    <div className="detail-column flex-item-for-desktop-up-6-12">
                      <div className="detail-section">
                        <h3 className="detail-section-title">Client/User Roles</h3>
                        <div className="detail-container">
                          <ImmediateToggle
                            controller={this.props.controller}
                            action={'UserClientRoles'}
                            queryFilter={this.props.queryFilter}
                            label={'Client Admin'}
                            data={{ role: RoleEnum.Admin }}
                          />
                        </div>
                        <div className="detail-container">
                          <ImmediateToggle
                            controller={this.props.controller}
                            action={'UserClientRoles'}
                            queryFilter={this.props.queryFilter}
                            label={'Content Access Admin'}
                            data={{ role: RoleEnum.ContentAccessAdmin }}
                          />
                        </div>
                        <div className="detail-container">
                          <ImmediateToggle
                            controller={this.props.controller}
                            action={'UserClientRoles'}
                            queryFilter={this.props.queryFilter}
                            label={'Content Publisher'}
                            data={{ role: RoleEnum.ContentPublisher }}
                          />
                        </div>
                        <div className="detail-container">
                          <ImmediateToggle
                            controller={this.props.controller}
                            action={'UserClientRoles'}
                            queryFilter={this.props.queryFilter}
                            label={'Content Eligible'}
                            data={{ role: RoleEnum.ContentUser }}
                          />
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              );
            case 'rootContentItem':
              const rootContentItemDetailForClient = this.state.detail as RootContentItemDetailForClient;
              return (
                <div>
                  <div className="detail-column-container">
                    <div className="detail-column flex-item-for-desktop-up-6-12">
                      <div className="detail-section">
                        <h3 className="detail-section-title">Content Item Details</h3>
                        <div className="detail-container">
                          <span className="detail-label">Name</span>
                          <span className="detail-value">{rootContentItemDetailForClient.ContentName}</span>
                        </div>
                        <div className="detail-container">
                          <span className="detail-label">Content Type</span>
                          <span className="detail-value">{rootContentItemDetailForClient.ContentType}</span>
                        </div>
                        <div className="detail-container">
                          <span className="detail-label">Description</span>
                          <span className="detail-value">{rootContentItemDetailForClient.Description}</span>
                        </div>
                        <div className="detail-container">
                          <span className="detail-label">Last Updated</span>
                          <span className="detail-value">{rootContentItemDetailForClient.LastUpdated}</span>
                        </div>
                        <div className="detail-container">
                          <span className="detail-label">Last Accessed</span>
                          <span className="detail-value">{rootContentItemDetailForClient.LastAccessed}</span>
                        </div>
                        <div className="detail-container">
                          <ImmediateToggle
                            controller={this.props.controller}
                            action={'ContentSuspension'}
                            queryFilter={this.props.queryFilter}
                            label={'Suspended'}
                            data={{ }}
                          />
                        </div>
                      </div>
                    </div>
                    <div className="detail-column flex-item-for-desktop-up-6-12">
                      <div className="detail-section">
                        <h3 className="detail-section-title">Selection Groups</h3>
                        <div className="nested-list-container">
                          {this.renderNestedList(rootContentItemDetailForClient.SelectionGroups)}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              );
            default:
              return null;
          }
        case 'profitCenter':
          switch (this.props.secondarySelectedDataSource.name) {
            case 'user':
              const userDetailForProfitCenter = this.state.detail as UserDetailForProfitCenter;
              return (
                <div>
                  <div className="detail-column-container">
                    <div className="detail-column flex-item-for-desktop-up-6-12">
                      <div className="detail-section">
                        <h3 className="detail-section-title">Authorized User Details</h3>
                        <div className="detail-container">
                          <span className="detail-label">Name</span>
                          <span className="detail-value">{userDetailForProfitCenter.FirstName}</span>
                        </div>
                        <div className="detail-container">
                          <span className="detail-label">Email</span>
                          <span className="detail-value">{userDetailForProfitCenter.Email}</span>
                        </div>
                        <div className="detail-container">
                          <span className="detail-label">Phone</span>
                          <span className="detail-value">{userDetailForProfitCenter.Phone}</span>
                        </div>
                      </div>
                    </div>
                    <div className="detail-column flex-item-for-desktop-up-6-12">
                      <div className="detail-section">
                        <h3 className="detail-section-title">Assigned Clients</h3>
                        <div className="nested-list-container">
                          {this.renderNestedList(userDetailForProfitCenter.AssignedClients)}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              );
            case 'client':
              const clientDetailForProfitCenter = this.state.detail as ClientDetailForProfitCenter;
              return (
                <div>
                  <div className="detail-column-container">
                    <div className="detail-column flex-item-for-desktop-up-6-12">
                      <div className="detail-section">
                        <h3 className="detail-section-title">Client Details</h3>
                        <div className="detail-container">
                          <span className="detail-label">Name</span>
                          <span className="detail-value">{clientDetailForProfitCenter.Name}</span>
                        </div>
                        <div className="detail-container">
                          <span className="detail-label">Code</span>
                          <span className="detail-value">{clientDetailForProfitCenter.Code}</span>
                        </div>
                        <div className="detail-container">
                          <span className="detail-label">Contact</span>
                          <span className="detail-value">{clientDetailForProfitCenter.ContactName}</span>
                        </div>
                        <div className="detail-container">
                          <span className="detail-label">Email</span>
                          <span className="detail-value">{clientDetailForProfitCenter.ContactEmail}</span>
                        </div>
                        <div className="detail-container">
                          <span className="detail-label">Phone</span>
                          <span className="detail-value">{clientDetailForProfitCenter.ContactPhone}</span>
                        </div>
                      </div>
                    </div>
                    <div className="detail-column flex-item-for-desktop-up-6-12">
                      <div className="detail-section">
                        <h3 className="detail-section-title">Authorized Users</h3>
                        <div className="nested-list-container">
                          {this.renderNestedList(clientDetailForProfitCenter.AuthorizedUsers)}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              );
            default:
              return null;
          }
        default:
          return null;
      }
    })();

    const detail = !this.props.selectedCard
      ? null
      : this.state.detail === null
        ? (<div>Loading...</div>)
        : secondaryDetail;
    return (
      <div>
        {detail}
      </div>
    );
  }

  private fetch() {
    if (!this.url) {
      return this.setState({ detail: undefined });
    }

    getData(this.url, this.props.queryFilter)
    .then((response) => {
      this.setState({
        detail: response,
      });
    });
  }

  private renderNestedList(list: NestedList): JSX.Element[] {
    return list.Sections.map((section, i) => {
      const values = section.Values.map((value, j) => (
        <div
          key={j}
          className="nested-list-value"
        >{value}
        </div>
      ));
      return (
        <div
          key={i}
          className="nested-list-section"
        >
          <h4 className="nested-list-section-title">{section.Name}</h4>
          {values}
        </div>
      );
    });
  }
}
