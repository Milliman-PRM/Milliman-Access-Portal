import { connect } from "react-redux";
import React from "react";
import * as AccessActionCreators from './redux/action-creators';
import { ClientWithStats } from "../models";
import { AccessState } from "./redux/store";
import { Dict } from "../shared-components/redux/store";

type ClientEntity = (ClientWithStats & { indent: 1 | 2 }) | 'divider';
interface ClientAdminProps {
  clients: ClientEntity[];
}

class ClientAdmin extends React.Component<ClientAdminProps & typeof AccessActionCreators> {
  public componentDidMount() {
    this.props.fetchClients({});
  }

  public render() {
    return (
      <>
        <h3>Client Admin View</h3>
      </>
    );
  }
}


function mapStateToProps(state: AccessState): ClientAdminProps {
  const { data } = state;

  return {
    clients: null
  }
}

export const ConnectedClientAdmin = connect(
  mapStateToProps,
  AccessActionCreators,
)(ClientAdmin);
