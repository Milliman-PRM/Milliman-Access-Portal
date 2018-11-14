import '../../../images/user.svg';

import * as React from 'react';
import { connect } from 'react-redux';

import { isPublicationActive, ReductionStatus } from '../../view-models/content-publishing';
import {
  Client, ReductionFieldset, RootContentItem, RootContentItemWithStatus, SelectionGroupWithStatus,
} from '../models';
import { ActionIcon } from '../shared-components/action-icon';
import { CardPanel } from '../shared-components/card-panel/card-panel';
import {
  CardPanelSectionToolbar, CardPanelSectionToolbarButtons,
} from '../shared-components/card-panel/card-panel-sections';
import { Card, CardAttributes } from '../shared-components/card/card';
import CardButton from '../shared-components/card/card-button';
import { CardExpansion } from '../shared-components/card/card-expansion';
import {
  CardSectionButtons, CardSectionMain, CardSectionStats, CardText,
} from '../shared-components/card/card-sections';
import { CardStat } from '../shared-components/card/card-stat';
import { Filter } from '../shared-components/filter';
import { Guid } from '../shared-components/interfaces';
import { NavBar } from '../shared-components/navbar';
import * as actions from './redux/actions';
import {
  activeGroupsWithStatus, activeItemsWithStatus, activeReductionFieldsets, allGroupsCollapsed,
  allGroupsExpanded, itemCardAttributes, modifiedReductionValues, pendingMaster,
  pendingReductionValues, selectedGroupWithStatus, selectedItem, selectionsFormModified,
} from './redux/selectors';
import { ContentAccessAdminState } from './redux/store';
import { SelectionsPanel } from './selections-panel';

interface ContentAccessAdminProps {
  clients: Client[];
  items: RootContentItemWithStatus[];
  groups: SelectionGroupWithStatus[];
  reductionFieldsets: ReductionFieldset[];
  clientPanel: {
    selectedCard: Guid;
  };
  itemPanel: {
    cards: {
      [id: string]: CardAttributes;
    };
    selectedCard: Guid;
  };
  groupPanel: {
    cards: {
      [id: string]: CardAttributes;
    };
    allExpanded: boolean;
    allCollapsed: boolean;
    selectedCard: Guid;
  };
  selectedItem: RootContentItem;
  selectedGroup: SelectionGroupWithStatus;
  selectedValues: Guid[];
  modifiedValues: Guid[];
  selectedMaster: boolean;
  formModified: boolean;
}
interface ContentAccessAdminActions {
  nop: () => void;
  selectClientCard: (id: Guid) => actions.ActionWithId;
  selectItemCard: (id: Guid) => actions.ActionWithId;
  selectGroupCard: (id: Guid) => actions.ActionWithId;
  setGroupCardExpanded: (id: Guid, bValue: boolean) => actions.ActionWithId & actions.ActionWithBoolean;
  expandAllGroups: () => void;
  collapseAllGroups: () => void;
  setMasterSelected: (bValue: boolean) => actions.ActionWithBoolean;
  setValueSelected: (id: Guid, bValue: boolean) => actions.ActionWithId & actions.ActionWithBoolean;
}

class ContentAccessAdmin extends React.Component<ContentAccessAdminProps & ContentAccessAdminActions> {
  private readonly currentView: string = document
    .getElementsByTagName('body')[0].getAttribute('data-nav-location');

  public componentDidMount() {
    this.props.nop();
  }

  public render() {
    return (
      <>
        <NavBar currentView={this.currentView} />
        {this.renderClientPanel()}
        {this.renderItemPanel()}
        {this.renderGroupPanel()}
        {this.renderSelectionsPanel()}
      </>
    );
  }

  private renderClientPanel() {
    const { clients, clientPanel, selectClientCard } = this.props;
    return (
      <CardPanel
        cards={{}}
        entities={clients}
        renderEntity={(entity, key) => (
          <Card
            key={key}
            selected={clientPanel.selectedCard === entity.id}
            onSelect={() => selectClientCard(entity.id)}
          >
            <CardSectionMain>
              <CardText text={entity.name} subtext={entity.code} />
              <CardSectionStats>
                <CardStat
                  name={'Eligible users'}
                  value={0}
                  icon={'user'}
                />
                <CardStat
                  name={'Reports'}
                  value={0}
                  icon={'reports'}
                />
              </CardSectionStats>
            </CardSectionMain>
          </Card>
        )}
      >
        <h3 className="admin-panel-header">Clients</h3>
        <CardPanelSectionToolbar>
          <Filter
            placeholderText={''}
            setFilterText={() => null}
            filterText={''}
          />
          <CardPanelSectionToolbarButtons>
            <div id="icons" />
          </CardPanelSectionToolbarButtons>
        </CardPanelSectionToolbar>
      </CardPanel>
    );
  }

  private renderItemPanel() {
    const { items, clientPanel, itemPanel, selectItemCard } = this.props;
    return clientPanel.selectedCard && (
      <CardPanel
        cards={itemPanel.cards}
        entities={items}
        renderEntity={(entity, key) => (
          <Card
            key={key}
            disabled={isPublicationActive(entity.status && entity.status.requestStatus)}
            selected={itemPanel.selectedCard === entity.id}
            onSelect={() => selectItemCard(entity.id)}
            status={entity.status}
          >
            <CardSectionMain>
              <CardText text={entity.name} subtext={'Content Type'} />
              <CardSectionStats>
                <CardStat
                  name={'Assigned users'}
                  value={0}
                  icon={'user'}
                />
                <CardStat
                  name={'Selection groups'}
                  value={0}
                  icon={'group'}
                />
              </CardSectionStats>
            </CardSectionMain>
          </Card>
        )}
      >
        <h3 className="admin-panel-header">Content Items</h3>
        <CardPanelSectionToolbar>
          <Filter
            placeholderText={''}
            setFilterText={() => null}
            filterText={''}
          />
          <CardPanelSectionToolbarButtons>
            <div id="icons" />
          </CardPanelSectionToolbarButtons>
        </CardPanelSectionToolbar>
      </CardPanel>
    );
  }

