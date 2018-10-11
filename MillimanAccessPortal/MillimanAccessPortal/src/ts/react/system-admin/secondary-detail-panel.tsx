import { isEqual } from 'lodash';
import * as React from 'react';

import { getData, postData } from '../../shared';
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
  selectedCard: string;
  queryFilter: QueryFilter;
  detail: SecondaryDetail;
  onCancelPublication: (event: React.MouseEvent<HTMLElement>) => void;
  onCancelReduction: (event: React.MouseEvent<HTMLElement>, id: string) => void;
  onFetchUserClient: (role: RoleEnum) => void;
  onPushUserClient: (role: RoleEnum) => void;
  checkedClientAdmin: boolean;
  checkedContentPublisher: boolean;
  checkedAccessAdmin: boolean;
  checkedContentUser: boolean;
  onFetchSuspend: (role: RoleEnum) => void;
  onPushSuspend: (role: RoleEnum) => void;
  checkedSuspended: boolean;
}

export class SecondaryDetailPanel extends React.Component<SecondaryDetailPanelProps> {
  public render() {
    // populate detail panel
    const secondaryDetail = (() => {
      if (!this.props.detail) {
        return null;
      }
      switch (this.props.primarySelectedDataSource.name) {
        case 'user':
          switch (this.props.secondarySelectedDataSource.name) {
            case 'client':
              const clientDetailForUser = this.props.detail as ClientDetailForUser;
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
                            queryFilter={this.props.queryFilter}
                            label={'Client Admin'}
                            data={RoleEnum.Admin}
                            checked={this.props.checkedClientAdmin}
                            onFetch={this.props.onFetchUserClient}
                            onPush={this.props.onPushUserClient}
                          />
                        </div>
                        <div className="detail-container">
                          <ImmediateToggle
                            queryFilter={this.props.queryFilter}
                            label={'Content Access Admin'}
                            data={RoleEnum.ContentAccessAdmin}
                            checked={this.props.checkedAccessAdmin}
                            onFetch={this.props.onFetchUserClient}
                            onPush={this.props.onPushUserClient}
                          />
                        </div>
                        <div className="detail-container">
                          <ImmediateToggle
                            queryFilter={this.props.queryFilter}
                            label={'Content Publisher'}
                            data={RoleEnum.ContentPublisher}
                            checked={this.props.checkedContentPublisher}
                            onFetch={this.props.onFetchUserClient}
                            onPush={this.props.onPushUserClient}
                          />
                        </div>
                        <div className="detail-container">
                          <ImmediateToggle
                            queryFilter={this.props.queryFilter}
                            label={'Content Eligible'}
                            data={RoleEnum.ContentUser}
                            checked={this.props.checkedContentUser}
                            onFetch={this.props.onFetchUserClient}
                            onPush={this.props.onPushUserClient}
                          />
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              );
            case 'rootContentItem':
              const rootContentItemDetailForUser = this.props.detail as RootContentItemDetailForUser;
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
              const userDetailForClient = this.props.detail as UserDetailForClient;
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
                            queryFilter={this.props.queryFilter}
                            label={'Client Admin'}
                            data={RoleEnum.Admin}
                            checked={this.props.checkedClientAdmin}
                            onFetch={this.props.onFetchUserClient}
                            onPush={this.props.onPushUserClient}
                          />
                        </div>
                        <div className="detail-container">
                          <ImmediateToggle
                            queryFilter={this.props.queryFilter}
                            label={'Content Access Admin'}
                            data={RoleEnum.ContentAccessAdmin}
                            checked={this.props.checkedAccessAdmin}
                            onFetch={this.props.onFetchUserClient}
                            onPush={this.props.onPushUserClient}
                          />
                        </div>
                        <div className="detail-container">
                          <ImmediateToggle
                            queryFilter={this.props.queryFilter}
                            label={'Content Publisher'}
                            data={RoleEnum.ContentPublisher}
                            checked={this.props.checkedContentPublisher}
                            onFetch={this.props.onFetchUserClient}
                            onPush={this.props.onPushUserClient}
                          />
                        </div>
                        <div className="detail-container">
                          <ImmediateToggle
                            queryFilter={this.props.queryFilter}
                            label={'Content Eligible'}
                            data={RoleEnum.ContentUser}
                            checked={this.props.checkedContentUser}
                            onFetch={this.props.onFetchUserClient}
                            onPush={this.props.onPushUserClient}
                          />
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              );
            case 'rootContentItem':
              const rootContentItemDetailForClient = this.props.detail as RootContentItemDetailForClient;
              const publishingStatus = rootContentItemDetailForClient.IsPublishing
                ? (
                  <span className="detail-value">
                    Yes (
                      <a
                        href={''}
                        onClick={this.props.onCancelPublication}
                      >
                        Cancel
                      </a>
                    )
                  </span>
                )
                : (
                  <span className="detail-value">
                    No
                  </span>
                );
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
                          <span className="detail-label">Reducing</span>
                          {publishingStatus}
                        </div>
                        <div className="detail-container">
                          <ImmediateToggle
                            queryFilter={this.props.queryFilter}
                            label={'Suspended'}
                            data={{ }}
                            checked={this.props.checkedSuspended}
                            onFetch={this.props.onFetchSuspend}
                            onPush={this.props.onPushSuspend}
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
              const userDetailForProfitCenter = this.props.detail as UserDetailForProfitCenter;
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
              const clientDetailForProfitCenter = this.props.detail as ClientDetailForProfitCenter;
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
      : this.props.detail === null
        ? (<div>Loading...</div>)
        : secondaryDetail;
    return (
      <div>
        {detail}
      </div>
    );
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
      const cancelText = section.Marked
        ? (
          <span>
            (
            <a
              href={''}
              onClick={(event) => this.props.onCancelReduction(event, section.Id)}
            >
              Cancel
            </a>
            )
          </span>
        )
        : null;
      return (
        <div
          key={i}
          className="nested-list-section"
        >
          <h4 className="nested-list-section-title">{section.Name} {cancelText}</h4>
          {values}
        </div>
      );
    });
  }
}
