import '../../../images/user.svg';

import * as React from 'react';
import * as Modal from 'react-modal';
import { connect } from 'react-redux';
import ReduxToastr from 'react-redux-toastr';
import Select from 'react-select';

import { isPublicationActive, ReductionStatus } from '../../view-models/content-publishing';
import {
    Client, ClientWithEligibleUsers, ReductionFieldset, RootContentItem,
    RootContentItemWithPublication, SelectionGroup, SelectionGroupWithStatus, User,
} from '../models';
import { ActionIcon } from '../shared-components/action-icon';
import { ButtonSpinner } from '../shared-components/button-spinner';
import { CardPanel } from '../shared-components/card-panel/card-panel';
import {
    CardPanelSectionAction, CardPanelSectionContent,
} from '../shared-components/card-panel/card-panel-sections';
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
    addableUsers, allGroupsCollapsed, allGroupsExpanded, clientEntities, groupEntities,
    groupToDelete, itemEntities, modifiedReductionValues, pendingMaster, pendingReductionValues,
    selectedGroupWithStatus, selectedItem, selectionsFormModified,
} from './redux/selectors';
import {
    AccessState, AccessStateCardAttributes, AccessStateFilters, AccessStateModals,
    AccessStatePending, AccessStateSelected,
} from './redux/store';
import { SelectionsPanel } from './selections-panel';

interface ClientEntity extends ClientWithEligibleUsers {
  indent: 1 | 2;
}
interface RootContentItemEntity extends RootContentItemWithPublication {
  contentTypeName: string;
}
interface SelectionGroupEntity extends SelectionGroupWithStatus {
  assignedUsers: User[];
  userQuery: string;
  editing: boolean;
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
  addableUsers: User[];

  allGroupsExpanded: boolean;
  allGroupsCollapsed: boolean;
  groupToDelete: SelectionGroup;
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
  setPendingNewGroupName: (name: string) => void;
  openDeleteGroupModal: (id: Guid) => void;
  closeDeleteGroupModal: () => void;
  openInvalidateModal: () => void;
  closeInvalidateModal: () => void;

  setGroupEditingOn: (id: Guid) => void;
  setGroupEditingOff: (id: Guid) => void;
  setPendingGroupName: (me: string) => void;
  setPendingGroupUserQuery: (query: string) => void;
  setPendingGroupUserAssigned: (id: Guid) => void;
  setPendingGroupUserRemoved: (id: Guid) => void;

  scheduleStatusRefresh: (delay: number) => void;
  scheduleSessionCheck: (delay: number) => void;

  fetchClients: () => void;
  fetchItems: (id: Guid) => void;
  fetchGroups: (id: Guid) => void;
  fetchSelections: (id: Guid) => void;
  fetchStatusRefresh: (clientId: Guid, itemId: Guid) => void;
  fetchSessionCheck: () => void;

  createGroup: (id: Guid, name: string) => void;
  updateGroup: (id: Guid, name: string, users: Guid[]) => void;
  deleteGroup: (id: Guid) => void;
  suspendGroup: (id: Guid, isSuspended: boolean) => void;
  updateSelections: (id: Guid, isMaster: boolean, selections: Guid[]) => void;
  cancelReduction: (id: Guid) => void;
}

class ContentAccessAdmin extends React.Component<ContentAccessAdminProps & ContentAccessAdminActions> {
  private readonly currentView: string = document
    .getElementsByTagName('body')[0].getAttribute('data-nav-location');

