import '../../../images/user.svg';

import * as React from 'react';
import * as Modal from 'react-modal';
import { connect } from 'react-redux';

import { isPublicationActive, ReductionStatus } from '../../view-models/content-publishing';
import {
  Client, ReductionFieldset, RootContentItem, RootContentItemWithStatus, SelectionGroup,
  SelectionGroupWithStatus,
} from '../models';
import { ActionIcon } from '../shared-components/action-icon';
import { CardPanel } from '../shared-components/card-panel/card-panel';
import {
  PanelSectionToolbar, PanelSectionToolbarButtons,
} from '../shared-components/card-panel/panel-sections';
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
  activeReductionFieldsets, activeSelectedClient, activeSelectedGroup, activeSelectedItem,
  allGroupsCollapsed, allGroupsExpanded, clientEntities, groupEntities, itemCardAttributes,
  itemEntities, modifiedReductionValues, pendingMaster, pendingReductionValues,
  selectedGroupWithStatus, selectedItem, selectionsFormModified,
} from './redux/selectors';
import { ContentAccessAdminState } from './redux/store';
import { SelectionsPanel } from './selections-panel';

interface ClientEntity extends Client {
  reports: number;
  eligibleUsers: number;
}
interface RootContentItemEntity extends RootContentItemWithStatus {
  selectionGroups: number;
  assignedUsers: number;
}
interface SelectionGroupEntity extends SelectionGroupWithStatus {
  assignedUsers: number;
}

