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
import { Card } from '../shared-components/card/card';
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
  allGroupsCollapsed, allGroupsExpanded, clientEntities, groupEntities, itemEntities,
  modifiedReductionValues, pendingMaster, pendingReductionValues, selectedGroupWithStatus,
  selectedItem, selectionsFormModified,
} from './redux/selectors';
import {
  AccessState, AccessStateCardAttributes, AccessStateFilters, AccessStateModals, AccessStatePending,
  AccessStateSelected,
} from './redux/store';
import { SelectionsPanel } from './selections-panel';

interface ClientEntity extends Client {
  reports: number;
  eligibleUsers: number;
}
interface RootContentItemEntity extends RootContentItemWithStatus {
  selectionGroups: number;
  assignedUsers: number;
  contentTypeName: string;
}
interface SelectionGroupEntity extends SelectionGroupWithStatus {
  assignedUsers: number;
}

interface ContentAccessAdminProps {
  clients: ClientEntity[];
  items: RootContentItemEntity[];
  groups: SelectionGroupEntity[];
  reductionFieldsets: ReductionFieldset[];
  selected: AccessStateSelected;
  cardAttributes: AccessStateCardAttributes;
  pending: AccessStatePending;
  filters: AccessStateFilters;
  modals: AccessStateModals;

  selectedItem: RootContentItem;
  selectedGroup: SelectionGroupWithStatus;
  activeSelectedClient: Client;
  activeSelectedItem: RootContentItem;
  activeSelectedGroup: SelectionGroup;
  selectedValues: Guid[];
  modifiedValues: Guid[];
  selectedMaster: boolean;
  formModified: boolean;

  allGroupsExpanded: boolean;
  allGroupsCollapsed: boolean;
}
interface ContentAccessAdminActions {
  selectClient: (id: Guid) => void;
  selectItem: (id: Guid) => void;
  selectGroup: (id: Guid) => void;

  setExpandedGroup: (id: Guid) => void;
  setCollapsedGroup: (id: Guid) => void;
  setAllExpandedGroup: () => void;
  setAllCollapsedGroup: () => void;

  setFilterTextClient: (text: string) => void;
  setFilterTextItem: (text: string) => void;
  setFilterTextGroup: (text: string) => void;
  setFilterTextSelections: (text: string) => void;

  setPendingIsMaster: (isMaster: boolean) => void;
  setPendingSelectionOn: (id: Guid) => void;
  setPendingSelectionOff: (id: Guid) => void;

  openAddGroupModal: () => void;
  closeAddGroupModal: () => void;
  setPendingGroupName: (name: string) => void;
}

