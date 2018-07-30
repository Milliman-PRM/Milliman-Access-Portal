import '../../../scss/react/system-admin/detail-panel.scss';

import { ajax } from 'jquery';
import { isEqual } from 'lodash';
import * as React from 'react';

import { Entity } from '../shared-components/entity';
import { DataSource, QueryFilter } from '../shared-components/interfaces';
import { ClientDetail, PrimaryDetail, ProfitCenterDetail, UserDetail } from './interfaces';

interface PrimaryDetailPanelProps {
  controller: string;
  selectedDataSource: DataSource<Entity>;
  selectedCard: number;
  queryFilter: QueryFilter;
}

interface PrimaryDetailPanelState {
  detail: PrimaryDetail;
  prevQuery: {
    dataSource: string;
    entityId: number;
  };
}

export class PrimaryDetailPanel extends React.Component<PrimaryDetailPanelProps, PrimaryDetailPanelState> {

  // see https://github.com/reactjs/rfcs/issues/26#issuecomment-365744134
  public static getDerivedStateFromProps(
    nextProps: PrimaryDetailPanelProps, prevState: PrimaryDetailPanelState,
  ): Partial<PrimaryDetailPanelState> {
    const nextQuery = {
      dataSource: nextProps.selectedDataSource.name,
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
      && `${this.props.controller}/${this.props.selectedDataSource.detailAction}/`;
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
    const primaryDetail = (() => {
      if (!this.state.detail) {
        return null;
      }
      switch (this.props.selectedDataSource.name) {
        case 'user':
          const userDetail = this.state.detail as UserDetail;
          return (
            <div>
              <h2>User Details</h2>
              <div style={{display: 'flex'}}>
                <div className="flex-item-for-desktop-up-6-12">
                  <div>
                    <h3>User Details</h3>
                    <div>
                      <span className="detail-label">First Name</span>
                      <span className="detail-value">{userDetail.FirstName}</span>
                    </div>
                    <div>
                      <span className="detail-label">Last Name</span>
                      <span className="detail-value">{userDetail.LastName}</span>
                    </div>
                    <div>
                      <span className="detail-label">Company</span>
                      <span className="detail-value">{userDetail.Employer}</span>
                    </div>
                    <div>
                      <span className="detail-label">Username</span>
                      <span className="detail-value">{userDetail.UserName}</span>
                    </div>
                    <div>
                      <span className="detail-label">Email</span>
                      <span className="detail-value">{userDetail.Email}</span>
                    </div>
                    <div>
                      <span className="detail-label">Phone</span>
                      <span className="detail-value">{userDetail.Phone}</span>
                    </div>
                  </div>
                </div>
                <div className="flex-item-for-desktop-up-6-12">
                  <div>
                    <h3>System Permissions</h3>
                    <div>
                      <span>(component placeholder)</span>
                    </div>
                    <div>
                      <span>(component placeholder)</span>
                    </div>
                  </div>
                  <div>
                    <h3>User Settings</h3>
                    <div>
                      <span>(component placeholder)</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          );
        case 'client':
          const clientDetail = this.state.detail as ClientDetail;
          return (
            <div>
              <h2>Client Details</h2>
              <div style={{display: 'flex'}}>
                <div className="flex-item-for-desktop-up-6-12">
                  <div>
                    <h3>Client Information</h3>
                    <div>
                      <span className="detail-label">Name</span>
                      <span className="detail-value">{clientDetail.ClientName}</span>
                    </div>
                    <div>
                      <span className="detail-label">Code</span>
                      <span className="detail-value">{clientDetail.ClientCode}</span>
                    </div>
                    <div>
                      <span className="detail-label">Contact</span>
                      <span className="detail-value">{clientDetail.ClientContactName}</span>
                    </div>
                    <div>
                      <span className="detail-label">Email</span>
                      <span className="detail-value">{clientDetail.ClientContactEmail}</span>
                    </div>
                    <div>
                      <span className="detail-label">Phone</span>
                      <span className="detail-value">{clientDetail.ClientContactPhone}</span>
                    </div>
                  </div>
                </div>
                <div className="flex-item-for-desktop-up-6-12">
                  <div>
                    <h3>Billing Information</h3>
                    <div>
                      <span className="detail-label">Prof. Center</span>
                      <span className="detail-value">{clientDetail.ProfitCenter}</span>
                    </div>
                    <div>
                      <span className="detail-label">Office</span>
                      <span className="detail-value">{clientDetail.Office}</span>
                    </div>
                    <div>
                      <span className="detail-label">Contact</span>
                      <span className="detail-value">{clientDetail.ConsultantName}</span>
                    </div>
                    <div>
                      <span className="detail-label">Email</span>
                      <span className="detail-value">{clientDetail.ConsultantEmail}</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          );
        case 'profitCenter':
          const profitCenterDetail = this.state.detail as ProfitCenterDetail;
          return (
            <div>
              <h2>Profit Center Details</h2>
              <div style={{display: 'flex'}}>
                <div className="flex-item-for-desktop-up-6-12">
                  <div>
                    <h3>Profit Center Information</h3>
                    <div>
                      <span className="detail-label">Name</span>
                      <span className="detail-value">{profitCenterDetail.Name}</span>
                    </div>
                    <div>
                      <span className="detail-label">Office</span>
                      <span className="detail-value">{profitCenterDetail.Office}</span>
                    </div>
                    <div>
                      <span className="detail-label">Contact</span>
                      <span className="detail-value">{profitCenterDetail.ContactName}</span>
                    </div>
                    <div>
                      <span className="detail-label">Email</span>
                      <span className="detail-value">{profitCenterDetail.ContactEmail}</span>
                    </div>
                    <div>
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
      : this.state.detail === null
        ? (<div>Loading...</div>)
        : primaryDetail;
    return (
      <div>
        {detail}
      </div>
    );
  }

  private fetch() {
    if (!this.url) {
      return;
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
}
