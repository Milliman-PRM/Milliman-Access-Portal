import * as React from 'react';
import { connect } from 'react-redux';

import * as AccessActionCreators from './redux/action-creators';
import { AccessState } from './redux/store';

import { ClientWithStats } from '../models';
import { NavBar } from '../shared-components/navbar';

type ClientEntity = (ClientWithStats & { indent: 1 | 2 }) | 'divider';
interface ClientAdminProps {
  clients: ClientEntity[];
}

class ClientAdmin extends React.Component<ClientAdminProps & typeof AccessActionCreators> {
  private readonly currentView: string = document
    .getElementsByTagName('body')[0].getAttribute('data-nav-location');

  public componentDidMount() {
    this.props.fetchClients({});
  }

  public render() {
    return (
      <>
        <NavBar currentView={this.currentView} />
        <h3>Client Admin View</h3>
      </>
    );
  }
}

function mapStateToProps(state: AccessState): ClientAdminProps {
  const { data } = state;

  return {
    clients: null,
  };
}

export const ConnectedClientAdmin = connect(
  mapStateToProps,
  AccessActionCreators,
)(ClientAdmin);
