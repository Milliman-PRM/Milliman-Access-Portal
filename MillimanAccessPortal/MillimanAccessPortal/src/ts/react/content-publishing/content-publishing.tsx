import '../../../images/icons/add.svg';
import '../../../images/icons/user.svg';

import * as React from 'react';
import { connect } from 'react-redux';
import ReduxToastr from 'react-redux-toastr';

import { isPublicationActive, PublicationStatus } from '../../view-models/content-publishing';
import {
    Client, ClientWithStats, RootContentItem, RootContentItemWithPublication,
} from '../models';
import { ActionIcon } from '../shared-components/action-icon';
import { CardPanel } from '../shared-components/card-panel/card-panel';
import {
    PanelSectionToolbar, PanelSectionToolbarButtons,
} from '../shared-components/card-panel/panel-sections';
import { Card } from '../shared-components/card/card';
import CardButton from '../shared-components/card/card-button';
import {
   CardSectionButtons, CardSectionMain, CardSectionStats, CardText,
} from '../shared-components/card/card-sections';
import { CardStat } from '../shared-components/card/card-stat';
import { Filter } from '../shared-components/filter';
import { NavBar } from '../shared-components/navbar';
import * as PublishingActionCreators from './redux/action-creators';
import {
    activeSelectedClient, activeSelectedItem, clientEntities, itemEntities, selectedItem,
} from './redux/selectors';
import {
    PublishingState, PublishingStateCardAttributes, PublishingStateFilters,
    PublishingStatePending, PublishingStateSelected,
} from './redux/store';

type ClientEntity = (ClientWithStats & { indent: 1 | 2 }) | 'divider';
interface RootContentItemEntity extends RootContentItemWithPublication {
  contentTypeName: string;
}

interface ContentPublishingProps {
  clients: ClientEntity[];
  items: RootContentItemEntity[];
  selected: PublishingStateSelected;
  cardAttributes: PublishingStateCardAttributes;
  pending: PublishingStatePending;
  filters: PublishingStateFilters;

  selectedItem: RootContentItem;
  activeSelectedClient: Client;
  activeSelectedItem: RootContentItem;
}

class ContentPublishing extends React.Component<ContentPublishingProps & typeof PublishingActionCreators> {
  private readonly currentView: string = document
    .getElementsByTagName('body')[0].getAttribute('data-nav-location');

  public componentDidMount() {
    this.props.fetchGlobalData({});
    this.props.fetchClients({});
    this.props.scheduleStatusRefresh({ delay: 0 });
    this.props.scheduleSessionCheck({ delay: 0 });
    // setUnloadAlert(() => this.props.pending.item);
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
        {this.renderItemPanel()}
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
                if (selected.client !== entity.id) {
                  this.props.fetchItems({ clientId: entity.id });
                }
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

  private renderItemPanel() {
    const { activeSelectedClient: activeClient, items, selected, filters, pending } = this.props;
    const createNewContentItemIcon = (
      <ActionIcon
        label="New Content Item"
        icon="add"
        action={() => { alert('Create New Content Item'); }}
      />
    );
    return activeClient && (
      <CardPanel
        entities={items}
        loading={pending.data.items}
        renderEntity={(entity, key) => {
          const cardButtons = entity.status.requestStatus === PublicationStatus.Processed ?
            (
              <>
                <CardButton
                  color={'green'}
                  tooltip={'Approve'}
                  onClick={() => alert('Go Live Preview')}
                  icon={'checkmark'}
                />
              </>
            ) : entity.status.requestStatus === PublicationStatus.Queued
              || entity.status.requestStatus === PublicationStatus.Validating ? (
                <>
                  <CardButton
                    color={'red'}
                    tooltip={'Cancel'}
                    onClick={() => alert('cancel')}
                    icon={'cancel'}
                  />
                </>
              ) : (
                <>
                  <CardButton
                    color={'red'}
                    tooltip={'Delete'}
                    onClick={() => alert('delete')}
                    icon={'delete'}
                  />
                  <CardButton
                    color={'green'}
                    tooltip={'Edit'}
                    onClick={() => alert('upload')}
                    icon={'edit'}
                  />
                </>
              );
          return (
            <Card
              key={key}
              selected={selected.item === entity.id}
              onSelect={() => {
                this.props.selectItem({ id: entity.id });
              }}
              suspended={entity.isSuspended}
              status={entity.status}
            >
              <CardSectionMain>
                <CardText
                  text={entity.name}
                  textSuffix={entity.isSuspended ? '[Suspended]' : ''}
                  subtext={entity.contentTypeName}
                />
                <CardSectionStats>
                  <CardStat
                    name={'Selection groups'}
                    value={entity.selectionGroupCount}
                    icon={'group'}
                  />
                  <CardStat
                    name={'Assigned users'}
                    value={entity.assignedUserCount}
                    icon={'user'}
                  />
                </CardSectionStats>
                <CardSectionButtons>
                  {cardButtons}
                </CardSectionButtons>
              </CardSectionMain>
            </Card>
            );
        }}
        renderNewEntityButton={() => (
          <div className="card-container action-card-container" onClick={() => alert('Content Item Created')}>
            <div className="admin-panel-content">
              <div className="card-body-container card-100 action-card">
                <h2 className="card-body-primary-text">
                  <svg className="action-card-icon">
                    <use href="#add" />
                  </svg>
                  <span>CREATE CONTENT ITEM</span>
                </h2>
              </div>
            </div>
          </div>
        )}
      >
        <h3 className="admin-panel-header">Content Items</h3>
        <PanelSectionToolbar>
          <Filter
            placeholderText={'Filter content items...'}
            setFilterText={(text) => this.props.setFilterTextItem({ text })}
            filterText={filters.item.text}
          />
          <PanelSectionToolbarButtons>
            {createNewContentItemIcon}
          </PanelSectionToolbarButtons>
        </PanelSectionToolbar>
      </CardPanel>
    );
  }
}

function mapStateToProps(state: PublishingState): ContentPublishingProps {
  const { data, selected, cardAttributes, pending, filters } = state;
  return {
    clients: clientEntities(state),
    items: itemEntities(state),
    selected,
    cardAttributes,
    pending,
    filters,
    selectedItem: selectedItem(state),
    activeSelectedClient: activeSelectedClient(state),
    activeSelectedItem: activeSelectedItem(state),
  };
}

export const ConnectedContentPublishing = connect(
  mapStateToProps,
  PublishingActionCreators,
)(ContentPublishing);
