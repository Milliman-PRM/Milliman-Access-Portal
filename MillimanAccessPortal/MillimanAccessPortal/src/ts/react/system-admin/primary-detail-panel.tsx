import '../../../scss/react/system-admin/detail-panel.scss';

import * as React from 'react';

import { QueryFilter } from '../shared-components/interfaces';
import { Toggle } from '../shared-components/toggle';
import { ClientDetail, PrimaryDetail, ProfitCenterDetail, UserDetail } from './interfaces';
import { SystemAdminColumn } from './system-admin';

interface PrimaryDetailPanelProps {
  selectedColumn: SystemAdminColumn;
  selectedCard: string;
  queryFilter: QueryFilter;
  detail: PrimaryDetail;
  onPushSystemAdmin: (event: React.MouseEvent<HTMLDivElement>) => void;
  checkedSystemAdmin: boolean;
  onPushSuspend: (event: React.MouseEvent<HTMLDivElement>) => void;
  checkedSuspended: boolean;
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
                      <Toggle
                        label={'System Admin'}
                        checked={this.props.checkedSystemAdmin}
                        onClick={this.props.onPushSystemAdmin}
                      />
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
                      <span className="detail-value">{clientDetail.clientName}</span>
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
                  </div>
                </div>
                <div className="detail-column flex-item-for-desktop-up-6-12">
                  <div className="detail-section">
                    <h3 className="detail-section-title">Billing Information</h3>
                    <div className="detail-container">
                      <span className="detail-label">Prof. Center</span>
                      <span className="detail-value">{clientDetail.profitCenter}</span>
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
