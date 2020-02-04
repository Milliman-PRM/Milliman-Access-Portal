import * as React from 'react';
import { connect } from 'react-redux';
import ReduxToastr from 'react-redux-toastr';

import * as FileDropActionCreator from './redux/action-creators';
import * as Selector from './redux/selectors';
import * as State from './redux/store';

import { Client, ClientWithStats } from '../models';
import { NavBar } from '../shared-components/navbar';

type ClientEntity = (ClientWithStats & { indent: 1 | 2 }) | 'divider';

interface FileDropProps {
  clients: ClientEntity[];
  selected: State.FileDropSelectedState;
  cardAttributes: State.FileDropCardAttributesState;
  pending: State.FileDropPendingState;
  filters: State.FileDropFilterState;
  activeSelectedClient: Client;
}

class FileDrop extends React.Component<FileDropProps & typeof FileDropActionCreator> {
  private readonly currentView: string = document
    .getElementsByTagName('body')[0].getAttribute('data-nav-location');

  public componentDidMount() {
    this.props.scheduleStatusRefresh({ delay: 0 });
    this.props.scheduleSessionCheck({ delay: 0 });

    // TODO: Implement these actions properly
    // this.props.fetchGlobalData({});
    // this.props.fetchClients({});
  }

  public render() {
    return (
      <>
        <ReduxToastr
          timeOut={5000}
          newestOnTop={false}
          position="bottom-center"
          transitionIn="fadeIn"
          transitionOut="fadeOut"
        />
        <NavBar currentView={this.currentView} />
      </>
    );
  }
}

function mapStateToProps(state: State.FileDropState): FileDropProps {
  const { data, selected, cardAttributes, pending, filters } = state;

  return {
    clients: Selector.clientEntities(state),
    selected,
    cardAttributes,
    pending,
    filters,
    activeSelectedClient: Selector.activeSelectedClient(state),
  };
}

export const ConnectedFileDrop = connect(
  mapStateToProps,
  FileDropActionCreator,
)(FileDrop);
