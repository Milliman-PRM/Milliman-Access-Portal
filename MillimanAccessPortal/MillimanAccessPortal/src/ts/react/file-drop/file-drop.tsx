import * as React from 'react';
import * as Modal from 'react-modal';
import { connect } from 'react-redux';
import ReduxToastr from 'react-redux-toastr';

import * as FileDropActionCreator from './redux/action-creators';
import * as Selector from './redux/selectors';
import * as State from './redux/store';

import { Client, FileDropClientWithStats, FileDropWithStats } from '../models';
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
import { Input, TextAreaInput } from '../shared-components/form/input';
import { NavBar } from '../shared-components/navbar';
import { TabRow } from '../shared-components/tab-row';

type ClientEntity = (FileDropClientWithStats & { indent: 1 | 2 }) | 'divider';

interface FileDropProps {
  clients: ClientEntity[];
  fileDrops: FileDropWithStats[];
  selected: State.FileDropSelectedState;
  cardAttributes: State.FileDropCardAttributesState;
  pending: State.FileDropPendingState;
  filters: State.FileDropFilterState;
  modals: State.FileDropModals;
  activeSelectedClient: FileDropClientWithStats;
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
    const { selected, modals, pending } = this.props;
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
          const card = cardAttributes.clients[entity.id];
          return (
            <Card
              key={key}
              selected={selected.client === entity.id}
              disabled={card.disabled}
              onSelect={() => {
                 // TODO: Update this section once all of the necessary actions and data are available
                 if (false) {
                   // TODO: Properly implement any modals
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

  private renderFileDropPanel() {
    const { activeSelectedClient, clients, selected, filters, pending, cardAttributes, fileDrops } = this.props;
    const createNewFileDropIcon = (
      <ActionIcon
        label="New File Drop"
        icon="add"
        action={() => {
          if (false) {
            // TODO: implement any modals necessary before opening this modal
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
                    if (false) {
                      // TODO: Implement any necessary modals before performing action
                    } else {
                      // TODO: Implement this action
                      this.props.editFileDrop({ fileDrop: entity });
                    }
                  }}
                  icon={'edit'}
                />
                <CardButton
                  color={'red'}
                  tooltip={'Delete File Drop'}
                  onClick={() => {
                    if (false) {
                      // TODO: Implement any necessary modals before performing action
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
                    if (false) {
                      // TODO: Implement any necessary modals before performing action
                    } else {
                      // TODO: Implement this action
                      this.props.updateFileDrop({
                        clientId: pending.editFileDrop.clientId,
                        id: pending.editFileDrop.id,
                        name: pending.editFileDrop.fileDropName,
                        description: pending.editFileDrop.fileDropDescription,
                      });
                    }
                  }}
                  icon={'checkmark'}
                />
                <CardButton
                  color={'red'}
                  tooltip={'Cancel Edit'}
                  onClick={() => {
                    if (false) {
                      // TODO: Implement any necessary modals before performing action
                    } else {
                      this.props.cancelFileDropEdit({});
                    }
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
                  if (false) {
                    // TODO: Implement necessary modals
                  } else {
                    if (selected.fileDrop !== entity.id) {
                      // this.props.fetchFileDropDetail({ FileDropId: entity.id });
                    }
                    this.props.selectFileDrop({ id: entity.id });
                    if (activeSelectedClient.canManageFileDrops) {
                      this.props.selectFileDropTab({ tab: 'permissions' });
                    } else {
                      this.props.selectFileDropTab({ tab: 'settings' });
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
                if (false) {
                  // TODO Implement any necessary modals
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
              setFilterText={(text) => this.props.setFilterTextFileDrop({ text })}
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
    const { activeSelectedClient, pending } = this.props;
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
        <h3 className="admin-panel-header">Content Item</h3>
        <TabRow
          tabs={tabList}
          selectedTab={pending.selectedFileDropTab}
          onTabSelect={(tab: State.AvailableFileDropTabs) => this.props.selectFileDropTab({ tab })}
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
    return (
      <>
        <PanelSectionToolbar>
          <Filter
            placeholderText={'Filter Permission Groups/Users...'}
            setFilterText={() => false}
            filterText={''}
          />
          <PanelSectionToolbarButtons />
        </PanelSectionToolbar>
        <ContentPanelSectionContent>
          <div>Content Here...</div>
        </ContentPanelSectionContent>
      </>
    );
  }

  private renderActivityLogTab() {
    return (
      <>
        <PanelSectionToolbar>
          <Filter
            placeholderText={'Filter events...'}
            setFilterText={() => false}
            filterText={''}
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
    clients: Selector.clientEntities(state),
    fileDrops: Selector.fileDropEntities(state),
    selected,
    cardAttributes,
    pending,
    filters,
    modals,
    activeSelectedClient: Selector.activeSelectedClient(state),
  };
}

export const ConnectedFileDrop = connect(
  mapStateToProps,
  FileDropActionCreator,
)(FileDrop);
