import { ajax } from 'jquery';
import { isEqual } from 'lodash';
import * as React from 'react';

import { Entity } from '../shared-components/entity';
import { DataSource, QueryFilter } from '../shared-components/interfaces';
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
                  <div style={{display: 'flex'}}>
                    <div className="flex-item-for-desktop-up-6-12">
                      <div>
                        <h3>Client Details</h3>
                        <div>
                          <span className="detail-label">Client Name</span>
                          <span className="detail-value">{clientDetailForUser.ClientName}</span>
                        </div>
                        <div>
                          <span className="detail-label">Client Code</span>
                          <span className="detail-value">{clientDetailForUser.ClientCode}</span>
                        </div>
                      </div>
                    </div>
                    <div className="flex-item-for-desktop-up-6-12">
                      <div>
                        <h3>Client/User Roles</h3>
                        <div>
                          <span>(component placeholder)</span>
                        </div>
                        <div>
                          <span>(component placeholder)</span>
                        </div>
                        <div>
                          <span>(component placeholder)</span>
                        </div>
                        <div>
                          <span>(component placeholder)</span>
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
                  <div style={{display: 'flex'}}>
                    <div className="flex-item-for-desktop-up-6-12">
                      <div>
                        <h3>Authorized Content Details</h3>
                        <div>
                          <span className="detail-label">Content Name</span>
                          <span className="detail-value">{rootContentItemDetailForUser.ContentName}</span>
                        </div>
                        <div>
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
                  <div style={{display: 'flex'}}>
                    <div className="flex-item-for-desktop-up-6-12">
                      <div>
                        <h3>User Details</h3>
                        <div>
                          <span className="detail-label">First Name</span>
                          <span className="detail-value">{userDetailForClient.FirstName}</span>
                        </div>
                        <div>
                          <span className="detail-label">Last Name</span>
                          <span className="detail-value">{userDetailForClient.LastName}</span>
                        </div>
                        <div>
                          <span className="detail-label">Company</span>
                          <span className="detail-value">{userDetailForClient.Employer}</span>
                        </div>
                        <div>
                          <span className="detail-label">Username</span>
                          <span className="detail-value">{userDetailForClient.UserName}</span>
                        </div>
                        <div>
                          <span className="detail-label">Email</span>
                          <span className="detail-value">{userDetailForClient.Email}</span>
                        </div>
                        <div>
                          <span className="detail-label">Phone</span>
                          <span className="detail-value">{userDetailForClient.Phone}</span>
                        </div>
                      </div>
                    </div>
                    <div className="flex-item-for-desktop-up-6-12">
                      <div>
                        <h3>Client/User Roles</h3>
                        <div>
                          <span>(component placeholder)</span>
                        </div>
                        <div>
                          <span>(component placeholder)</span>
                        </div>
                        <div>
                          <span>(component placeholder)</span>
                        </div>
                        <div>
                          <span>(component placeholder)</span>
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
                  <div style={{display: 'flex'}}>
                    <div className="flex-item-for-desktop-up-6-12">
                      <div>
                        <h3>Content Item Details</h3>
                        <div>
                          <span className="detail-label">Name</span>
                          <span className="detail-value">{rootContentItemDetailForClient.ContentName}</span>
                        </div>
                        <div>
                          <span className="detail-label">Content Type</span>
                          <span className="detail-value">{rootContentItemDetailForClient.ContentType}</span>
                        </div>
                        <div>
                          <span className="detail-label">Description</span>
                          <span className="detail-value">{rootContentItemDetailForClient.Description}</span>
                        </div>
                        <div>
                          <span className="detail-label">Last Updated</span>
                          <span className="detail-value">{rootContentItemDetailForClient.LastUpdated}</span>
                        </div>
                        <div>
                          <span className="detail-label">Last Accessed</span>
                          <span className="detail-value">{rootContentItemDetailForClient.LastAccessed}</span>
                        </div>
                        <div>
                          <span>(component placeholder)</span>
                        </div>
                      </div>
                    </div>
                    <div className="flex-item-for-desktop-up-6-12">
                      <div>
                        <h3>Selection Groups</h3>
                        <div>
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
                  <div style={{display: 'flex'}}>
                    <div className="flex-item-for-desktop-up-6-12">
                      <div>
                        <h3>Authorized User Details</h3>
                        <div>
                          <span className="detail-label">Name</span>
                          <span className="detail-value">{userDetailForProfitCenter.FirstName}</span>
                        </div>
                        <div>
                          <span className="detail-label">Email</span>
                          <span className="detail-value">{userDetailForProfitCenter.Email}</span>
                        </div>
                        <div>
                          <span className="detail-label">Phone</span>
                          <span className="detail-value">{userDetailForProfitCenter.Phone}</span>
                        </div>
                      </div>
                    </div>
                    <div className="flex-item-for-desktop-up-6-12">
                      <div>
                        <h3>Assigned Clients</h3>
                        <div>
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
                  <div style={{display: 'flex'}}>
                    <div className="flex-item-for-desktop-up-6-12">
                      <div>
                        <h3>Client Details</h3>
                        <div>
                          <span className="detail-label">Name</span>
                          <span className="detail-value">{clientDetailForProfitCenter.Name}</span>
                        </div>
                        <div>
                          <span className="detail-label">Code</span>
                          <span className="detail-value">{clientDetailForProfitCenter.Code}</span>
                        </div>
                        <div>
                          <span className="detail-label">Contact</span>
                          <span className="detail-value">{clientDetailForProfitCenter.ContactName}</span>
                        </div>
                        <div>
                          <span className="detail-label">Email</span>
                          <span className="detail-value">{clientDetailForProfitCenter.ContactEmail}</span>
                        </div>
                        <div>
                          <span className="detail-label">Phone</span>
                          <span className="detail-value">{clientDetailForProfitCenter.ContactPhone}</span>
                        </div>
                      </div>
                    </div>
                    <div className="flex-item-for-desktop-up-6-12">
                      <div>
                        <h3>Authorized Users</h3>
                        <div>
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

    ajax({
      data: this.props.queryFilter,
      method: 'GET',
      url: this.url,
    }).done((response) => {
      this.setState({
        detail: response,
      });
    }).fail((response) => {
      throw new Error(response.getResponseHeader('Warning') || 'Unknown error');
    });
  }

  private renderNestedList(list: NestedList): JSX.Element[] {
    return list.Sections.map((section, i) => {
      const values = section.Values.map((value, j) => (
        <div
          key={j}
        >{value}
        </div>
      ));
      return (
        <div
          key={i}
        >
          <h4>{section.Name}</h4>
          {values}
        </div>
      );
    });
  }
}
