import '../../../scss/react/system-admin/detail-panel.scss';

import * as moment from 'moment';
import * as React from 'react';

import { Toggle } from '../shared-components/form/toggle';
import { Guid, QueryFilter } from '../shared-components/interfaces';
import { ClientDetail, PrimaryDetail, ProfitCenterDetail, UserDetail } from './interfaces';
import { SystemAdminColumn, UserStatus } from './system-admin';
import { UserStatusDisplay } from './user-status-display';

interface PrimaryDetailPanelProps {
  selectedColumn: SystemAdminColumn;
  selectedCard: string;
  queryFilter: QueryFilter;
  detail: PrimaryDetail;
  onPushSystemAdmin: (event: React.MouseEvent<HTMLButtonElement>, checked: boolean) => void;
  checkedSystemAdmin: boolean;
  onPushSuspend: (event: React.MouseEvent<HTMLDivElement>) => void;
  checkedSuspended: boolean;
  onPushEnableUserAccount: (id: Guid, email: string) => void;
  status: UserStatus;
  doDomainLimitOpen: (event: React.MouseEvent<HTMLButtonElement>) => void;
}

export class PrimaryDetailPanel extends React.Component<PrimaryDetailPanelProps> {

  public render() {
    // populate detail panel
    const primaryDetail = (() => {
      if (!this.props.detail) {
        return null;
      }
      switch (this.props.selectedColumn) {
        case SystemAdminColumn.USER:
          const userDetail = this.props.detail as UserDetail;
          return (
            <div>
              <h2>User Details</h2>
              <div className="detail-column-container">
                <div className="detail-column flex-item-for-desktop-up-6-12">
                  <div className="detail-section">
                    <h3 className="detail-section-title">User Details</h3>
                    <div className="detail-container">
                      <span className="detail-label">First Name</span>
                      <span className="detail-value">{userDetail.firstName}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Last Name</span>
                      <span className="detail-value">{userDetail.lastName}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Company</span>
                      <span className="detail-value">{userDetail.employer}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Username</span>
                      <span className="detail-value">{userDetail.userName}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Email</span>
                      <span className="detail-value">{userDetail.email}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Phone</span>
                      <span className="detail-value">{userDetail.phone}</span>
                    </div>
                  </div>
                </div>
                <div className="detail-column flex-item-for-desktop-up-6-12">
                  <div className="detail-section">
                    <h3 className="detail-section-title">System Permissions</h3>
                    <div className="detail-container">
                      <button
                        name="systemAdminButton"
                        className={`systemAdminButton ${this.props.checkedSystemAdmin ? 'red-button' : 'blue-button'}`}
                        onClick={(event) => this.props.onPushSystemAdmin(event, !this.props.checkedSystemAdmin)}
                      >
                        {this.props.checkedSystemAdmin ?
                          <span>Revoke</span> :
                          <span>Enable</span>
                        }
                      </button>
                      <label htmlFor="systemAdminButton" style={{ fontSize: '1rem' }}>
                        System Admin
                      </label>
                    </div>
                  </div>
                  <div className="detail-section">
                    <h3 className="detail-section-title">User Settings</h3>
                    <div className="detail-container">
                      <Toggle
                        label={'Suspended'}
                        checked={this.props.checkedSuspended}
                        onClick={this.props.onPushSuspend}
                      />
                    </div>
                  </div>
                  <div className="detail-section">
                    <UserStatusDisplay
                      userId={this.props.detail.id}
                      userEmail={(this.props.detail as UserDetail).email}
                      status={this.props.status}
                      onPushEnableUserAccount={this.props.onPushEnableUserAccount}
                    />
                  </div>
                </div>
              </div>
            </div>
          );
        case SystemAdminColumn.CLIENT:
          const clientDetail = this.props.detail as ClientDetail;
          return (
            <div>
              <h2>Client Details</h2>
              <div className="detail-column-container">
                <div className="detail-column flex-item-for-desktop-up-6-12">
                  <div className="detail-section">
                    <h3 className="detail-section-title">Client Information</h3>
                    <div className="detail-container">
                      <span className="detail-label">Name</span>
                      <span className="detail-value">{clientDetail.name}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Code</span>
                      <span className="detail-value">{clientDetail.clientCode}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Contact</span>
                      <span className="detail-value">{clientDetail.clientContactName}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Email</span>
                      <span className="detail-value">{clientDetail.clientContactEmail}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Phone</span>
                      <span className="detail-value">{clientDetail.clientContactPhone}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Email Domain(s)</span>
                      <span className="detail-value">
                        <ul>
                          {clientDetail.acceptedEmailDomainList.map((x, i) => (<li key={i}>{x}</li>))}
                        </ul>
                      </span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Email Exception(s)</span>
                      <span className="detail-value">
                        <ul>
                          {clientDetail.acceptedEmailAddressExceptionList.map((x, i) => (<li key={i}>{x}</li>))}
                        </ul>
                      </span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Domain Limit</span>
                      <span className="detail-value">
                        {clientDetail.domainListCountLimit}
                        <button
                          className="link-button inline-link-button"
                          onClick={this.props.doDomainLimitOpen}
                        >
                          Change Domain Limit
                        </button>
                      </span>
                    </div>
                  </div>
                </div>
                <div className="detail-column flex-item-for-desktop-up-6-12">
                  <div className="detail-section">
                    <h3 className="detail-section-title">Billing Information</h3>
                    <div className="detail-container">
                      <span className="detail-label">Prof. Center</span>
                      <span className="detail-value">{clientDetail.profitCenter.name}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Office</span>
                      <span className="detail-value">{clientDetail.office}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Contact</span>
                      <span className="detail-value">{clientDetail.consultantName}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Email</span>
                      <span className="detail-value">{clientDetail.consultantEmail}</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          );
        case SystemAdminColumn.PROFIT_CENTER:
          const profitCenterDetail = this.props.detail as ProfitCenterDetail;
          return (
            <div>
              <h2>Profit Center Details</h2>
              <div className="detail-column-container">
                <div className="detail-column flex-item-for-desktop-up-6-12">
                  <div className="detail-section">
                    <h3 className="detail-section-title">Profit Center Information</h3>
                    <div className="detail-container">
                      <span className="detail-label">Name</span>
                      <span className="detail-value">{profitCenterDetail.name}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Code</span>
                      <span className="detail-value">{profitCenterDetail.code}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Office</span>
                      <span className="detail-value">{profitCenterDetail.office}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Contact</span>
                      <span className="detail-value">{profitCenterDetail.contactName}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Email</span>
                      <span className="detail-value">{profitCenterDetail.contactEmail}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Phone</span>
                      <span className="detail-value">{profitCenterDetail.contactPhone}</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          );
        default:
          return null;
      }
    })();

    const detail = !this.props.selectedCard
      ? null
      : this.props.detail === null
        ? (<div>Loading...</div>)
        : primaryDetail;
    return (
      <div>
        {detail}
      </div>
    );
  }
}
