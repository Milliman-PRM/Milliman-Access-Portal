import '../../../scss/react/file-drop/file-drop.scss';

import '../../../images/icons/expand-card.svg';

import * as moment from 'moment';
import * as React from 'react';
import * as Modal from 'react-modal';
import { connect } from 'react-redux';
import ReduxToastr from 'react-redux-toastr';

import { toastr } from 'react-redux-toastr';

import * as FileDropActionCreator from './redux/action-creators';
import * as Selector from './redux/selectors';
import * as State from './redux/store';

import { generateUniqueId } from '../../generate-unique-identifier';
import {
  AvailableEligibleUsers,
  FileDropClientWithStats,
  FileDropEvent,
  FileDropNotificationTypeEnum,
  FileDropWithStats,
  Guid,
  PermissionGroupsChangesModel,
  PermissionGroupsReturnModel,
} from '../models';
import { ActionIcon } from '../shared-components/action-icon';
import { ButtonSpinner } from '../shared-components/button-spinner';
import { CardPanel } from '../shared-components/card-panel/card-panel';
import { PanelSectionToolbar, PanelSectionToolbarButtons } from '../shared-components/card-panel/panel-sections';
import { Card } from '../shared-components/card/card';
import CardButton from '../shared-components/card/card-button';
import { CardExpansion } from '../shared-components/card/card-expansion';
import {
  CardSectionButtons, CardSectionMain, CardSectionStats, CardText,
} from '../shared-components/card/card-sections';
import { CardStat } from '../shared-components/card/card-stat';
import { ColumnSpinner } from '../shared-components/column-spinner';
import { ContentPanel, ContentPanelSectionContent } from '../shared-components/content-panel/content-panel';
import { Filter } from '../shared-components/filter';
import { ContentPanelForm, FormSection } from '../shared-components/form/form-elements';
import { Input, TextAreaInput } from '../shared-components/form/input';
import { Toggle } from '../shared-components/form/toggle';
import { NavBar } from '../shared-components/navbar';
import { TabRow } from '../shared-components/tab-row';
import { UploadStatusBar } from '../shared-components/upload-status-bar';
import { FileDropUpload } from './file-drop-upload';
import { FolderContents } from './folder-contents';
import { PermissionsTable } from './permissions-table';

type ClientEntity = (FileDropClientWithStats & { indent: 1 | 2 }) | 'divider';

interface FileDropProps {
  data: State.FileDropDataState;
  clients: ClientEntity[];
  fileDrops: FileDropWithStats[];
  permissionGroups: PermissionGroupsReturnModel;
  activityLog: FileDropEvent[];
  selected: State.FileDropSelectedState;
  cardAttributes: State.FileDropCardAttributesState;
  pending: State.FileDropPendingState;
  filters: State.FileDropFilterState;
  modals: State.FileDropModals;
  activeSelectedClient: FileDropClientWithStats;
  activeSelectedFileDrop: FileDropWithStats;
  activeSelectedFileDropFolderUploads: State.FileDropUploadState[];
  permissionGroupChangesPending: boolean;
  permissionGroupChangesReady: boolean;
  pendingPermissionGroupsChanges: PermissionGroupsChangesModel;
  unassignedEligibleUsers: AvailableEligibleUsers[];
}

class FileDrop extends React.Component<FileDropProps & typeof FileDropActionCreator> {
  protected dragUploadRef: React.RefObject<HTMLDivElement>;
  protected browseUploadRef: React.RefObject<HTMLInputElement>;

  private readonly currentView: string = document
    .getElementsByTagName('body')[0].getAttribute('data-nav-location');

  constructor(props: FileDropProps & typeof FileDropActionCreator) {
    super(props);
    this.dragUploadRef = React.createRef();
    this.browseUploadRef = React.createRef();
  }

  public componentDidMount() {
    this.props.scheduleStatusRefresh({ delay: 0 });
    this.props.scheduleSessionCheck({ delay: 0 });
    this.props.initializeFirstUploadObject({});
    this.props.fetchClients({});
  }

