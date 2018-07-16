import '../../../scss/react/shared-components/content-panel.scss';

import { ajax } from 'jquery';
import * as React from 'react';

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
        style={this.props.selected === profitCenter.Id ? {fontWeight: 'bold'} : {}}
      >
        {profitCenter.Name}
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
