import * as React from 'react';
import * as Modal from 'react-modal';
import { connect } from 'react-redux';
import ReduxToastr from 'react-redux-toastr';

import * as FileDropActionCreator from './redux/action-creators';
import * as Selector from './redux/selectors';
import * as State from './redux/store';

import { generateUniqueId } from '../../generate-unique-identifier';
import {
  AvailableEligibleUsers, FileDropClientWithStats, FileDropWithStats, PermissionGroupsChangesModel,
  PermissionGroupsReturnModel,
} from '../models';
import { ActionIcon } from '../shared-components/action-icon';
import { ButtonSpinner } from '../shared-components/button-spinner';
import { CardPanel } from '../shared-components/card-panel/card-panel';
import { PanelSectionToolbar, PanelSectionToolbarButtons } from '../shared-components/card-panel/panel-sections';
import { Card } from '../shared-components/card/card';
import CardButton from '../shared-components/card/card-button';
import {
  CardSectionButtons, CardSectionMain, CardSectionStats, CardText,
} from '../shared-components/card/card-sections';
import { CardStat } from '../shared-components/card/card-stat';
import { ContentPanel, ContentPanelSectionContent } from '../shared-components/content-panel/content-panel';
import { Filter } from '../shared-components/filter';
import { ContentPanelForm } from '../shared-components/form/form-elements';
import { Input, TextAreaInput } from '../shared-components/form/input';
import { NavBar } from '../shared-components/navbar';
import { TabRow } from '../shared-components/tab-row';
import { PermissionsTable } from './permissions-table';

type ClientEntity = (FileDropClientWithStats & { indent: 1 | 2 }) | 'divider';