  public render() {
    const { selected, modals, pending, activeSelectedClient, data } = this.props;
    const isFileDropAdmin = data.clients && Object.keys(data.clients).some((client) => {
      return data.clients[client].canManageFileDrops === true;
    });

    return (
      <>
        <div>
          {
            Object.keys(pending.uploads).map((upload) => {
              const uploadObject = pending.uploads[upload];
              return (
                <FileDropUpload
                  key={upload}
                  uploadId={upload}
                  clientId={uploadObject.clientId || selected.client}
                  disallowedFileNames={data.fileDropContents
                    && data.fileDropContents.files
                    && data.fileDropContents.files.map((file) => file.fileName)}
                  fileDropId={uploadObject.fileDropId || selected.fileDrop}
                  fileName={uploadObject.fileName}
                  folderId={uploadObject.folderId || selected.fileDropFolder.folderId}
                  canonicalPath={uploadObject.canonicalPath || selected.fileDropFolder.canonicalPath}
                  cancelable={uploadObject.cancelable}
                  canceled={uploadObject.canceled}
                  postErrorToast={(toastMsg) => toastr.error('', toastMsg)}
                  postSuccessToast={(toastMsg) => toastr.success('', toastMsg)}
                  dragRef={uploadObject.cancelable ? null : this.dragUploadRef}
                  browseRef={uploadObject.cancelable ? null : this.browseUploadRef}
                  writeAccess={(data.fileDropContents &&
                    data.fileDropContents.currentUserPermissions) ?
                    data.fileDropContents.currentUserPermissions.writeAccess : false
                  }
                  beginUpload={(uploadId, clientId, fileDropId, folderId, canonicalPath, fileName) =>
                    this.props.beginFileDropFileUpload({
                      uploadId, clientId, fileDropId, folderId, canonicalPath, fileName,
                    })}
                  cancelFileUpload={(uploadId) =>
                    this.props.cancelFileUpload({ uploadId })}
                  finalizeFileDropUpload={(uploadId, fileDropId, folderId, canonicalPath) =>
                    this.props.finalizeFileDropUpload({ uploadId, fileDropId, folderId, canonicalPath })}
                  setUploadError={(uploadId, errorMsg) =>
                    this.props.setUploadError({ uploadId, errorMsg })}
                  updateChecksumProgress={(uploadId, progress) =>
                    this.props.updateChecksumProgress({ uploadId, progress })}
                  updateUploadProgress={(uploadId, progress) =>
                    this.props.updateUploadProgress({ uploadId, progress })}
                />
              );
            })
          }
        </div>
        <ReduxToastr
          timeOut={5000}
          newestOnTop={false}
          position="bottom-center"
          transitionIn="fadeIn"
          transitionOut="fadeOut"
        />
        <NavBar currentView={this.currentView} userGuidePath={isFileDropAdmin ? 'FileDropAdmin' : null} />
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
                if (pending.createFileDrop.fileDropName && !pending.async.createFileDrop) {
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
                disabled={!pending.createFileDrop.fileDropName.trim()}
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
                const { fileDrops } = this.props;
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
                      this.props.selectFileDropTab({ tab: 'files' });
                      if (selected.fileDrop !== entityToSelect && entityToSelect !== null) {
                        this.props.fetchPermissionGroups({
                          clientId: selected.client,
                          fileDropId: entityToSelect,
                        });
                      }
                    } else {
                      this.props.selectFileDropTab({ tab: 'files' });
                      this.props.fetchSettings({ fileDropId: entityToSelect });
                    }
                    break;
                  case 'Delete File Drop': {
                      const fileDrop = fileDrops.filter((fD) => fD.id === entityToSelect);
                      if (fileDrop.length === 1) {
                        // Add a slight pause to make it obvious that you've switched modals
                        setTimeout(() => this.props.openDeleteFileDropModal({
                          fileDrop: fileDrops[0],
                        }), 400);
                      }
                      break;
                    }
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
                    // Once this is implemented, a fetch to the files action should be called here
                    this.props.selectFileDropTab({ tab: 'files' });
                    break;
                  case 'activityLog':
                    this.props.fetchActivityLog({ fileDropId: selected.fileDrop });
                    this.props.selectFileDropTab({ tab: 'activityLog' });
                    break;
                  case 'settings':
                    this.props.fetchSettings({ fileDropId: selected.fileDrop });
                    this.props.selectFileDropTab({ tab: 'settings' });
                    break;
                }
              }}
            >
              Discard
            </button>
          </div>
        </Modal>
        <Modal
          isOpen={modals.passwordNotification.isOpen}
          onRequestClose={() => this.props.closePasswordNotificationModal({})}
          ariaHideApp={false}
          className="modal"
          overlayClassName="modal-overlay"
          closeTimeoutMS={100}
        >
          <h3 className="title blue">SFTP Connection Credentials</h3>
          <span className="modal-text">
            Please store the provided password in a secure location, such as a password manager.
          </span>
          <span className="modal-text">
            Once this window is closed, you will no longer be able to access this password, and must generate a new
            credential if this information is lost.
          </span>
          <div>
            <input
              type="text"
              id="password"
              defaultValue={data.fileDropSettings.fileDropPassword}
            />
            <table>
              <tbody>
                <tr>
                  <td><strong>Username:</strong></td>
                  <td>{data.fileDropSettings.sftpUserName}</td>
                </tr>
                <tr>
                  <td><strong>Password:</strong></td>
                  <td>{data.fileDropSettings.fileDropPassword}</td>
                </tr>
              </tbody>
            </table>
          </div>
          <div className="button-container">
            <button
              className="red-button"
              type="button"
              onClick={() => this.props.closePasswordNotificationModal({})}
            >
              Close
            </button>
            <button
              className="blue-button"
              onClick={() => {
                const passwordInput = document.getElementById('password') as HTMLInputElement;
                passwordInput.select();
                passwordInput.setSelectionRange(0, 99999);
                document.execCommand('copy');
                toastr.success('', 'Password copied to clipboard');
              }}
            >
              Copy Password
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
              disabled={!entity.authorizedFileDropUser && !entity.canManageFileDrops}
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
              }}
              indentation={entity.indent}
            >
              <CardSectionMain>
                <CardText text={entity.name} subtext={entity.canManageFileDrops ? entity.code : null} />
                {
                  !card.disabled &&
                  <CardSectionStats>
                    <CardStat
                      name={'File Drop users'}
                      value={entity.userCount}
                      icon={'user'}
                    />
                    <CardStat
                      name={'File Drops'}
                      value={entity.fileDropCount}
                      icon={'file-drop'}
                    />
                  </CardSectionStats>
                }
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
    const createNewFileDropIcon = activeSelectedClient.canManageFileDrops && (
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
    const activeUploads = (fileDropId: Guid) => {
      return Object.keys(pending.uploads)
        .filter((uploadId) => pending.uploads[uploadId].fileDropId === fileDropId)
        .map((uploadId) => {
          const upload = pending.uploads[uploadId];
          return (
            <div key={uploadId} className="file-drop-card-upload">
              <div className="filename">
                {upload.fileName}
                <ActionIcon
                  icon={'cancel'}
                  disabled={!upload.cancelable}
                  label="Cancel Upload"
                  action={() => this.props.beginFileDropUploadCancel({ uploadId })}
                />
              </div>
              <UploadStatusBar
                checksumProgress={upload.checksumProgress}
                uploadProgress={upload.uploadProgress}
                errorMsg={upload.errorMsg}
              />
            </div>
          );
        });
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
            const fdActiveUploads = activeUploads(entity.id);
            return (
              <Card
                key={key}
                selected={selected.fileDrop === entity.id}
                onSelect={() => {
                  if (permissionGroupChangesPending) {
                    this.props.openModifiedFormModal({
                      afterFormModal: {
                        entityToSelect: entity.id,
                        entityType: 'Select File Drop',
                      },
                    });
                  } else {
                    this.props.selectFileDrop({ id: entity.id });
                    if (selected.fileDrop !== entity.id) {
                      this.props.fetchFolderContents({ fileDropId: entity.id, canonicalPath: '/' });
                    }
                    this.props.selectFileDropTab({ tab: 'files' });
                  }
                }}
                suspended={entity.isSuspended}
                bannerMessage={fdActiveUploads.length > 0 ? {
                  level: 'informational',
                  message: (
                    <div
                      className="upload-message-container"
                      onClick={(event) => {
                        event.stopPropagation();
                        this.props.toggleFileDropCardExpansion({ fileDropId: entity.id });
                      }
                     }
                    >
                      <span className="upload-notice">
                        {`${fdActiveUploads.length} file${fdActiveUploads.length > 1 ? 's' : ''} currently uploading`}
                      </span>
                      <svg className={`expand-icon ${cardAttributes.fileDrops[entity.id].expanded ? 'inverted' : ''}`}>
                        <use xlinkHref={'#expand-card'} />
                      </svg>
                    </div>
                    ),
                  } : null
                }
              >
                <CardSectionMain>
                  {
                    !cardEditing &&
                      <CardText
                        text={entity.name}
                        textSuffix={entity.isSuspended ? '[Suspended]' : ''}
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
                    activeSelectedClient.canManageFileDrops && !cardEditing &&
                    <CardSectionStats>
                      <CardStat
                        name={'Authorized Users'}
                        value={entity.userCount}
                        icon={'user'}
                      />
                    </CardSectionStats>
                  }
                  {
                    activeSelectedClient.canManageFileDrops &&
                    <CardSectionButtons>
                      {cardButtons(entity, true, cardEditing)}
                    </CardSectionButtons>
                  }
                </CardSectionMain>
                {
                  fdActiveUploads.length > 0 &&
                  cardAttributes.fileDrops[entity.id].expanded &&
                  <CardExpansion expanded={true}>
                    {fdActiveUploads}
                  </CardExpansion>
                }
              </Card>
            );
          }}
          renderNewEntityButton={() => activeSelectedClient.canManageFileDrops && (
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
        { id: 'files', label: 'Files' },
        { id: 'permissions', label: 'User Permissions' },
        { id: 'activityLog', label: 'Activity Log' },
        { id: 'settings', label: 'My Settings' },
      ] : [
          { id: 'files', label: 'Files' },
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
                  this.props.fetchFolderContents({ fileDropId: selected.fileDrop, canonicalPath: '/' });
                  break;
                case 'permissions':
                  this.props.fetchPermissionGroups({ clientId: selected.client, fileDropId: selected.fileDrop });
                  break;
                case 'activityLog':
                  this.props.fetchActivityLog({ fileDropId: selected.fileDrop });
                  break;
                case 'settings':
                  this.props.fetchSettings({ fileDropId: selected.fileDrop });
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
              return this.renderFilesTab();
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

  private renderFilesTab() {
    const { fileDropContents } = this.props.cardAttributes;
    return (
      <>
        <PanelSectionToolbar>
          <Filter
            placeholderText={'Filter files or folders...'}
            setFilterText={() => false}
            filterText={''}
          />
          {
            this.props.data.fileDropContents &&
            this.props.data.fileDropContents.currentUserPermissions &&
            this.props.data.fileDropContents.currentUserPermissions.writeAccess &&
            <>
              <ActionIcon
                label="Add File"
                icon="add-file"
                action={() => this.browseUploadRef.current.click()}
              />
              <ActionIcon
                label="Add Folder"
                icon="add-folder"
                action={() => this.props.enterCreateFolderMode()}
              />
            </>
          }
        </PanelSectionToolbar>
        <ContentPanelSectionContent>
          <ContentPanelForm
            readOnly={false}
          >
            <div
              className="files-table-container"
              ref={this.dragUploadRef}
            >
              {
                this.props.data.fileDropContents &&
                <FolderContents
                  directories={this.props.data.fileDropContents.directories}
                  files={this.props.data.fileDropContents.files}
                  activeUploads={this.props.activeSelectedFileDropFolderUploads}
                  fileDropId={this.props.selected.fileDrop}
                  fileDropName={this.props.activeSelectedFileDrop.name}
                  fileDropContentAttributes={fileDropContents}
                  currentUserPermissions={this.props.data.fileDropContents.currentUserPermissions}
                  navigateTo={(fileDropId, canonicalPath) =>
                    this.props.fetchFolderContents({ fileDropId, canonicalPath })
                  }
                  beginFileDropUploadCancel={(uploadId) =>
                    this.props.beginFileDropUploadCancel({ uploadId })
                  }
                  thisDirectory={this.props.data.fileDropContents.thisDirectory}
                  createFolder={this.props.pending.createFolder}
                  browseRef={this.browseUploadRef}
                  deleteFile={(fileDropId, fileId) =>
                    this.props.deleteFileDropFile({ fileDropId, fileId })}
                  deleteFolder={(fileDropId, folderId) =>
                    this.props.deleteFileDropFolder({ fileDropId, folderId })}
                  expandFileOrFolder={(id, expanded) => this.props.setFileOrFolderExpansion({ id, expanded })}
                  editFileDropItem={(id, editing, fileName, description) =>
                    this.props.setFileOrFolderEditing({ id, editing, fileName, description })
                  }
                  updateFileDropItemDescription={(id, description) =>
                    this.props.updateFileOrFolderDescription({ id, description })
                  }
                  saveFileDropFileDescription={(fileDropId, fileId, fileDescription) =>
                    this.props.updateFileDropFile({
                      fileDropId,
                      fileId,
                      fileDescription,
                    })
                  }
                  saveFileDropFolderDescription={(fileDropId, folderId, folderDescription) =>
                    this.props.updateFileDropFolder({
                      fileDropId,
                      folderId,
                      folderDescription,
                    })
                  }
                  enterCreateFolderMode={() => this.props.enterCreateFolderMode({})}
                  exitCreateFolderMode={() => this.props.exitCreateFolderMode({})}
                  updateCreateFolderValues={(field, value) => this.props.updateCreateFolderValues({ field, value })}
                  createFileDropFolder={(fileDropId, containingFileDropDirectoryId, newFolderName, description) =>
                    this.props.createFileDropFolder({
                      fileDropId, containingFileDropDirectoryId, newFolderName, description,
                    })
                  }
                />
              }
            </div>
          </ContentPanelForm>
        </ContentPanelSectionContent>
      </>
    );
  }

  private renderPermissionsTab() {
    const {
      filters, pending, pendingPermissionGroupsChanges, permissionGroupChangesPending,
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
        label={permissionGroupChangesPending ? 'Discard Changes' : 'Exit Edit Mode'}
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
                  onClick={(event: React.MouseEvent) => {
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
    const { filters, activityLog, data, selected } = this.props;
    return (
      <>
        <PanelSectionToolbar>
          <Filter
            placeholderText={'Filter events...'}
            setFilterText={(text) => this.props.setFilterText({ filter: 'activityLog', text })}
            filterText={filters.activityLog.text}
          />
          <ActionIcon
            label="Refresh Activity Log"
            icon="reload"
            action={() => {
                this.props.fetchActivityLog({ fileDropId: selected.fileDrop });
              }
            }
          />
          <PanelSectionToolbarButtons />
        </PanelSectionToolbar>
        <ContentPanelSectionContent>
          <div className="activity-log-table-header">
            <span className="activity-log-header">Activity Log - <strong>Last 30 Days</strong></span>
            <a
              href={`./FileDrop/DownloadFullActivityLog?fileDropId=${this.props.activeSelectedFileDrop.id}`}
              className="download-button button blue-button"
              download={true}
            >
              Download All
            </a>
          </div>
          <ContentPanelForm
            readOnly={false}
          >
            {
              this.props.pending.async.activityLog &&
              <ColumnSpinner />
            }
            <div>
              <table className="activity-log-table">
                <thead>
                  <tr>
                    <th className="col-date">Date</th>
                    <th className="col-author">Performed by</th>
                    <th className="col-action">Action</th>
                    <th className="col-description">Description</th>
                  </tr>
                </thead>
                <tbody>
                  {
                    activityLog.map((logEvent) => (
                      <tr className="event-row" key={`${logEvent.timeStampUtc}`}>
                        <td className="date-width">
                          <span title={moment(logEvent.timeStampUtc).local().format('MM/DD/YYYY h:mm:ss A')}>
                            {
                              moment(logEvent.timeStampUtc).local().format('M/D/YY \nh:mmA')
                            }
                          </span>
                        </td>
                        <td className="name-max-width">
                          <span
                            title={logEvent.fullName}
                          >
                            {logEvent.fullName}
                          </span>
                          <br />
                          <span
                            className="username"
                            title={logEvent.userName}
                          >
                            {logEvent.userName}
                          </span>
                        </td>
                        <td className="action-text">{logEvent.eventType}</td>
                        <td>{logEvent.description}</td>
                      </tr>
                    ))
                  }
                </tbody>
              </table>
            </div>
          </ContentPanelForm>
        </ContentPanelSectionContent>
      </>
    );
  }

  private renderSettingsTab() {
    const { fileDrop } = this.props.selected;
    const { fileDropSettings } = this.props.data;
    const uploadNotification = fileDropSettings && fileDropSettings.notifications
      ? fileDropSettings.notifications.filter((x) =>
        x.notificationType === FileDropNotificationTypeEnum.FileWritten,
      )[0] || null
      : null;
    return (
      <>
        <ContentPanelSectionContent>
          <ContentPanelForm
            readOnly={false}
          >
            {
              this.props.pending.async.settings &&
              <ColumnSpinner />
            }
            {
              !this.props.pending.async.settings &&
              <>
                <FormSection title="SFTP Connection Information">
                  <table className="sftpConnectionInfoTable">
                    <tbody>
                      <tr>
                        <td><strong>File Drop:</strong></td>
                        <td>{this.props.data.fileDrops[fileDrop].name}</td>
                      </tr>
                      <tr>
                        <td><strong>Protocol:</strong></td>
                        <td>SFTP - SSH File Transfer Protocol</td>
                      </tr>
                      <tr>
                        <td><strong>Host:</strong></td>
                        <td>{fileDropSettings.sftpHost}</td>
                      </tr>
                      <tr>
                        <td><strong>Port:</strong></td>
                        <td>{fileDropSettings.sftpPort}</td>
                      </tr>
                      <tr>
                        <td><strong>Fingerprint (MD5):</strong></td>
                        <td>{fileDropSettings.fingerprint}</td>
                      </tr>
                    </tbody>
                  </table>
                </FormSection>
                {
                  fileDropSettings.assignedPermissionGroupId &&
                  <FormSection title="SFTP Credentials">
                    {
                      !fileDropSettings.userHasPassword &&
                      <span
                        className="button blue-button"
                        onClick={() => this.props.generateNewSftpPassword(fileDrop)}
                      >
                        Generate Credentials
                      </span>
                    }
                    {
                      fileDropSettings.userHasPassword &&
                      <>
                        <table className="sftpCredentialsTable">
                          <tbody>
                            <tr>
                              <td><strong>Username:</strong></td>
                              <td>{fileDropSettings.sftpUserName}</td>
                            </tr>
                            <tr>
                              <td><strong>SFTP Account Status:</strong></td>
                              <td>
                                {
                                  (!fileDropSettings.isPasswordExpired && !fileDropSettings.isSuspended)
                                    ? 'Active'
                                    : fileDropSettings.isPasswordExpired
                                      ? 'Password Expired'
                                      : 'Suspended'
                                }
                              </td>
                            </tr>
                          </tbody>
                        </table>
                        <span
                          className="button blue-button"
                          onClick={() => this.props.generateNewSftpPassword(fileDrop)}
                        >
                          {
                            fileDropSettings.userHasPassword
                              ? 'Regenerate Password'
                              : 'Generate Credentials'
                          }
                        </span>
                      </>
                    }
                  </ FormSection>
                }
                {
                  fileDropSettings.assignedPermissionGroupId &&
                  uploadNotification &&
                  uploadNotification.canModify &&
                  <FormSection title="Notification Settings">
                    {
                      uploadNotification &&
                      uploadNotification.canModify &&
                      <Toggle
                        label="Upload"
                        checked={uploadNotification.isEnabled}
                        readOnly={!uploadNotification.canModify}
                        onClick={() => {
                          if (uploadNotification.canModify) {
                            this.props.setFileDropNotificationSetting({
                              fileDropId: fileDrop,
                              notifications: [{
                                notificationType: FileDropNotificationTypeEnum.FileWritten,
                                isEnabled: !uploadNotification.isEnabled,
                              }],
                            });
                          }
                        }}
                      />
                    }
                    <p className="notification-instruction">
                      Receive an email when a file is uploaded to this File Drop
                    </p>
                  </ FormSection>
                }
              </>
            }
          </ContentPanelForm>
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
    activityLog: Selector.activityLogEntities(state),
    selected,
    cardAttributes,
    pending,
    filters,
    modals,
    activeSelectedClient: Selector.activeSelectedClient(state),
    activeSelectedFileDrop: Selector.activeSelectedFileDrop(state),
    activeSelectedFileDropFolderUploads: Selector.activeSelectedFileDropFolderUploads(state),
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
