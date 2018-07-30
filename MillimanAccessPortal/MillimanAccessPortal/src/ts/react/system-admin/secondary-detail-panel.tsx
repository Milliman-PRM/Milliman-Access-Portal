import { ajax } from 'jquery';
import { isEqual } from 'lodash';
import * as React from 'react';

import { Entity } from '../shared-components/entity';
import { DataSource, QueryFilter } from '../shared-components/interfaces';
import { PrimaryDetail, SecondaryDetail, UserDetail } from './interfaces';

interface DetailPanelProps {
  primarySelectedDataSource: DataSource<Entity>;
  secondarySelectedDataSource: DataSource<Entity>;
  primarySelectedCard: number;
  secondarySelectedCard: number;
  queryFilter: QueryFilter;
  controller: string;
}

interface DetailPanelState {
  primaryDetail: PrimaryDetail;
  secondaryDetail: SecondaryDetail;
  prevPrimaryQuery: {
    dataSource: string;
    entityId: number;
  };
  prevSecondaryQuery: {
    dataSource: string;
    entityId: number;
  };
}

export class DetailPanel extends React.Component<DetailPanelProps, DetailPanelState> {

  // see https://github.com/reactjs/rfcs/issues/26#issuecomment-365744134
  public static getDerivedStateFromProps(
    nextProps: DetailPanelProps, prevState: DetailPanelState,
  ): Partial<DetailPanelState> {
    const nextPrimaryQuery = {
      dataSource: nextProps.primarySelectedDataSource.name,
      entityId: nextProps.primarySelectedCard,
    };
    const nextSecondaryQuery = {
      dataSource: nextProps.secondarySelectedDataSource.name,
      entityId: nextProps.secondarySelectedCard,
    };

    const primaryChange = !isEqual(nextPrimaryQuery, prevState.prevPrimaryQuery);
    const secondaryChange = !isEqual(nextPrimaryQuery, prevState.prevPrimaryQuery);

    const newState = {};
    if (primaryChange) {
      Object.assign(newState, {
        prevPrimaryQuery: nextPrimaryQuery,
        primaryDetail: null,
      });
    }
    if (secondaryChange) {
      Object.assign(newState, {
        prevSecondaryQuery: nextSecondaryQuery,
        secondaryDetail: null,
      });
    }
    return null;
  }

  private get url() {
    return this.props.selectedDataSource
      && `${this.props.controller}/${this.props.selectedDataSource.action}/`;
  }

  public constructor(props) {
    super(props);

    this.state = {
      primaryDetail: null,
      secondaryDetail: null,
      prevPrimaryQuery: null,
      prevSecondaryQuery: null,
    };
  }

  public componentDidMount() {
    this.props.setSelectedDataSource(this.props.dataSources[0] && this.props.dataSources[0].name);
    this.fetch();
  }

  public componentDidUpdate() {
    if (this.state.entities === null) {
      this.fetch();
    }
    if (this.props.selectedDataSource.name === null) {
      this.props.setSelectedDataSource(this.props.dataSources[0] && this.props.dataSources[0].name);
    }
  }


  public render() {
    // populate detail panel
    const detail = (() => {
      if (!this.state.primaryDetail) {
        return null;
      }
      switch (this.props.primaryDataSource) {
        case 'user':
          const userDetail = this.state.primaryDetail as UserDetail;
          return (
            <div>
              <h2>User Details</h2>
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
                    <span className="detail-label">(component placeholder)</span>
                  </div>
                  <div>
                    <span className="detail-label">(component placeholder)</span>
                  </div>
                </div>
                <div>
                  <h3>User Settings</h3>
                  <div>
                    <span className="detail-label">(component placeholder)</span>
                  </div>
                </div>
              </div>
            </div>
          );
        case 'client':
          return (
            <h2>Client Details</h2>
          );
        case 'profitCenter':
          return (
            <h2>Profit Center Details</h2>
          );
        default:
          return null;
      }
    })();

    return (
      <div>
        {detail}
      </div>
    );
  }

  private fetch() {
    if (!this.url) {
      return this.setState({ entities: [] });
    }

    ajax({
      data: this.props.queryFilter,
      method: 'GET',
      url: this.url,
    }).done((response) => {
      if (this.props.selectedDataSource) {
        this.setState({
          entities: this.props.selectedDataSource.processResponse(response),
        });
      }
    }).fail((response) => {
      throw new Error(response.getResponseHeader('Warning') || 'Unknown error');
    });
  }
}
