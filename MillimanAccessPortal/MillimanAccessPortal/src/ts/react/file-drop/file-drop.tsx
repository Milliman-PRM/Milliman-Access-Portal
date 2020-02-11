import * as React from 'react';
import { connect } from 'react-redux';
import ReduxToastr from 'react-redux-toastr';

import * as FileDropActionCreator from './redux/action-creators';
import * as Selector from './redux/selectors';
import * as State from './redux/store';

import { Client, ClientWithStats } from '../models';
import { CardPanel } from '../shared-components/card-panel/card-panel';
import { PanelSectionToolbar, PanelSectionToolbarButtons } from '../shared-components/card-panel/panel-sections';
import { Card } from '../shared-components/card/card';
import { CardSectionMain, CardSectionStats, CardText } from '../shared-components/card/card-sections';
import { CardStat } from '../shared-components/card/card-stat';
import { Filter } from '../shared-components/filter';
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
    this.props.fetchClients({});
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
        {this.renderClientPanel()}
      </>
    );
  }

  private renderClientPanel() {
    const { clients, selected, filters, pending, cardAttributes } = this.props;
    return (
      <CardPanel
        entities={clients}
        loading={pending.async.clients}
        renderEntity={(entity, key) => {
          if (entity === 'divider') {
            return <div className="hr" key={key} />;
          }
          const card = cardAttributes.client[entity.id];
          return (
            <Card
              key={key}
              selected={selected.client === entity.id}
              disabled={card.disabled}
              onSelect={() => {
                // TODO: Update this section once all of the necessary actions and data are available
                // if (this.props.formChangesPending || this.props.uploadChangesPending) {
                //   this.props.openModifiedFormModal({
                //     afterFormModal:
                //     {
                //       entityToSelect: entity.id,
                //       entityType: 'Select Client',
                //     },
                //   });
                // } else {
                //   if (selected.client !== entity.id) {
                //     this.props.fetchItems({ clientId: entity.id });
                //   }
                //   this.props.selectClient({ id: entity.id });
                // }
                this.props.selectClient({ id: entity.id });
              }}
              indentation={entity.indent}
            >
              <CardSectionMain>
                <CardText text={entity.name} subtext={entity.code} />
                <CardSectionStats>
                  <CardStat
                    name={'Reports'}
                    value={entity.contentItemCount}
                    icon={'reports'}
                  />
                  <CardStat
                    name={'Users'}
                    value={entity.userCount}
                    icon={'user'}
                  />
                </CardSectionStats>
              </CardSectionMain>
            </Card>
          );
        }}
      >
        <h3 className="admin-panel-header">Clients</h3>
        <PanelSectionToolbar>
          <Filter
            placeholderText={'Filter clients...'}
            setFilterText={(text) => this.props.setFilterTextClient({ text })}
            filterText={filters.client.text}
          />
          <PanelSectionToolbarButtons>
            <div id="icons" />
          </PanelSectionToolbarButtons>
        </PanelSectionToolbar>
      </CardPanel>
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
