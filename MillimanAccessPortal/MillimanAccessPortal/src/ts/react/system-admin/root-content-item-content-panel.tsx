import '../../../scss/react/shared-components/content-panel.scss';

import { ajax } from 'jquery';
import * as React from 'react';

import { RootContentItemCard } from '../shared-components/root-content-item-card';
import { ContentPanelProps, RootContentItemInfo } from './interfaces';

export class RootContentItemContentPanel extends React.Component<ContentPanelProps<RootContentItemInfo>, {}> {
  public constructor(props) {
    super(props);
  }

  public render() {
    const items = this.props.data.map((item) => (
      <li
        key={item.Id}
        // tslint:disable-next-line:jsx-no-lambda
        onClick={() => this.props.select(item.Id)}
      >
        <RootContentItemCard
          data={item}
          selected={this.props.selected === item.Id}
        />
      </li>
    ));
    return (
      <div className="admin-panel-content-container">
        <ul className="admin-panel-content">
          {items}
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
      url: 'SystemAdmin/RootContentItems/',
    }).done((response: RootContentItemInfo[]) => {
      this.props.onFetch(response);
    }).fail((response) => {
      console.log(response.getResponseHeader('Warning')
        || 'An unknown error has occurred.');
    });
  }
}