class ContentAccessAdmin extends React.Component<ContentAccessAdminProps & ContentAccessAdminActions> {
  private readonly currentView: string = document
    .getElementsByTagName('body')[0].getAttribute('data-nav-location');

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
    const { clients, selected, filters } = this.props;
    return (
      <CardPanel
        entities={clients}
        renderEntity={(entity, key) => (
          <Card
            key={key}
            selected={selected.client === entity.id}
            onSelect={() => this.props.selectClient(entity.id)}
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
            setFilterText={this.props.setFilterTextClient}
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
    const { activeSelectedClient: activeClient, items, selected, filters } = this.props;
    return activeClient && (
      <CardPanel
        entities={items}
        renderEntity={(entity, key) => (
          <Card
            key={key}
            disabled={isPublicationActive(entity.status && entity.status.requestStatus)}
            selected={selected.item === entity.id}
            onSelect={() => this.props.selectItem(entity.id)}
            suspended={entity.isSuspended}
            status={entity.status}
          >
            <CardSectionMain>
              <CardText text={entity.name} subtext={entity.contentTypeName} />
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
            setFilterText={this.props.setFilterTextItem}
            filterText={filters.item.text}
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
      selectedItem: item,
      selected,
      filters,
      modals,
      cardAttributes,
      allGroupsExpanded: allExpanded,
      allGroupsCollapsed: allCollapsed,
    } = this.props;

    const expandAllIcon = allExpanded
      ? null
      : (
        <ActionIcon
          label="Expand all"
          icon="expand-cards"
          action={this.props.setAllExpandedGroup}
        />
      );
    const collapseAllIcon = allCollapsed
      ? null
      : (
        <ActionIcon
          label="Collapse all"
          icon="collapse-cards"
          action={this.props.setAllCollapsedGroup}
        />
      );

    return activeClient && activeItem && (
      <CardPanel
        entities={groups}
        renderEntity={(entity, key) => {
          const card = cardAttributes.group.get(entity.id);
          return (
            <Card
              key={key}
              selected={selected.group === entity.id}
              onSelect={() => this.props.selectGroup(entity.id)}
              suspended={entity.isSuspended}
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
                expanded={card && card.expanded}
                setExpanded={(value) => value
                  ? this.props.setExpandedGroup(entity.id)
                  : this.props.setCollapsedGroup(entity.id)}
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
            setFilterText={this.props.setFilterTextGroup}
            filterText={filters.group.text}
          />
          <PanelSectionToolbarButtons>
            {expandAllIcon}
            {collapseAllIcon}
          </PanelSectionToolbarButtons>
        </PanelSectionToolbar>
        <div className="card-container" onClick={this.props.openAddGroupModal}>
          <div className="card-body-container card-100 action-card">
            <h2 className="card-body-primary-text">
              <ActionIcon icon={'add'} />
              <span>ADD SELECTION GROUP</span>
            </h2>
          </div>
        </div>
        <Modal
          isOpen={modals.addGroup.isOpen}
          onRequestClose={this.props.closeAddGroupModal}
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
              this.props.closeAddGroupModal();
            }}
          >
            <input
              type="text"
              placeholder="Selection group name"
              onChange={(event) => this.props.setPendingGroupName(event.target.value)}
            />
            <div className="button-container">
              <button className="link-button" type="button" onClick={this.props.closeAddGroupModal}>
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
      filters,
      selectedValues,
      modifiedValues,
      selectedMaster,
      formModified,
    } = this.props;
    const fieldsets = reductionFieldsets.map((s) => ({
      name: s.field.displayName,
      fields: s.values.map((v) => ({
        name: v.value,
        selected: selectedValues.indexOf(v.id) !== -1,
        modified: modifiedValues.indexOf(v.id) !== -1,
        onChange: (selected: boolean) => selected
          ? this.props.setPendingSelectionOn(v.id)
          : this.props.setPendingSelectionOff(v.id),
      })),
    }));
    return activeClient && activeItem && activeGroup && (
      <SelectionsPanel
        isSuspended={group.isSuspended}
        doesReduce={item.doesReduce}
        isModified={formModified}
        isMaster={selectedMaster}
        onIsMasterChange={this.props.setPendingIsMaster}
        title={group.name}
        subtitle={item.name}
        status={group.status.taskStatus || ReductionStatus.Unspecified}
        fieldsets={fieldsets}
      >
        <PanelSectionToolbar>
          <Filter
            placeholderText={'Filter fields and values...'}
            setFilterText={this.props.setFilterTextSelections}
            filterText={filters.selections.text}
          />
        </PanelSectionToolbar>
      </SelectionsPanel>
    );
  }
}

function mapStateToProps(state: AccessState): ContentAccessAdminProps {
  const { selected, cardAttributes, pending, filters, modals } = state;
  return {
    clients: clientEntities(state),
    items: itemEntities(state),
    groups: groupEntities(state),
    reductionFieldsets: activeReductionFieldsets(state),
    selected,
    cardAttributes,
    pending,
    filters,
    modals,
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
    allGroupsExpanded: allGroupsExpanded(state),
    allGroupsCollapsed: allGroupsCollapsed(state),
  };
}

export const ConnectedContentAccessAdmin = connect(
  mapStateToProps,
  actions,
)(ContentAccessAdmin);
