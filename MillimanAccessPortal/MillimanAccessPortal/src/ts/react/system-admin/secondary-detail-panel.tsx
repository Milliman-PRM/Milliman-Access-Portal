import * as React from 'react';

import { Guid, QueryFilter, RoleEnum } from '../shared-components/interfaces';
import { Toggle } from '../shared-components/toggle';
import {
  ClientDetailForProfitCenter, ClientDetailForUser, NestedList, RootContentItemDetailForClient,
  RootContentItemDetailForUser, SecondaryDetail, UserDetailForClient, UserDetailForProfitCenter,
} from './interfaces';
import { SystemAdminColumn } from './system-admin';

interface SecondaryDetailPanelProps {
  primarySelectedColumn: SystemAdminColumn;
  secondarySelectedColumn: SystemAdminColumn;
  selectedCard: string;
  queryFilter: QueryFilter;
  detail: SecondaryDetail;
  onCancelPublication: (event: React.MouseEvent<HTMLElement>) => void;
  onCancelReduction: (event: React.MouseEvent<HTMLElement>, id: Guid) => void;
  onPushUserClient: (event: React.ChangeEvent<HTMLInputElement>, role: RoleEnum) => void;
  checkedClientAdmin: boolean;
  checkedContentPublisher: boolean;
  checkedAccessAdmin: boolean;
  checkedContentUser: boolean;
  onPushSuspend: (event: React.ChangeEvent<HTMLInputElement>) => void;
  checkedSuspended: boolean;
}

export class SecondaryDetailPanel extends React.Component<SecondaryDetailPanelProps> {
  public render() {
    // populate detail panel
    const secondaryDetail = (() => {
      if (!this.props.detail) {
        return null;
      }
      switch (this.props.primarySelectedColumn) {
        case SystemAdminColumn.USER:
          switch (this.props.secondarySelectedColumn) {
            case SystemAdminColumn.CLIENT:
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
                          <Toggle
                            label={'Client Admin'}
                            checked={this.props.checkedClientAdmin}
                            onChange={(event) => this.props.onPushUserClient(event, RoleEnum.Admin)}
                          />
                        </div>
                        <div className="detail-container">
                          <Toggle
                            label={'Content Access Admin'}
                            checked={this.props.checkedAccessAdmin}
                            onChange={(event) => this.props.onPushUserClient(event, RoleEnum.ContentAccessAdmin)}
                          />
                        </div>
                        <div className="detail-container">
                          <Toggle
                            label={'Content Publisher'}
                            checked={this.props.checkedContentPublisher}
                            onChange={(event) => this.props.onPushUserClient(event, RoleEnum.ContentPublisher)}
                          />
                        </div>
                        <div className="detail-container">
                          <Toggle
                            label={'Content Eligible'}
                            checked={this.props.checkedContentUser}
                            onChange={(event) => this.props.onPushUserClient(event, RoleEnum.ContentUser)}
                          />
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              );
            case SystemAdminColumn.ROOT_CONTENT_ITEM:
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
        case SystemAdminColumn.CLIENT:
          switch (this.props.secondarySelectedColumn) {
            case SystemAdminColumn.USER:
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
                          <Toggle
                            label={'Client Admin'}
                            checked={this.props.checkedClientAdmin}
                            onChange={(event) => this.props.onPushUserClient(event, RoleEnum.Admin)}
                          />
                        </div>
                        <div className="detail-container">
                          <Toggle
                            label={'Content Access Admin'}
                            checked={this.props.checkedAccessAdmin}
                            onChange={(event) => this.props.onPushUserClient(event, RoleEnum.ContentAccessAdmin)}
                          />
                        </div>
                        <div className="detail-container">
                          <Toggle
                            label={'Content Publisher'}
                            checked={this.props.checkedContentPublisher}
                            onChange={(event) => this.props.onPushUserClient(event, RoleEnum.ContentPublisher)}
                          />
                        </div>
                        <div className="detail-container">
                          <Toggle
                            label={'Content Eligible'}
                            checked={this.props.checkedContentUser}
                            onChange={(event) => this.props.onPushUserClient(event, RoleEnum.ContentUser)}
                          />
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              );
            case SystemAdminColumn.ROOT_CONTENT_ITEM:
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
                          <Toggle
                            label={'Suspended'}
                            checked={this.props.checkedSuspended}
                            onChange={this.props.onPushSuspend}
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
        case SystemAdminColumn.PROFIT_CENTER:
          switch (this.props.secondarySelectedColumn) {
            case SystemAdminColumn.USER:
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
            case SystemAdminColumn.CLIENT:
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
