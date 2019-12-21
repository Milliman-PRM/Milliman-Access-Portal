import '../../../images/icons/add.svg';
import '../../../images/icons/user.svg';

import * as React from 'react';
import * as Modal from 'react-modal';
import { connect } from 'react-redux';
import ReduxToastr from 'react-redux-toastr';
import Select from 'react-select';

import { setUnloadAlert } from '../../unload-alerts';
import {
  isPublicationActive, isReductionActive, PublicationStatus, ReductionStatus,
} from '../../view-models/content-publishing';
import {
    Client, ClientWithEligibleUsers, ClientWithStats, ReductionFieldset, RootContentItem,
    RootContentItemWithPublication, SelectionGroup, SelectionGroupWithStatus, User,
} from '../models';
import { ActionIcon } from '../shared-components/action-icon';
import { ButtonSpinner } from '../shared-components/button-spinner';
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
import * as AccessActionCreators from './redux/action-creators';
import {
    activeReductionFieldsets, activeSelectedClient, activeSelectedGroup, activeSelectedItem,
    addableUsers, allGroupsCollapsed, allGroupsExpanded, allValuesDeselected, allValuesSelected,
    clientEntities, groupEntities, groupToDelete, itemEntities, modifiedReductionValues,
    pendingMaster, pendingReductionValues, reductionValuesModified, selectedGroupWithStatus, selectedItem,
    selectionsFormModified,
} from './redux/selectors';
import {
    AccessState, AccessStateCardAttributes, AccessStateFilters, AccessStateModals,
    AccessStatePending, AccessStateSelected,
} from './redux/store';
import { SelectionsPanel } from './selections-panel';

type ClientEntity = (ClientWithStats & { indent: 1 | 2 }) | 'divider';
interface RootContentItemEntity extends RootContentItemWithPublication {
  contentTypeName: string;
}
interface SelectionGroupEntity extends SelectionGroupWithStatus {
  assignedUsers: User[];
  assignedUserCount: number;
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
  valuesModified: boolean;
  selectedMaster: boolean;
  formModified: boolean;
  addableUsers: User[];

  allGroupsExpanded: boolean;
  allGroupsCollapsed: boolean;
  allValuesSelected: boolean;
  allValuesDeselected: boolean;
  groupToDelete: SelectionGroup;
}

class ContentAccessAdmin extends React.Component<ContentAccessAdminProps & typeof AccessActionCreators> {
  private readonly currentView: string = document
    .getElementsByTagName('body')[0].getAttribute('data-nav-location');