interface ContentAccessAdminProps {
  clients: ClientEntity[];
  items: RootContentItemEntity[];
  groups: SelectionGroupEntity[];
  reductionFieldsets: ReductionFieldset[];
  clientPanel: {
    selectedCard: Guid;
    filterText: string;
  };
  itemPanel: {
    cards: {
      [id: string]: CardAttributes;
    };
    selectedCard: Guid;
    filterText: string;
  };
  groupPanel: {
    cards: {
      [id: string]: CardAttributes;
    };
    allExpanded: boolean;
    allCollapsed: boolean;
    selectedCard: Guid;
    filterText: string;
    isModalOpen: boolean;
  };
  selectionsPanel: {
    filterText: string;
  };
  selectedItem: RootContentItem;
  selectedGroup: SelectionGroupWithStatus;
  activeSelectedClient: Client;
  activeSelectedItem: RootContentItem;
  activeSelectedGroup: SelectionGroup;
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
  setClientFilterText: (sValue: string) => void;
  setItemFilterText: (sValue: string) => void;
  setGroupFilterText: (sValue: string) => void;
  setValueFilterText: (sValue: string) => void;
  setMasterSelected: (bValue: boolean) => actions.ActionWithBoolean;
  setValueSelected: (id: Guid, bValue: boolean) => actions.ActionWithId & actions.ActionWithBoolean;
  openAddGroupModal: () => void;
  closeAddGroupModal: () => void;
  setValueAddGroupModal: (sValue: string) => void;
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
    const { clients, clientPanel, selectClientCard, setClientFilterText } = this.props;
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
                  name={'Reports'}
                  value={entity.reports}
                  icon={'reports'}
                />
                <CardStat
                  name={'Eligible users'}
                  value={entity.eligibleUsers}
                  icon={'user'}
                />
              </CardSectionStats>
            </CardSectionMain>
          </Card>
        )}
      >
        <h3 className="admin-panel-header">Clients</h3>
        <PanelSectionToolbar>
          <Filter
            placeholderText={'Filter clients...'}
            setFilterText={setClientFilterText}
            filterText={clientPanel.filterText}
          />
          <PanelSectionToolbarButtons>
            <div id="icons" />
          </PanelSectionToolbarButtons>
        </PanelSectionToolbar>
      </CardPanel>
    );
  }

  private renderItemPanel() {
    const { items, activeSelectedClient: activeClient, itemPanel, selectItemCard, setItemFilterText } = this.props;
    return activeClient && (
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
                  name={'Selection groups'}
                  value={entity.selectionGroups}
                  icon={'group'}
                />
                <CardStat
                  name={'Assigned users'}
                  value={entity.assignedUsers}
                  icon={'user'}
                />
              </CardSectionStats>
            </CardSectionMain>
          </Card>
        )}
      >
        <h3 className="admin-panel-header">Content Items</h3>
        <PanelSectionToolbar>
          <Filter
            placeholderText={'Filter content items...'}
            setFilterText={setItemFilterText}
            filterText={itemPanel.filterText}
          />
          <PanelSectionToolbarButtons>
            <div id="icons" />
          </PanelSectionToolbarButtons>
        </PanelSectionToolbar>
      </CardPanel>
    );
  }

  private renderGroupPanel() {
    const {
      groups,
      activeSelectedClient: activeClient,
      activeSelectedItem: activeItem,
      groupPanel,
      selectGroupCard,
      selectedItem: item,
      setGroupCardExpanded,
      setGroupFilterText,
      openAddGroupModal,
      closeAddGroupModal,
      setValueAddGroupModal,
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
        />
      );
    const collapseAllIcon = groupPanel.allCollapsed
      ? null
      : (
        <ActionIcon
          label="Collapse all"
          icon="collapse-cards"
          action={collapseAllGroups}
        />
      );

    return activeClient && activeItem && (
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
                    value={entity.assignedUsers}
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
        <PanelSectionToolbar>
          <Filter
            placeholderText={'Filter selection groups...'}
            setFilterText={setGroupFilterText}
            filterText={groupPanel.filterText}
          />
          <PanelSectionToolbarButtons>
            {expandAllIcon}
            {collapseAllIcon}
          </PanelSectionToolbarButtons>
        </PanelSectionToolbar>
        <div className="card-container" onClick={openAddGroupModal}>
          <div className="card-body-container card-100 action-card">
            <h2 className="card-body-primary-text">
              <ActionIcon icon={'add'} />
              <span>ADD SELECTION GROUP</span>
            </h2>
          </div>
        </div>
        <Modal
          isOpen={groupPanel.isModalOpen}
          onRequestClose={closeAddGroupModal}
          ariaHideApp={false}
          className="modal"
          overlayClassName="modal-overlay"
        >
          <h3 className="title blue">Add Selection Group</h3>
          <span className="modal-text">Selection group name:</span>
          <form
            onSubmit={(event) => {
              event.nativeEvent.preventDefault();
              alert('You created a selection group!');
              closeAddGroupModal();
            }}
          >
            <input
              type="text"
              placeholder="Selection group name"
              onChange={(event) => setValueAddGroupModal(event.target.value)}
            />
            <div className="button-container">
              <button className="link-button" type="button" onClick={closeAddGroupModal}>
                Cancel
              </button>
              <button className="blue-button" type="submit">
                Add
              </button>
            </div>
          </form>
        </Modal>
      </CardPanel>
    );
  }

  private renderSelectionsPanel() {
    const {
      selectedItem: item,
      selectedGroup: group,
      reductionFieldsets,
      activeSelectedClient: activeClient,
      activeSelectedItem: activeItem,
      activeSelectedGroup: activeGroup,
      selectionsPanel,
      setValueSelected,
      setValueFilterText,
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
    return activeClient && activeItem && activeGroup && (
      <SelectionsPanel
        isSuspended={group.isSuspended}
        doesReduce={item.doesReduce}
        isModified={formModified}
        isMaster={selectedMaster}
        onIsMasterChange={setMasterSelected}
        title={group.name}
        subtitle={item.name}
        status={group.status.taskStatus || ReductionStatus.Unspecified}
        fieldsets={fieldsets}
      >
        <PanelSectionToolbar>
          <Filter
            placeholderText={'Filter fields and values...'}
            setFilterText={setValueFilterText}
            filterText={selectionsPanel.filterText}
          />
        </PanelSectionToolbar>
      </SelectionsPanel>
    );
  }
}

function mapStateToProps(state: ContentAccessAdminState): ContentAccessAdminProps {
  const { clientPanel, itemPanel, groupPanel, selectionsPanel } = state;
  return {
    clients: clientEntities(state),
    items: itemEntities(state),
    groups: groupEntities(state),
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
    selectionsPanel: {
      filterText: selectionsPanel.filterText,
    },
    selectedItem: selectedItem(state),
    selectedGroup: selectedGroupWithStatus(state),
    activeSelectedClient: activeSelectedClient(state),
    activeSelectedItem: activeSelectedItem(state),
    activeSelectedGroup: activeSelectedGroup(state),
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
