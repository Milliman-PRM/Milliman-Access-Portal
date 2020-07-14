import * as React from 'react';
import { connect } from 'react-redux';

import * as AccessActionCreators from './redux/action-creators';
import { AccessState, AccessStateFilters } from './redux/store';

import { ClientWithEligibleUsers, ClientWithStats } from '../models';
import { CardPanel } from '../shared-components/card-panel/card-panel';
import { PanelSectionToolbar, PanelSectionToolbarButtons } from '../shared-components/card-panel/panel-sections';
import { Card } from '../shared-components/card/card';
import { CardSectionMain, CardSectionStats, CardText } from '../shared-components/card/card-sections';
import { CardStat } from '../shared-components/card/card-stat';
import { Filter } from '../shared-components/filter';
import { NavBar } from '../shared-components/navbar';
import { clientEntities } from './redux/selectors';

type ClientEntity = ((ClientWithEligibleUsers | ClientWithStats) & { indent: 1 | 2 }) | 'divider';
interface ClientAdminProps {
  clients: ClientEntity[];
  filters: AccessStateFilters;
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
        {this.renderClientPanel()}
      </>
    );
  }

  private renderClientPanel() {
    const { clients, filters } = this.props;
    return (
      <CardPanel
        entities={clients}
        renderEntity={(entity, key) => {
          if (entity === 'divider') {
            return <div className="hr" key={key} />;
          }
          return (
            <Card
              key={key}
              selected={false}
              disabled={false}
              onSelect={null}
              indentation={entity.indent}
            >
              <CardSectionMain>
                <CardText text={entity.name} subtext={entity.code} />
                <CardSectionStats>
                  <CardStat
                    name={'Eligible users'}
                    value={entity.userCount}
                    icon={'user'}
                  />
                  <CardStat
                    name={'Content items'}
                    value={entity.contentItemCount}
                    icon={'reports'}
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

function mapStateToProps(state: AccessState): ClientAdminProps {
  const { data, filters } = state;

  return {
    clients: clientEntities(state),
    filters,
  };
}

export const ConnectedClientAdmin = connect(
  mapStateToProps,
  AccessActionCreators,
)(ClientAdmin);
