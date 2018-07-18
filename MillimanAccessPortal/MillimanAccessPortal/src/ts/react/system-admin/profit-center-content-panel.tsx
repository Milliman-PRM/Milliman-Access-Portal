import '../../../scss/react/shared-components/content-panel.scss';

import { ajax } from 'jquery';
import * as React from 'react';

import { ProfitCenterCard } from '../shared-components/profit-center-card';
import { ContentPanelProps, ProfitCenterInfo } from './interfaces';

export class ProfitCenterContentPanel extends React.Component<ContentPanelProps<ProfitCenterInfo>, {}> {
  public constructor(props) {
    super(props);
  }

  public render() {
    const profitCenters = this.props.data.map((profitCenter) => (
      <li
        key={profitCenter.Id}
        // tslint:disable-next-line:jsx-no-lambda
        onClick={() => this.props.select(profitCenter.Id)}
      >
        <ProfitCenterCard
          data={profitCenter}
          selected={this.props.selected === profitCenter.Id}
        />
      </li>
    ));
    return (
      <div className="admin-panel-content-container">
        <ul className="admin-panel-content">
          {profitCenters}
        </ul>
      </div>
    );
  }

  public componentDidMount() {
    this.fetch();
  }

  public componentWillUnmount() {
    this.props.onFetch([]);
  }

  public fetch() {
    ajax({
      data: this.props.queryFilter,
      method: 'GET',
      url: 'SystemAdmin/ProfitCenters/',
    }).done((response: ProfitCenterInfo[]) => {
      this.props.onFetch(response);
    }).fail((response) => {
      console.log(response.getResponseHeader('Warning')
        || 'An unknown error has occurred.');
    });
  }
}
