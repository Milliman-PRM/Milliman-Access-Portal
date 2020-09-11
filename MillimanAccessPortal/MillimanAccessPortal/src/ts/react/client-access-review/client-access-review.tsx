import * as React from 'react';
import * as Modal from 'react-modal';
import { connect } from 'react-redux';
import ReduxToastr from 'react-redux-toastr';

import { setUnloadAlert } from '../../unload-alerts';
import { Client, ClientWithReviewDate } from '../models';
import { CardPanel } from '../shared-components/card-panel/card-panel';
import {
    PanelSectionToolbar, PanelSectionToolbarButtons,
} from '../shared-components/card-panel/panel-sections';
import { Card } from '../shared-components/card/card';
import { CardSectionMain, CardText } from '../shared-components/card/card-sections';
import { Filter } from '../shared-components/filter';
import { NavBar } from '../shared-components/navbar';
import * as ClientAccessReviewActionCreators from './redux/action-creators';
import { activeSelectedClient, clientEntities } from './redux/selectors';
import {
    AccessReviewState, AccessReviewStateCardAttributes, AccessReviewStateFilters, AccessReviewStateModals,
    AccessReviewStatePending, AccessReviewStateSelected, ClientSummaryModel,
} from './redux/store';

type ClientEntity = (ClientWithReviewDate & { indent: 1 | 2 }) | 'divider';

interface ClientAccessReviewProps {
  clients: ClientEntity[];
  clientSummary: ClientSummaryModel;
  selected: AccessReviewStateSelected;
  cardAttributes: AccessReviewStateCardAttributes;
  pending: AccessReviewStatePending;
  filters: AccessReviewStateFilters;
  modals: AccessReviewStateModals;
  activeSelectedClient: Client;
}

class ClientAccessReview extends React.Component<ClientAccessReviewProps & typeof ClientAccessReviewActionCreators> {
  private readonly currentView: string = document
    .getElementsByTagName('body')[0].getAttribute('data-nav-location');

  public componentDidMount() {
    this.props.fetchGlobalData({});
    this.props.fetchClients({});
    this.props.scheduleSessionCheck({ delay: 0 });
    // TODO: Implement Unload Alert properly
    setUnloadAlert(() => false);
  }

  public render() {
    const { clientSummary, pending, selected } = this.props;
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
        {
          selected.client
          && !pending.data.clientSummary
          && clientSummary.clientName
          && this.renderClientSummaryPanel()
        }
      </>
    );
  }

  private renderClientPanel() {
    const { clients, selected, filters, pending, cardAttributes } = this.props;
    return (
      <CardPanel
        entities={clients}
        loading={pending.data.clients}
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
                this.props.selectClient({ id: entity.id });
                if (entity.id !== selected.client) {
                  this.props.fetchClientSummary({ clientId: entity.id });
                }
              }}
              indentation={entity.indent}
            >
              <CardSectionMain>
                <CardText text={entity.name} subtext={entity.code} />
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

  private renderClientSummaryPanel() {
    return (
      <div>Client Summary Panel</div>
    );
  }
}

function mapStateToProps(state: AccessReviewState): ClientAccessReviewProps {
  const { selected, cardAttributes, filters, modals, pending, data } = state;
  return {
    clients: clientEntities(state),
    clientSummary: data.selectedClientSummary,
    selected,
    cardAttributes,
    pending,
    filters,
    modals,
    activeSelectedClient: activeSelectedClient(state),
  };
}

export const ConnectedClientAccessReview = connect(
  mapStateToProps,
  ClientAccessReviewActionCreators,
)(ClientAccessReview);