  private renderGroupPanel() {
    const {
      groups,
      itemPanel,
      groupPanel,
      selectGroupCard,
      selectedItem: item,
      setGroupCardExpanded,
      expandAllGroups,
      collapseAllGroups,
    } = this.props;

    const expandAllIcon = groupPanel.allExpanded
      ? null
      : (
        <ActionIcon
          label="Expand all"
          icon="expand-cards"
          action={expandAllGroups}
          inline={true}
        />
      );
    const collapseAllIcon = groupPanel.allCollapsed
      ? null
      : (
        <ActionIcon
          label="Collapse all"
          icon="collapse-cards"
          action={collapseAllGroups}
          inline={true}
        />
      );

    return itemPanel.selectedCard && (
      <CardPanel
        cards={groupPanel.cards}
        entities={groups}
        renderEntity={(entity, key) => {
          const card = groupPanel.cards[entity.id] || {};
          return (
            <Card
              key={key}
              selected={groupPanel.selectedCard === entity.id}
              onSelect={() => selectGroupCard(entity.id)}
              status={entity.status}
            >
              <CardSectionMain>
                <CardText text={entity.name} subtext={item.name} />
                <CardSectionStats>
                  <CardStat
                    name={'Assigned users'}
                    value={0}
                    icon={'user'}
                  />
                </CardSectionStats>
                <CardSectionButtons>
                  <CardButton
                    color={'red'}
                    tooltip={'Delete selection group'}
                    onClick={() => alert('You clicked delete.')}
                    icon={'delete'}
                  />
                  <CardButton
                    color={'blue'}
                    tooltip={'Edit selection group'}
                    onClick={() => alert('You clicked edit.')}
                    icon={'edit'}
                  />
                </CardSectionButtons>
              </CardSectionMain>
              <CardExpansion
                label={'Members'}
                expanded={card.expanded}
                setExpanded={(value) => setGroupCardExpanded(entity.id, value)}
              >
                <ul>
                {[{}].map((o: any, i) => (
                  <li key={i}>
                    <span className="detail-item-user">
                      <div className="detail-item-user-icon">
                        <svg className="card-user-icon">
                          <use xlinkHref={'user'} />
                        </svg>
                      </div>
                      <div className="detail-item-user-name">
                        <h4 className="first-last">{o.primaryText}</h4>
                        <span className="user-name">{o.secondaryText}</span>
                      </div>
                    </span>
                  </li>
                ))}
                </ul>
              </CardExpansion>
            </Card>
          );
        }}
      >
        <h3 className="admin-panel-header">Selection Groups</h3>
        <CardPanelSectionToolbar>
          <Filter
            placeholderText={''}
            setFilterText={() => null}
            filterText={''}
          />
          <CardPanelSectionToolbarButtons>
            {expandAllIcon}
            {collapseAllIcon}
          </CardPanelSectionToolbarButtons>
        </CardPanelSectionToolbar>
      </CardPanel>
    );
  }

  private renderSelectionsPanel() {
    const {
      selectedItem: item,
      selectedGroup: group,
      reductionFieldsets,
      groupPanel,
      setValueSelected,
      selectedValues,
      modifiedValues,
      selectedMaster,
      setMasterSelected,
      formModified,
    } = this.props;
    const fieldsets = reductionFieldsets.map((s) => ({
      name: s.field.displayName,
      fields: s.values.map((v) => ({
        name: v.value,
        selected: selectedValues.indexOf(v.id) !== -1,
        modified: modifiedValues.indexOf(v.id) !== -1,
        onChange: (selected: boolean) => setValueSelected(v.id, selected),
      })),
    }));
    return groupPanel.selectedCard && (
      <SelectionsPanel
        isSuspended={group.isSuspended}
        doesReduce={item.doesReduce}
        isModified={formModified}
        isMaster={selectedMaster}
        onIsMasterChange={setMasterSelected}
        title={group.name}
        subtitle={item.name}
        status={group.status ? group.status.taskStatus : ReductionStatus.Unspecified}
        fieldsets={fieldsets}
      />
    );
  }
}

function mapStateToProps(state: ContentAccessAdminState): ContentAccessAdminProps {
  const { clientPanel, itemPanel, groupPanel } = state;
  const { clients } = state.data;
  return {
    clients,
    items: activeItemsWithStatus(state),
    groups: activeGroupsWithStatus(state),
    reductionFieldsets: activeReductionFieldsets(state),
    clientPanel,
    itemPanel: {
      ...itemPanel,
      cards: itemCardAttributes(state),
    },
    groupPanel: {
      ...groupPanel,
      allExpanded: allGroupsExpanded(state),
      allCollapsed: allGroupsCollapsed(state),
    },
    selectedItem: selectedItem(state),
    selectedGroup: selectedGroupWithStatus(state),
    selectedValues: pendingReductionValues(state)
      ? pendingReductionValues(state).map((v) => v.id)
      : [],
    modifiedValues: modifiedReductionValues(state)
      ? modifiedReductionValues(state).map((v) => v.id)
      : [],
    selectedMaster: pendingMaster(state),
    formModified: selectionsFormModified(state),
  };
}

export const ConnectedContentAccessAdmin = connect(
  mapStateToProps,
  actions,
)(ContentAccessAdmin);
