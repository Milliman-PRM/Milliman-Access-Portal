import '../../../scss/react/shared-components/content-panel.scss';

import { ajax } from 'jquery';
import * as React from 'react';

import { ClientSummary } from '../../view-models/content-publishing';
import { ClientCard } from '../shared-components/client-card';
import { ContentPanelProps } from './interfaces';

export class ClientContentPanel extends React.Component<ContentPanelProps<ClientSummary>, {}> {
  public constructor(props) {
    super(props);
  }

  public render() {
    const clients = this.props.data.map((client) => (
      <li
        key={client.Id}
        // tslint:disable-next-line:jsx-no-lambda
        onClick={() => this.props.select(client.Id)}
        style={this.props.selected === client.Id ? {fontWeight: 'bold'} : {}}
      >
        <ClientCard
          data={client}
        />
      </li>
    ));
    return (
      <div className="admin-panel-content-container">
        <ul className="admin-panel-content">
          {clients}
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
      url: 'SystemAdmin/Clients/',
    }).done((response: ClientSummary[]) => {
      this.props.onFetch(response);
    }).fail((response) => {
      console.log(response.getResponseHeader('Warning')
        || 'An unknown error has occurred.');
    });
  }
}