  public componentDidMount() {
    this.props.fetchGlobalData({});
    this.props.fetchClients({});
    this.props.scheduleStatusRefresh({ delay: 0 });
    this.props.scheduleSessionCheck({ delay: 0 });
    setUnloadAlert(() => this.props.pending.group.id !== null);
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
                if (pending.group.id !== null) {
                  this.props.promptGroupEditing({});
                  return;
                }
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
    return activeClient && (
      <CardPanel
        entities={items}
        loading={pending.data.items}
        renderEntity={(entity, key) => (
          <Card
            key={key}
            selected={selected.item === entity.id}
            onSelect={() => {
              if (pending.group.id !== null) {
                this.props.promptGroupEditing({});
                return;
              }
              if (selected.item !== entity.id) {
                this.props.fetchGroups({ contentItemId: entity.id });
              }
              this.props.selectItem({ id: entity.id });
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
            setFilterText={(text) => this.props.setFilterTextItem({ text })}
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
      items,
      modals,
      cardAttributes,
      addableUsers: users,
      allGroupsExpanded: allExpanded,
      allGroupsCollapsed: allCollapsed,
      groupToDelete: groupDelete,
    } = this.props;

    const selectedItemIsPublishing = item && isPublicationActive(
      items.filter((x) => x.id === item.id)[0].status.requestStatus,
    );
    const expandAllIcon = allExpanded
      ? null
      : (
        <ActionIcon
          label="Expand all"
          icon="expand-cards"
          action={() => this.props.setAllExpandedGroup({})}
        />
      );
    const collapseAllIcon = allCollapsed
      ? null
      : (
        <ActionIcon
          label="Collapse all"
          icon="collapse-cards"
          action={() => this.props.setAllCollapsedGroup({})}
        />
      );
    const addGroupIcon = !selectedItemIsPublishing ? (
        <ActionIcon
          label="Add group"
          icon="add"
          action={() => this.props.openAddGroupModal({})}
        />
      ) : null;

    return activeClient && activeItem && (
      <CardPanel
        entities={groups}
        loading={pending.data.groups}
        renderEntity={(entity, key) => {
          const card = cardAttributes.group[entity.id];
          const cardButtons = entity.editing
            ? (
              <>
                <CardButton
                  color={'green'}
                  tooltip={'Save changes'}
                  onClick={() => {
                    if (pending.group.name === '') {
                      this.props.promptGroupNameEmpty({});
                      return;
                    }
                    this.props.updateGroup({
                      groupId: entity.id,
                      name: entity.name,
                      users: entity.assignedUsers.map((u) => u.id),
                    });
                  }}
                  icon={'checkmark'}
                />
                <CardButton
                  color={'red'}
                  tooltip={'Cancel'}
                  onClick={() => this.props.setGroupEditingOff({ id: entity.id })}
                  icon={'cancel'}
                />
              </>
            )
            : (
              <>
                {
                  (!selectedItemIsPublishing)
                    ? isReductionActive(entity.status.taskStatus)
                      ? (
                        <CardButton
                          color={'red'}
                          tooltip={'Cancel Reduction Task'}
                          onClick={() => this.props.cancelReduction({ groupId: entity.id })}
                          icon={'cancel'}
                        />
                      ) : (
                        <CardButton
                          color={'red'}
                          tooltip={'Delete selection group'}
                          onClick={() => this.props.openDeleteGroupModal({ id: entity.id })}
                          icon={'delete'}
                        />
                      )
                  : null
                }
                {pending.group.id === null
                  ? (
                    <CardButton
                      color={'blue'}
                      tooltip={'Edit selection group'}
                      onClick={() => this.props.setGroupEditingOn({ id: entity.id })}
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
                  ? this.props.setExpandedGroup({ id: entity.id })
                  : this.props.setCollapsedGroup({ id: entity.id})}
              >
                <ul className="detail-item-user-list">
                  {entity.assignedUsers.map((u) => (
                    <li key={u.id}>
                      <span className="detail-item-user">
                        <div className="detail-item-icon">
                        {
                          entity.editing
                            ? <CardButton
                              icon="remove-circle"
                              color="red"
                              onClick={() => this.props.setPendingGroupUserRemoved({ id: u.id })}
                            />
                            : <svg
                              className="action-icon"
                              style={{
                                width: '2em',
                                height: '2em',
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
                        <span
                          className="detail-item-user"
                          onClick={(event) => event.stopPropagation()}
                        >
                          <Select
                            className="react-select"
                            classNamePrefix="react-select"
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
                                this.props.setPendingGroupUserAssigned({ id: singleValue.value });
                              }
                            }}
                            onInputChange={(query) => this.props.setPendingGroupUserQuery({ query })}
                            inputValue={entity.userQuery}
                            controlShouldRenderValue={false}
                            placeholder="Select users..."
                            autoFocus={true}
                          />
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
                if (pending.group.id !== null && pending.group.id !== entity.id) {
                  this.props.promptGroupEditing({});
                  return;
                }
                if (selected.group !== entity.id) {
                  this.props.fetchSelections({ groupId: entity.id });
                }
                this.props.selectGroup({ id: entity.id });
              }}
              suspended={entity.isSuspended}
              inactive={entity.isInactive}
              locked={entity.id === pending.group.id}
              status={entity.status}
            >
              <CardSectionMain>
                <CardText
                  text={entity.name}
                  textSuffix={entity.isSuspended ? '[Suspended]' : entity.isInactive ? '[Inactive]' : ''}
                  subtext={item.name}
                  editing={entity.editing}
                  setText={(name) => this.props.setPendingGroupName({ name })}
                />
                <div
                  className="card-stats-container"
                  onClick={(event) => {
                    event.stopPropagation();
                    card && card.expanded
                      ? this.props.setCollapsedGroup({ id: entity.id})
                      : this.props.setExpandedGroup({ id: entity.id });
                  }}
                >
                  <CardStat
                    name={'Assigned users'}
                    value={entity.assignedUserCount}
                    icon={'user'}
                  />
                </div>
                <CardSectionButtons>
                  {cardButtons}
                </CardSectionButtons>
              </CardSectionMain>
              {cardExpansion}
            </Card>
          );
        }}
        renderNewEntityButton={() => !selectedItemIsPublishing && (
          <div className="card-container action-card-container" onClick={() => this.props.openAddGroupModal({})}>
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
        )}
      >
        <h3 className="admin-panel-header">Selection Groups</h3>
        <PanelSectionToolbar>
          <Filter
            placeholderText={'Filter selection groups...'}
            setFilterText={(text) => this.props.setFilterTextGroup({ text })}
            filterText={filters.group.text}
          />
          <PanelSectionToolbarButtons>
            {expandAllIcon}
            {collapseAllIcon}
            {addGroupIcon}
          </PanelSectionToolbarButtons>
        </PanelSectionToolbar>
        <Modal
          isOpen={modals.addGroup.isOpen}
          onRequestClose={() => this.props.closeAddGroupModal({})}
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
              if (!pending.data.createGroup && pending.newGroupName) {
                this.props.createGroup({
                  contentItemId: this.props.selectedItem.id,
                  name: this.props.pending.newGroupName,
                });
              }
            }}
          >
            <input
              type="text"
              placeholder="Selection group name"
              onChange={(event) => this.props.setPendingNewGroupName({
                name: event.target.value,
              })}
              value={this.props.pending.newGroupName}
              autoFocus={true}
            />
            <div className="button-container">
              <button className="link-button" type="button" onClick={() => this.props.closeAddGroupModal({})}>
                Cancel
              </button>
              <button
                className={`blue-button${pending.newGroupName ? '' : ' disabled'}`}
                type="submit"
              >
                Add
                {this.props.pending.data.createGroup
                  ? <ButtonSpinner version="circle" />
                  : null
                }
              </button>
            </div>
          </form>
        </Modal>
        <Modal
          isOpen={modals.deleteGroup.isOpen}
          onRequestClose={() => this.props.closeDeleteGroupModal({})}
          ariaHideApp={false}
          className="modal"
          overlayClassName="modal-overlay"
          closeTimeoutMS={100}
        >
          <h3 className="title red">Delete Selection Group</h3>
          <span className="modal-text">Delete <strong>{groupDelete ? groupDelete.name : ''}</strong>?</span>
          <div className="button-container">
            <button className="link-button" type="button" onClick={() => this.props.closeDeleteGroupModal({})}>
              Cancel
            </button>
            <button
              className="red-button"
              onClick={() => {
                if (!this.props.pending.data.deleteGroup) {
                  this.props.deleteGroup({ groupId: this.props.groupToDelete.id });
                }
              }}
            >
              Delete
              {this.props.pending.data.deleteGroup
                ? <ButtonSpinner version="circle" />
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
      allValuesDeselected: allDeselected,
      allValuesSelected: allSelected,
      selectedItem: item,
      selectedGroup: group,
      reductionFieldsets,
      activeSelectedClient: activeClient,
      activeSelectedItem: activeItem,
      activeSelectedGroup: activeGroup,
      filters,
      items,
      pending,
      modals,
      selectedValues,
      modifiedValues,
      selectedMaster,
      formModified,
      valuesModified,
    } = this.props;
    const fieldsets = reductionFieldsets.map((s) => ({
      name: s.field.displayName,
      fields: s.values.map((v) => ({
        name: v.value,
        selected: selectedValues.indexOf(v.id) !== -1,
        modified: modifiedValues.indexOf(v.id) !== -1,
        onChange: (selected: boolean) => selected
          ? this.props.setPendingSelectionOn({ id: v.id })
          : this.props.setPendingSelectionOff({ id: v.id }),
      })),
    }));
    return activeClient && activeItem && activeGroup && (
      <SelectionsPanel
        isSuspended={group.isSuspended}
        onIsSuspendedChange={(value) => this.props.suspendGroup({ groupId: group.id, isSuspended: value })}
        doesReduce={item.doesReduce}
        isAllValuesSelected={allSelected}
        isAllValuesDeselected={allDeselected}
        isModified={formModified}
        isValuesModified={valuesModified}
        isMaster={selectedMaster}
        onIsMasterChange={(isMaster) => this.props.setPendingIsMaster({ isMaster })}
        title={group.name}
        subtitle={item.name}
        status={group.status.taskStatus || ReductionStatus.Unspecified}
        itemStatus={items.filter((x) => x.id === item.id)[0].status.requestStatus}
        onBeginReduction={() => selectedValues.length || selectedMaster
          ? this.props.updateSelections({
            groupId: group.id,
            isMaster: selectedMaster,
            selections: selectedValues,
          })
          : this.props.openInactiveModal({})
        }
        onCancelReduction={() => this.props.cancelReduction({ groupId: group.id })}
        loading={pending.data.selections}
        onSetPendingAllSelectionsOff={() => this.props.setPendingAllSelectionsOff({})}
        onSetPendingAllSelectionsOn={() => this.props.setPendingAllSelectionsOn({})}
        onSetPendingAllSelectionsReset={() => this.props.setPendingAllSelectionsReset({})}
        submitting={pending.data.updateSelections || pending.data.cancelReduction}
        fieldsets={fieldsets}
      >
        <PanelSectionToolbar>
          <Filter
            placeholderText={'Filter fields and values...'}
            setFilterText={(text) => this.props.setFilterTextSelections({ text })}
            filterText={filters.selections.text}
          />
        </PanelSectionToolbar>
        <Modal
          isOpen={modals.invalidate.isOpen}
          onRequestClose={() => this.props.closeInactiveModal({})}
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
            <button className="link-button" type="button" onClick={() => this.props.closeInactiveModal({})}>
              Cancel
            </button>
            <button
              className="orange-button"
              onClick={() => {
                if (!this.props.pending.data.updateSelections) {
                  this.props.updateSelections({
                    groupId: group.id,
                    isMaster: selectedMaster,
                    selections: selectedValues,
                  });
                }
              }}
            >
              Proceed
              {this.props.pending.data.updateSelections
                ? <ButtonSpinner version="circle" />
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
    valuesModified: reductionValuesModified(state),
    selectedMaster: pendingMaster(state),
    formModified: selectionsFormModified(state),
    addableUsers: addableUsers(state),
    allGroupsExpanded: allGroupsExpanded(state),
    allGroupsCollapsed: allGroupsCollapsed(state),
    allValuesSelected: allValuesSelected(state),
    allValuesDeselected: allValuesDeselected(state),
    groupToDelete: groupToDelete(state),
  };
}

export const ConnectedContentAccessAdmin = connect(
  mapStateToProps,
  AccessActionCreators,
)(ContentAccessAdmin);