  public componentDidMount() {
    this.props.fetchClients();
    this.props.scheduleStatusRefresh(0);
    this.props.scheduleSessionCheck(0);
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
        {this.renderGroupPanel()}
        {this.renderSelectionsPanel()}
      </>
    );
  }

  private renderClientPanel() {
    const { clients, selected, filters, pending } = this.props;
    return (
      <CardPanel
        entities={clients}
        loading={pending.data.clients}
        renderEntity={(entity, key) => entity
          ? (
            <Card
              key={key}
              selected={selected.client === entity.id}
              onSelect={() => {
                if (selected.client !== entity.id) {
                  this.props.fetchItems(entity.id);
                }
                this.props.selectClient(entity.id);
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
          )
          : (
            <div className="hr" key={key} />
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
    const { activeSelectedClient: activeClient, items, selected, filters, pending } = this.props;
    return activeClient && (
      <CardPanel
        entities={items}
        loading={pending.data.items}
        renderEntity={(entity, key) => (
          <Card
            key={key}
            disabled={isPublicationActive(entity.status && entity.status.requestStatus)}
            selected={selected.item === entity.id}
            onSelect={() => {
              if (selected.item !== entity.id) {
                this.props.fetchGroups(entity.id);
              }
              this.props.selectItem(entity.id);
            }}
            suspended={entity.isSuspended}
            status={entity.status}
          >
            <CardSectionMain>
              <CardText text={entity.name} subtext={entity.contentTypeName} />
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
      pending,
      selected,
      filters,
      modals,
      cardAttributes,
      addableUsers: users,
      allGroupsExpanded: allExpanded,
      allGroupsCollapsed: allCollapsed,
      groupToDelete: groupDelete,
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
        loading={pending.data.groups}
        renderEntity={(entity, key) => {
          const card = cardAttributes.group.get(entity.id);
          const cardButtons = entity.editing
            ? (
              <>
                <CardButton
                  color={'green'}
                  tooltip={'Save changes'}
                  onClick={() => this.props.updateGroup(entity.id, entity.name, entity.assignedUsers.map((u) => u.id))}
                  icon={'checkmark'}
                />
                <CardButton
                  color={'red'}
                  tooltip={'Cancel'}
                  onClick={() => this.props.setGroupEditingOff(entity.id)}
                  icon={'cancel'}
                />
              </>
            )
            : (
              <>
                <CardButton
                  color={'red'}
                  tooltip={'Delete selection group'}
                  onClick={() => this.props.openDeleteGroupModal(entity.id)}
                  icon={'delete'}
                />
                {pending.group.id === null
                  ? (
                    <CardButton
                      color={'blue'}
                      tooltip={'Edit selection group'}
                      onClick={() => this.props.setGroupEditingOn(entity.id)}
                      icon={'edit'}
                    />
                  )
                  : null
                }
              </>
            );
          const cardExpansion = entity.assignedUsers.length || entity.editing
            ? (
              <CardExpansion
                label={'Assigned Users'}
                expanded={card && card.expanded}
                setExpanded={(value) => value
                  ? this.props.setExpandedGroup(entity.id)
                  : this.props.setCollapsedGroup(entity.id)}
              >
                <ul className="detail-item-user-list">
                  {entity.assignedUsers.map((u) => (
                    <li key={u.id}>
                      <span className="detail-item-user">
                        <div style={{ marginRight: '0.5em' }}>
                        {
                          entity.editing
                            ? <CardButton
                              icon="remove-circle"
                              color="red"
                              onClick={() => this.props.setPendingGroupUserRemoved(u.id)}
                            />
                            : <svg
                              className="action-icon"
                              style={{
                                width: 'calc(1.8em + 4px)',
                                height: 'calc(1.8em + 4px)',
                              }}
                            >
                              <use xlinkHref={'#user'} />
                            </svg>
                        }
                        </div>
                        <div className="detail-item-user-name">
                          {u.firstName && u.lastName
                          ? (
                            <div style={{ fontSize: '1em', fontWeight: 'bold' }}>
                              {`${u.firstName} ${u.lastName}`}
                            </div>
                          )
                          : (
                            <div style={{ fontSize: '1em' }}>
                              (Unactivated)
                            </div>
                          )}
                          <div style={{ fontSize: '0.85em' }}>
                            {u.userName}
                          </div>
                        </div>
                      </span>
                    </li>
                  ))}
                  {
                    entity.editing
                      ? <li>
                        <span className="detail-item-user">
                          <div
                            onClick={(event) => event.stopPropagation()}
                            style={{ width: '100%' }}
                          >
                            <Select
                              className="react-select"
                              options={users.map((u) => ({
                                value: u.id,
                                firstLast: u.firstName && u.lastName && `${u.firstName} ${u.lastName}`,
                                username: u.userName,
                              }))}
                              formatOptionLabel={(data) => (
                                <>
                                  {data.firstLast
                                  ? (
                                    <div style={{ fontSize: '1em', fontWeight: 'bold' }}>
                                      {data.firstLast}
                                    </div>
                                  )
                                  : (
                                    <div style={{ fontSize: '1em' }}>
                                      (Unactivated)
                                    </div>
                                  )}
                                  <div style={{ fontSize: '0.85em' }}>
                                    {data.username}
                                  </div>
                                </>
                              )}
                              filterOption={({ data }, rawInput) => (
                                data.username.toLowerCase().match(rawInput.toLowerCase())
                                || (
                                  data.firstLast
                                  && data.firstLast.toLowerCase().match(rawInput.toLowerCase())
                                )
                              )}
                              onChange={(value, action) => {
                                if (action.action === 'select-option') {
                                  const singleValue = value as { value: string; };
                                  this.props.setPendingGroupUserAssigned(singleValue.value);
                                }
                              }}
                              onInputChange={(newValue) => this.props.setPendingGroupUserQuery(newValue)}
                              inputValue={entity.userQuery}
                              controlShouldRenderValue={false}
                              autoFocus={true}
                            />
                          </div>
                        </span>
                      </li>
                      : null
                  }
                </ul>
              </CardExpansion>
            )
            : null;
          return (
            <Card
              key={key}
              selected={selected.group === entity.id}
              onSelect={() => {
                if (selected.group !== entity.id) {
                  this.props.fetchSelections(entity.id);
                }
                this.props.selectGroup(entity.id);
              }}
              suspended={entity.isSuspended}
              inactive={entity.isInactive}
              status={entity.status}
            >
              <CardSectionMain>
                <CardText
                  text={entity.name}
                  textSuffix={entity.isSuspended ? '[Suspended]' : entity.isInactive ? '[Inactive]' : ''}
                  subtext={item.name}
                  editing={entity.editing}
                  setText={(text) => this.props.setPendingGroupName(text)}
                />
                <CardSectionStats>
                  <CardStat
                    name={'Assigned users'}
                    value={entity.assignedUsers.length}
                    icon={'user'}
                  />
                </CardSectionStats>
                <CardSectionButtons>
                  {cardButtons}
                </CardSectionButtons>
              </CardSectionMain>
              {cardExpansion}
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
        <CardPanelSectionAction>
          <div className="card-container action-card-container" onClick={this.props.openAddGroupModal}>
            <div className="admin-panel-content">
              <div className="card-body-container card-100 action-card">
                <h2 className="card-body-primary-text">
                  <svg className="action-card-icon">
                    <use href="#add" />
                  </svg>
                  <span>NEW GROUP</span>
                </h2>
              </div>
            </div>
          </div>
        </CardPanelSectionAction>
        <Modal
          isOpen={modals.addGroup.isOpen}
          onRequestClose={this.props.closeAddGroupModal}
          ariaHideApp={false}
          className="modal"
          overlayClassName="modal-overlay"
          closeTimeoutMS={100}
        >
          <h3 className="title blue">Add Selection Group</h3>
          <span className="modal-text">Selection group name:</span>
          <form
            onSubmit={(event) => {
              event.nativeEvent.preventDefault();
              if (!this.props.pending.data.createGroup) {
                this.props.createGroup(this.props.selectedItem.id, this.props.pending.newGroupName);
              }
            }}
          >
            <input
              type="text"
              placeholder="Selection group name"
              onChange={(event) => this.props.setPendingNewGroupName(event.target.value)}
              autoFocus={true}
            />
            <div className="button-container">
              <button className="link-button" type="button" onClick={this.props.closeAddGroupModal}>
                Cancel
              </button>
              <button className="blue-button" type="submit">
                Add
                {this.props.pending.data.createGroup
                  ? <ButtonSpinner />
                  : null
                }
              </button>
            </div>
          </form>
        </Modal>
        <Modal
          isOpen={modals.deleteGroup.isOpen}
          onRequestClose={this.props.closeDeleteGroupModal}
          ariaHideApp={false}
          className="modal"
          overlayClassName="modal-overlay"
          closeTimeoutMS={100}
        >
          <h3 className="title red">Delete Selection Group</h3>
          <span className="modal-text">Delete <strong>{groupDelete ? groupDelete.name : ''}</strong>?</span>
          <div className="button-container">
            <button className="link-button" type="button" onClick={this.props.closeDeleteGroupModal}>
              Cancel
            </button>
            <button
              className="red-button"
              onClick={() => {
                if (!this.props.pending.data.deleteGroup) {
                  this.props.deleteGroup(this.props.groupToDelete.id);
                }
              }}
            >
              Delete
              {this.props.pending.data.deleteGroup
                ? <ButtonSpinner />
                : null
              }
            </button>
          </div>
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
      pending,
      modals,
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
        onIsSuspendedChange={(value) => this.props.suspendGroup(group.id, value)}
        doesReduce={item.doesReduce}
        isModified={formModified}
        isMaster={selectedMaster}
        onIsMasterChange={this.props.setPendingIsMaster}
        title={group.name}
        subtitle={item.name}
        status={group.status.taskStatus || ReductionStatus.Unspecified}
        onBeginReduction={() => selectedValues.length || selectedMaster
          ? this.props.updateSelections(group.id, selectedMaster, selectedValues)
          : this.props.openInvalidateModal()
        }
        onCancelReduction={() => this.props.cancelReduction(group.id)}
        loading={pending.data.selections}
        submitting={pending.data.updateSelections || pending.data.cancelReduction}
        fieldsets={fieldsets}
      >
        <PanelSectionToolbar>
          <Filter
            placeholderText={'Filter fields and values...'}
            setFilterText={this.props.setFilterTextSelections}
            filterText={filters.selections.text}
          />
        </PanelSectionToolbar>
        <Modal
          isOpen={modals.invalidate.isOpen}
          onRequestClose={this.props.closeInvalidateModal}
          ariaHideApp={false}
          className="modal"
          overlayClassName="modal-overlay"
          closeTimeoutMS={100}
        >
          <h3 className="title orange">Warning</h3>
          <span className="modal-text">
            You have not selected any values for this selection group.
            Submitting no selections will cause the group to become inactive
            and all of its users will be unable to view its content until values are selected.
          </span>
          <div className="button-container">
            <button className="link-button" type="button" onClick={this.props.closeInvalidateModal}>
              Cancel
            </button>
            <button
              className="orange-button"
              onClick={() => {
                if (!this.props.pending.data.updateSelections) {
                  this.props.updateSelections(group.id, selectedMaster, selectedValues);
                }
              }}
            >
              Proceed
              {this.props.pending.data.updateSelections
                ? <ButtonSpinner />
                : null
              }
            </button>
          </div>
        </Modal>
      </SelectionsPanel>
    );
  }
}

function mapStateToProps(state: AccessState): ContentAccessAdminProps {
  const { data, selected, cardAttributes, pending, filters, modals } = state;
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
    addableUsers: addableUsers(state),
    allGroupsExpanded: allGroupsExpanded(state),
    allGroupsCollapsed: allGroupsCollapsed(state),
    groupToDelete: groupToDelete(state),
  };
}

export const ConnectedContentAccessAdmin = connect(
  mapStateToProps,
  actions,
)(ContentAccessAdmin);
