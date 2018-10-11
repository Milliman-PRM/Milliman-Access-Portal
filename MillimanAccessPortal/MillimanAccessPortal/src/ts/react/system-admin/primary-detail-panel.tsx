import '../../../scss/react/system-admin/detail-panel.scss';

import { isEqual } from 'lodash';
import * as React from 'react';

import { getData } from '../../shared';
import { Entity } from '../shared-components/entity';
import { ImmediateToggle } from '../shared-components/immediate-toggle';
import { DataSource, QueryFilter, RoleEnum } from '../shared-components/interfaces';
import { ClientDetail, PrimaryDetail, ProfitCenterDetail, UserDetail } from './interfaces';

interface PrimaryDetailPanelProps {
  controller: string;
  selectedDataSource: DataSource<Entity>;
  selectedCard: string;
  queryFilter: QueryFilter;
  detail: PrimaryDetail;
}

export class PrimaryDetailPanel extends React.Component<PrimaryDetailPanelProps> {

  public render() {
    // populate detail panel
    const primaryDetail = (() => {
      if (!this.props.detail) {
        return null;
      }
      switch (this.props.selectedDataSource.name) {
        case 'user':
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
                      <span className="detail-value">{userDetail.FirstName}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Last Name</span>
                      <span className="detail-value">{userDetail.LastName}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Company</span>
                      <span className="detail-value">{userDetail.Employer}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Username</span>
                      <span className="detail-value">{userDetail.UserName}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Email</span>
                      <span className="detail-value">{userDetail.Email}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Phone</span>
                      <span className="detail-value">{userDetail.Phone}</span>
                    </div>
                  </div>
                </div>
                <div className="detail-column flex-item-for-desktop-up-6-12">
                  <div className="detail-section">
                    <h3 className="detail-section-title">System Permissions</h3>
                    <div className="detail-container">
                      <ImmediateToggle
                        controller={this.props.controller}
                        action={'SystemRole'}
                        queryFilter={this.props.queryFilter}
                        label={'System Admin'}
                        data={{ role: RoleEnum.Admin }}
                      />
                    </div>
                  </div>
                  <div className="detail-section">
                    <h3 className="detail-section-title">User Settings</h3>
                    <div className="detail-container">
                      <ImmediateToggle
                        controller={this.props.controller}
                        action={'UserSuspendedStatus'}
                        queryFilter={this.props.queryFilter}
                        label={'Suspended'}
                        data={{ }}
                      />
                    </div>
                  </div>
                </div>
              </div>
            </div>
          );
        case 'client':
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
                      <span className="detail-value">{clientDetail.ClientName}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Code</span>
                      <span className="detail-value">{clientDetail.ClientCode}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Contact</span>
                      <span className="detail-value">{clientDetail.ClientContactName}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Email</span>
                      <span className="detail-value">{clientDetail.ClientContactEmail}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Phone</span>
                      <span className="detail-value">{clientDetail.ClientContactPhone}</span>
                    </div>
                  </div>
                </div>
                <div className="detail-column flex-item-for-desktop-up-6-12">
                  <div className="detail-section">
                    <h3 className="detail-section-title">Billing Information</h3>
                    <div className="detail-container">
                      <span className="detail-label">Prof. Center</span>
                      <span className="detail-value">{clientDetail.ProfitCenter}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Office</span>
                      <span className="detail-value">{clientDetail.Office}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Contact</span>
                      <span className="detail-value">{clientDetail.ConsultantName}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Email</span>
                      <span className="detail-value">{clientDetail.ConsultantEmail}</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          );
        case 'profitCenter':
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
                      <span className="detail-value">{profitCenterDetail.Name}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Code</span>
                      <span className="detail-value">{profitCenterDetail.Code}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Office</span>
                      <span className="detail-value">{profitCenterDetail.Office}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Contact</span>
                      <span className="detail-value">{profitCenterDetail.ContactName}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Email</span>
                      <span className="detail-value">{profitCenterDetail.ContactEmail}</span>
                    </div>
                    <div className="detail-container">
                      <span className="detail-label">Phone</span>
                      <span className="detail-value">{profitCenterDetail.ContactPhone}</span>
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