interface FileDropProps {
  data: State.FileDropDataState;
  clients: ClientEntity[];
  fileDrops: FileDropWithStats[];
  permissionGroups: PermissionGroupsReturnModel;
  selected: State.FileDropSelectedState;
  cardAttributes: State.FileDropCardAttributesState;
  pending: State.FileDropPendingState;
  filters: State.FileDropFilterState;
  modals: State.FileDropModals;
  activeSelectedClient: FileDropClientWithStats;
  permissionGroupChangesPending: boolean;
  permissionGroupChangesReady: boolean;
  pendingPermissionGroupsChanges: PermissionGroupsChangesModel;
  unassignedEligibleUsers: AvailableEligibleUsers[];
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
    const { selected, modals, pending, unassignedEligibleUsers, activeSelectedClient } = this.props;
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
        {selected.client && this.renderFileDropPanel()}
        {selected.fileDrop && this.renderFileDropManagementPanel()}
        <Modal
          isOpen={modals.createFileDrop.isOpen}
          onRequestClose={() => this.props.closeCreateFileDropModal({})}
          ariaHideApp={false}
          className="modal"
          overlayClassName="modal-overlay"
          closeTimeoutMS={100}
        >
          <h3 className="title blue">Add File Drop</h3>
          <form
            onSubmit={(e) => {
              {
                e.preventDefault();
                if (pending.createFileDrop.fileDropName) {
                  this.props.createFileDrop({
                    clientId: pending.createFileDrop.clientId,
                    name: pending.createFileDrop.fileDropName,
                    description: pending.createFileDrop.fileDropDescription,
                  });
                }
              }
            }}
          >
            <Input
              autoFocus={true}
              error={pending.createFileDrop.errors.fileDropName}
              label="File Drop Name"
              name="File Drop Name"
              onChange={({ currentTarget: target }: React.FormEvent<HTMLInputElement>) => {
                this.props.updateFileDropFormData({
                  updateType: 'create',
                  field: 'fileDropName',
                  value: target.value,
                });
              }}
              placeholderText="New File Drop Name *"
              type="text"
              value={pending.createFileDrop.fileDropName}
            />
            <TextAreaInput
              error={pending.createFileDrop.errors.fileDropDescription}
              label="File Drop Description"
              name="File Drop Description"
              onChange={({ currentTarget: target }: React.FormEvent<HTMLTextAreaElement>) => {
                this.props.updateFileDropFormData({
                  updateType: 'create',
                  field: 'fileDropDescription',
                  value: target.value,
                });
              }}
              placeholderText="File Drop Description"
              value={pending.createFileDrop.fileDropDescription}
            />
            <div className="button-container">
              <button className="link-button" type="button" onClick={() => this.props.closeCreateFileDropModal({})}>
                Cancel
              </button>
              <button
                className={'blue-button'}
                disabled={!pending.createFileDrop.fileDropName}
                type="submit"
              >
                Add
                  {this.props.pending.async.createFileDrop
                  ? <ButtonSpinner version="circle" />
                  : null
                }
              </button>
            </div>
          </form>
        </Modal>
        <Modal
          isOpen={modals.deleteFileDrop.isOpen}
          onRequestClose={() => this.props.closeDeleteFileDropModal({})}
          ariaHideApp={false}
          className="modal"
          overlayClassName="modal-overlay"
          closeTimeoutMS={100}
        >
          <h3 className="title red">Delete File Drop</h3>
          <span className="modal-text">
            Delete <strong>{
              (pending.fileDropToDelete.id !== null)
                ? pending.fileDropToDelete.name
                : ''}</strong>?
          </span>
          <div className="button-container">
            <button
              className="link-button"
              type="button"
              onClick={() => this.props.closeDeleteFileDropModal({})}
            >
              Cancel
            </button>
            <button
              className="red-button"
              onClick={() => {
                // Add a slight pause to make it obvious that you've switched modals
                setTimeout(() => this.props.openDeleteFileDropConfirmationModal({}), 400);
              }}
            >
              Delete
              {pending.async.deleteFileDrop
                ? <ButtonSpinner version="circle" />
                : null
              }
            </button>
          </div>
        </Modal>
        <Modal
          isOpen={modals.confirmDeleteFileDrop.isOpen}
          onRequestClose={() => this.props.closeDeleteFileDropConfirmationModal({})}
          ariaHideApp={false}
          className="modal"
          overlayClassName="modal-overlay"
          closeTimeoutMS={100}
        >
          <h3 className="title red">Confirm Deletion of File Drop</h3>
          <span className="modal-text">
            Delete <strong>{
              (pending.fileDropToDelete.id !== null)
                ? pending.fileDropToDelete.name
                : ''}</strong>?
            <br />
            <br />
            <strong>THIS ACTION WILL DELETE ALL EXISTING FILES IN THIS FILE DROP.</strong>
            <br />
            <br />
            <strong>THIS ACTION CANNOT BE UNDONE.</strong>
          </span>
          <div className="button-container">
            <button
              className="link-button"
              type="button"
              onClick={() => this.props.closeDeleteFileDropConfirmationModal({})}
            >
              Cancel
            </button>
            <button
              className="red-button"
              onClick={() => {
                if (!pending.async.deleteFileDrop) {
                  this.props.deleteFileDrop(pending.fileDropToDelete.id);
                }
              }}
            >
              Confirm Deletion
              {pending.async.deleteFileDrop
                ? <ButtonSpinner version="circle" />
                : null
              }
            </button>
          </div>
        </Modal>
        <Modal
          isOpen={modals.formModified.isOpen}
          onRequestClose={() => this.props.closeModifiedFormModal({})}
          ariaHideApp={false}
          className="modal"
          overlayClassName="modal-overlay"
          closeTimeoutMS={100}
        >
          <h3 className="title red">Discard Changes</h3>
          <span className="modal-text">Would you like to discard unsaved changes?</span>
          <div className="button-container">
            <button
              className="link-button"
              type="button"
              onClick={() => this.props.closeModifiedFormModal({})}
            >
              Continue Editing
            </button>
            <button
              className="red-button"
              onClick={() => {
                const { data, fileDrops } = this.props;
                const { entityToSelect, entityType } = pending.afterFormModal;
                this.props.discardPendingPermissionGroupChanges({ originalValues: data.permissionGroups });
                switch (entityType) {
                  case 'Select Client':
                    if (selected.client !== entityToSelect) {
                      this.props.fetchFileDrops({ clientId: entityToSelect });
                    }
                    this.props.selectClient({ id: entityToSelect });
                    break;
                  case 'Select File Drop':
                    this.props.selectFileDrop({ id: entityToSelect });
                    if (activeSelectedClient.canManageFileDrops) {
                      this.props.selectFileDropTab({ tab: 'permissions' });
                      if (selected.fileDrop !== entityToSelect && entityToSelect !== null) {
                        this.props.fetchPermissionGroups({
                          clientId: selected.client,
                          fileDropId: entityToSelect,
                        });
                      }
                    } else {
                      this.props.selectFileDropTab({ tab: 'settings' });
                      // TODO: Call the appropriate action for this tab
                    }
                    break;
                  case 'Delete File Drop':
                    const fileDrop = fileDrops.filter((fD) => fD.id === entityToSelect);
                    if (fileDrop.length === 1) {
                      // Add a slight pause to make it obvious that you've switched modals
                      setTimeout(() => this.props.openDeleteFileDropModal({
                        fileDrop: fileDrops[0],
                      }), 400);
                    }
                    break;
                  case 'New File Drop':
                    setTimeout(() =>
                      this.props.openCreateFileDropModal({ clientId: selected.client }),
                      400,
                    );
                    break;
                  case 'Undo Changes':
                    // This action is triggered for every outcome
                    break;
                  case 'Undo Changes and Close Form':
                    this.props.setEditModeForPermissionGroups({ editModeEnabled: false });
                    break;
                  case 'files':
                    // TODO: Call the appropriate fetch action here
                    this.props.selectFileDropTab({ tab: 'files' });
                    break;
                  case 'activityLog':
                    this.props.fetchActivityLog({ fileDropId: selected.fileDrop });
                    this.props.selectFileDropTab({ tab: 'activityLog' });
                    break;
                  case 'settings':
                    // TODO: Call the appropriate fetch action here
                    this.props.selectFileDropTab({ tab: 'settings' });
                    break;
                }
              }}
            >
              Discard
            </button>
          </div>
        </Modal>
      </>
    );
  }

  private renderClientPanel() {
    const { clients, selected, filters, pending, cardAttributes, permissionGroupChangesPending } = this.props;
    return (
      <CardPanel
        entities={clients}
        loading={pending.async.clients}
        renderEntity={(entity, key) => {
          if (entity === 'divider') {
            return <div className="hr" key={key} />;
          }
          const card = cardAttributes.clients[entity.id];
          return (
            <Card
              key={key}
              selected={selected.client === entity.id}
              disabled={card.disabled}
              onSelect={() => {
                if (permissionGroupChangesPending) {
                  this.props.openModifiedFormModal({
                    afterFormModal: {
                      entityToSelect: entity.id,
                      entityType: 'Select Client',
                    },
                  });
                } else {
                   if (selected.client !== entity.id) {
                     this.props.fetchFileDrops({ clientId: entity.id });
                   }
                   this.props.selectClient({ id: entity.id });
                 }
                 // this.props.selectClient({ id: entity.id });
              }}
              indentation={entity.indent}
            >
              <CardSectionMain>
                <CardText text={entity.name} subtext={entity.code} />
                <CardSectionStats>
                  <CardStat
                    name={'File Drops'}
                    value={entity.fileDropCount}
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
            setFilterText={(text) => this.props.setFilterText({ filter: 'client', text })}
            filterText={filters.client.text}
          />
          <PanelSectionToolbarButtons>
            <div id="icons" />
          </PanelSectionToolbarButtons>
        </PanelSectionToolbar>
      </CardPanel>
    );
  }

  private renderFileDropPanel() {
    const {
      activeSelectedClient, selected, filters, pending, cardAttributes,
      fileDrops, permissionGroupChangesPending,
    } = this.props;
    const createNewFileDropIcon = (
      <ActionIcon
        label="New File Drop"
        icon="add"
        action={() => {
          if (permissionGroupChangesPending) {
            this.props.openModifiedFormModal({
              afterFormModal: {
                entityToSelect: selected.client,
                entityType: 'New File Drop',
              },
            });
          } else {
            this.props.selectFileDrop({ id: 'NEW FILE DROP' });
            this.props.openCreateFileDropModal({ clientId: selected.client });
          }
        }}
      />
    );
    const cardButtons = (entity: FileDropWithStats, canManageFileDrops: boolean, isEditing: boolean) => {
      return canManageFileDrops
        ? (
          <>
            {
              !isEditing &&
              <>
                <CardButton
                  color={'blue'}
                  tooltip={'Edit File Drop'}
                  onClick={() => {
                    this.props.editFileDrop({ fileDrop: entity });
                  }}
                  icon={'edit'}
                />
                <CardButton
                  color={'red'}
                  tooltip={'Delete File Drop'}
                  onClick={() => {
                    if (permissionGroupChangesPending) {
                      this.props.openModifiedFormModal({
                        afterFormModal: {
                          entityToSelect: entity.id,
                          entityType: 'Delete File Drop',
                        },
                      });
                    } else {
                      this.props.openDeleteFileDropModal({fileDrop: entity});
                    }
                  }}
                  icon={'delete'}
                />
              </>
            }
            {
              isEditing &&
              <>
                <CardButton
                  color={'green'}
                  tooltip={'Update File Drop'}
                  onClick={() => {
                    this.props.updateFileDrop({
                      clientId: pending.editFileDrop.clientId,
                      id: pending.editFileDrop.id,
                      name: pending.editFileDrop.fileDropName,
                      description: pending.editFileDrop.fileDropDescription,
                    });
                  }}
                  icon={'checkmark'}
                />
                <CardButton
                  color={'red'}
                  tooltip={'Cancel Edit'}
                  onClick={() => {
                    this.props.cancelFileDropEdit({});
                  }}
                  icon={'cancel'}
                />
              </>
            }
          </>
        ) : (
          null
        );
    };

    return Selector.activeSelectedClient && (
      <>
        <CardPanel
          entities={fileDrops}
          loading={pending.async.fileDrops}
          renderEntity={(entity, key) => {
            const cardEditing = (
              cardAttributes.fileDrops
              && cardAttributes.fileDrops[entity.id]
              && cardAttributes.fileDrops[entity.id].editing
              )
              ? cardAttributes.fileDrops[entity.id].editing
              : false;
            return (
              <Card
                key={key}
                selected={selected.fileDrop === entity.id}
                onSelect={() => {
                  if (permissionGroupChangesPending) {
                    this.props.openModifiedFormModal({
                      afterFormModal: {
                        entityToSelect: entity.id,
                        entityType: 'files',
                      },
                    });
                  } else {
                    this.props.selectFileDrop({ id: entity.id });
                    if (activeSelectedClient.canManageFileDrops) {
                      if (selected.fileDrop !== entity.id) {
                        this.props.fetchPermissionGroups({ clientId: selected.client, fileDropId: entity.id });
                      }
                      this.props.selectFileDropTab({ tab: 'permissions' });
                    } else {
                      this.props.selectFileDropTab({ tab: 'settings' });
                      // TODO: Call the appropriate action for this tab
                    }
                  }
                }}
                // suspended={entity.isSuspended}
              >
                <CardSectionMain>
                  {
                    !cardEditing &&
                      <CardText
                        text={entity.name}
                        // TODO: Implement this when isSuspended is available
                        // textSuffix={entity.isSuspended ? '[Suspended]' : ''}
                        subtext={entity.description}
                      />
                  }
                  {
                    cardEditing &&
                    <div className="card-body-primary-container">
                      <Input
                        autoFocus={true}
                        error={pending.editFileDrop.errors.fileDropName}
                        label="File Drop Name"
                        name="File Drop Name"
                        onChange={({ currentTarget: target }: React.FormEvent<HTMLInputElement>) => {
                          this.props.updateFileDropFormData({
                            updateType: 'edit',
                            field: 'fileDropName',
                            value: target.value,
                          });
                        }}
                        placeholderText="File Drop Name *"
                        type="text"
                        value={pending.editFileDrop.fileDropName}
                      />
                      <TextAreaInput
                        error={pending.editFileDrop.errors.fileDropDescription}
                        label="File Drop Description"
                        name="File Drop Description"
                        onChange={({ currentTarget: target }: React.FormEvent<HTMLTextAreaElement>) => {
                          this.props.updateFileDropFormData({
                            updateType: 'edit',
                            field: 'fileDropDescription',
                            value: target.value,
                          });
                        }}
                        placeholderText="File Drop Description"
                        value={pending.editFileDrop.fileDropDescription}
                      />
                    </div>
                  }
                  {
                    !cardEditing &&
                    <CardSectionStats
                      // TODO: Make this dynamic when canManage is available
                    >
                      <CardStat
                        name={'Authorized Users'}
                        value={entity.userCount}
                        icon={'user'}
                      />
                    </CardSectionStats>
                  }
                  <CardSectionButtons>
                    {cardButtons(entity, true, cardEditing)}
                  </CardSectionButtons>
                </CardSectionMain>
              </Card>
            );
          }}
          renderNewEntityButton={() => (
            <div
              className="card-container action-card-container"
              onClick={() => {
                if (permissionGroupChangesPending) {
                  this.props.openModifiedFormModal({
                    afterFormModal: {
                      entityToSelect: selected.client,
                      entityType: 'New File Drop',
                    },
                  });
                } else {
                  this.props.selectFileDrop({ id: 'NEW FILE DROP' });
                  this.props.openCreateFileDropModal({ clientId: selected.client });
                }
              }}
            >
              <div className="admin-panel-content">
                <div
                  className={
                    `
                    card-body-container card-100 action-card
                    ${this.props.selected.fileDrop === 'NEW FILE DROP' ? 'selected' : ''}
                  `
                  }
                >
                  <h2 className="card-body-primary-text">
                    <svg className="action-card-icon">
                      <use href="#add" />
                    </svg>
                    <span>NEW FILE DROP</span>
                  </h2>
                </div>
              </div>
            </div>
          )}
        >
          <h3 className="admin-panel-header">File Drops</h3>
          <PanelSectionToolbar>
            <Filter
              placeholderText={'Filter file drops...'}
              setFilterText={(text) => this.props.setFilterText({ filter: 'fileDrop', text })}
              filterText={filters.fileDrop.text}
            />
            <PanelSectionToolbarButtons>
              {createNewFileDropIcon}
            </PanelSectionToolbarButtons>
          </PanelSectionToolbar>
        </CardPanel>
      </>
    );
  }

  private renderFileDropManagementPanel() {
    const { activeSelectedClient, pending, permissionGroupChangesPending, selected } = this.props;
    const tabList: Array<{
      id: State.AvailableFileDropTabs;
      label: string;
    }> = (activeSelectedClient.canManageFileDrops)
      ? [
        // { id: 'files', label: 'Files' },
        { id: 'permissions', label: 'User Permissions' },
        { id: 'activityLog', label: 'Activity Log' },
        { id: 'settings', label: 'My Settings' },
      ] : [
        { id: 'settings', label: 'My Settings' },
      ];

    return (
      <ContentPanel loading={false}>
        <h3 className="admin-panel-header">File Drop Detail</h3>
        <TabRow
          tabs={tabList}
          selectedTab={pending.selectedFileDropTab}
          onTabSelect={(tab: State.AvailableFileDropTabs) => {
            if (permissionGroupChangesPending) {
              this.props.openModifiedFormModal({
                afterFormModal: {
                  entityToSelect: null,
                  entityType: tab,
                },
              });
            } else {
              switch (tab) {
                case 'files':
                  // TODO: Add appropriate call here.
                  break;
                case 'permissions':
                  this.props.fetchPermissionGroups({ clientId: selected.client, fileDropId: selected.fileDrop });
                  break;
                case 'activityLog':
                  this.props.fetchActivityLog({ fileDropId: selected.fileDrop });
                  break;
                case 'settings':
                  // TODO: Add appropriate call here.
                  break;
              }
              this.props.selectFileDropTab({ tab });
            }
          }}
          fullWidth={true}
        />
        {(() => {
          switch (pending.selectedFileDropTab) {
            case 'files':
              return null;
            case 'permissions':
              return this.renderPermissionsTab();
            case 'activityLog':
              return this.renderActivityLogTab();
            case 'settings':
              return this.renderSettingsTab();
            default:
              return null;
          }
        })()}
      </ContentPanel>
    );
  }

  private renderPermissionsTab() {
    const {
      data, filters, pending, pendingPermissionGroupsChanges, permissionGroupChangesPending,
      permissionGroupChangesReady, permissionGroups,
    } = this.props;
    const editPermissionGroupsButton = (
      <ActionIcon
        label="Edit Permission Groups"
        icon="edit"
        action={() => this.props.setEditModeForPermissionGroups({ editModeEnabled: true })}
      />
    );
    const cancelEditPermissionGroupsButton = (
      <ActionIcon
        label="Discard Changes"
        icon="cancel"
        action={() => {
          if (permissionGroupChangesPending) {
            this.props.openModifiedFormModal({
              afterFormModal: {
                entityToSelect: null,
                entityType: 'Undo Changes and Close Form',
              },
            });
          } else {
            this.props.setEditModeForPermissionGroups({ editModeEnabled: false });
          }
        }}
      />
    );
    const addUserButton = (
      <ActionIcon
        label="Add User"
        icon="add-user"
        disabled={!permissionGroupChangesReady}
        action={() => this.props.addNewPermissionGroup({ isSingleGroup: true, tempPGId: generateUniqueId('temp-pg') })}
      />
    );
    const addGroupButton = (
      <ActionIcon
        label="Add Group"
        icon="add-group"
        disabled={!permissionGroupChangesReady}
        action={() => this.props.addNewPermissionGroup({ isSingleGroup: false, tempPGId: generateUniqueId('temp-pg') })}
      />
    );

    return (
      <>
        <PanelSectionToolbar>
          <Filter
            placeholderText={'Filter permission groups/users...'}
            setFilterText={(text) => this.props.setFilterText({ filter: 'permissions', text })}
            filterText={filters.permissions.text}
          />
          <PanelSectionToolbarButtons>
            {!pending.permissionGroupsEditMode && editPermissionGroupsButton}
            {pending.permissionGroupsEditMode && addUserButton}
            {pending.permissionGroupsEditMode && addGroupButton}
            {pending.permissionGroupsEditMode && cancelEditPermissionGroupsButton}
          </PanelSectionToolbarButtons>
        </PanelSectionToolbar>
        <ContentPanelSectionContent>
          <ContentPanelForm
            readOnly={false}
          >
            <PermissionsTable
              permissions={permissionGroups}
              readOnly={!pending.permissionGroupsEditMode}
              isReadyToSubmit={permissionGroupChangesReady}
              unassignedEligibleUsers={this.props.unassignedEligibleUsers}
              addPermissionGroup={this.props.addNewPermissionGroup}
              setPermissionValue={this.props.setPermissionGroupPermissionValue}
              removePermissionGroup={this.props.removePermissionGroup}
              addUserToPermissionGroup={this.props.addUserToPermissionGroup}
              removeUserFromPermissionGroup={this.props.removeUserFromPermissionGroup}
              setPermissionGroupNameText={this.props.setPermissionGroupNameText}
            />
            {
              pending.permissionGroupsEditMode &&
              permissionGroupChangesPending &&
              <div className="button-container">
                <button
                  className="link-button"
                  type="button"
                  onClick={(event: any) => {
                    event.preventDefault();
                    this.props.openModifiedFormModal({
                      afterFormModal: {
                        entityToSelect: null,
                        entityType: 'Undo Changes',
                      },
                    });
                    // this.props.discardPendingPermissionGroupChanges({ originalValues: data.permissionGroups });
                  }}
                >
                  Discard Changes
                </button>
                <button
                  type="button"
                  className="green-button"
                  disabled={!permissionGroupChangesReady}
                  onClick={(event: React.MouseEvent) => {
                    event.preventDefault();
                    this.props.updatePermissionGroups(pendingPermissionGroupsChanges);
                  }}
                >
                  Save Changes
                  {this.props.pending.async.permissionsUpdate
                    ? <ButtonSpinner version="circle" />
                    : null
                  }
                </button>
              </div>
            }
          </ContentPanelForm>
        </ContentPanelSectionContent>
      </>
    );
  }

  private renderActivityLogTab() {
    const { filters } = this.props;
    return (
      <>
        <PanelSectionToolbar>
          <Filter
            placeholderText={'Filter events...'}
            setFilterText={(text) => this.props.setFilterText({ filter: 'activityLog', text })}
            filterText={filters.activityLog.text}
          />
          <PanelSectionToolbarButtons />
        </PanelSectionToolbar>
        <ContentPanelSectionContent>
          <div>Content Here...</div>
        </ContentPanelSectionContent>
      </>
    );
  }

  private renderSettingsTab() {
    return (
      <>
        <ContentPanelSectionContent>
          <div>Content Here...</div>
        </ContentPanelSectionContent>
      </>
    );
  }
}

function mapStateToProps(state: State.FileDropState): FileDropProps {
  const { data, selected, cardAttributes, pending, filters, modals } = state;

  return {
    data,
    clients: Selector.clientEntities(state),
    fileDrops: Selector.fileDropEntities(state),
    permissionGroups: Selector.permissionGroupEntities(state),
    selected,
    cardAttributes,
    pending,
    filters,
    modals,
    activeSelectedClient: Selector.activeSelectedClient(state),
    permissionGroupChangesPending: Selector.permissionGroupChangesPending(state),
    permissionGroupChangesReady: Selector.permissionGroupChangesReady(state),
    pendingPermissionGroupsChanges: Selector.pendingPermissionGroupsChanges(state),
    unassignedEligibleUsers: Selector.unassignedEligibleUsers(state),
  };
}

export const ConnectedFileDrop = connect(
  mapStateToProps,
  FileDropActionCreator,
)(FileDrop);
