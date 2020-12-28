import '../../../scss/react/file-drop/folder-contents.scss';

import '../../../images/icons/add-file.svg';
import '../../../images/icons/add-folder.svg';
import '../../../images/icons/file.svg';
import '../../../images/icons/folder.svg';
import '../../../images/icons/menu.svg';

import * as moment from 'moment';
import * as React from 'react';
import { FileDropDirectory, FileDropFile, Guid, PermissionSet } from '../models';
import { ActionIcon } from '../shared-components/action-icon';
import { ButtonSpinner } from '../shared-components/button-spinner';
import { Input, TextAreaInput } from '../shared-components/form/input';
import { PopupMenu } from '../shared-components/popup-menu';
import { Dict } from '../shared-components/redux/store';
import { UploadStatusBar } from '../shared-components/upload-status-bar';
import {
  AfterFormEntityTypes, CreateFolderData, FileAndFolderAttributes,
  FileDropPendingReturnState, FileDropUploadState,
} from './redux/store';

interface FolderContentsProps {
  thisDirectory: FileDropDirectory;
  directories: FileDropDirectory[];
  files: FileDropFile[];
  activeUploads: FileDropUploadState[];
  fileDropName: string;
  fileDropId: Guid;
  fileDropContentAttributes: Dict<FileAndFolderAttributes>;
  currentUserPermissions: PermissionSet;
  createFolder: CreateFolderData;
  browseRef?: React.RefObject<HTMLInputElement>;
  navigateTo: (fileDropId: Guid, canonicalPath: string) => void;
  beginFileDropUploadCancel: (uploadId: string) => void;
  deleteFile: (fileName: string, fileId: Guid) => void;
  deleteFolder: (folderName: string, folderId: Guid) => void;
  expandFileOrFolder: (id: Guid, expanded: boolean) => void;
  editFileDropItem: (id: Guid, editing: boolean, fileName: string, description: string) => void;
  updateFileDropItemName: (id: Guid, name: string) => void;
  updateFileDropItemDescription: (id: Guid, description: string) => void;
  saveFileDropFile: (fileDropId: Guid, fileId: Guid, description: string) => void;
  saveFileDropFolder: (fileDropId: Guid, folderId: Guid, description: string) => void;
  enterCreateFolderMode: () => void;
  exitCreateFolderMode: () => void;
  updateCreateFolderValues: (field: 'name' | 'description', value: string) => void;
  createFileDropFolder: (
    fileDropId: Guid, containingFileDropDirectoryId: Guid, newFolderName: string, description: string) => void;
  renameFileDropFile: (fileDropId: Guid, fileId: Guid, newFolderId: Guid, name: string) => void;
  renameFileDropFolder: (fileDropId: Guid, folderId: Guid, parentCanonicalPath: string, name: string) => void;
  moveFileDropFile: (
    fileDropId: Guid, fileId: Guid, fileDropName: string, canonicalPath: string, fileName: string) => void;
  moveFileDropFolder: (
    fileDropId: Guid, folderId: Guid, fileDropName: string, canonicalPath: string, folderName: string) => void;
  discardChanges: (id: Guid, type: AfterFormEntityTypes) => void;
  async: FileDropPendingReturnState;
}

export class FolderContents extends React.Component<FolderContentsProps> {
  public renderBreadCrumbs() {
    const { fileDropId, fileDropName, navigateTo, thisDirectory } = this.props;
    const pathDivider = '/';
    const breadCrumbObjectArray = `${fileDropName}${thisDirectory.canonicalPath}`.split(pathDivider)
      .map((path, index) => {
        const breadCrumbPath = [
          pathDivider,
          (index !== 0)
            ? thisDirectory.canonicalPath.substr(1).split(pathDivider).slice(0, index).join(pathDivider)
            : '',
        ].join('');
        const breadCrumbText = index === 0 ? fileDropName : path;
        if (path !== '') {
          return {
            breadCrumbPath,
            breadCrumbText,
          };
        }
      });
    if (breadCrumbObjectArray[breadCrumbObjectArray.length - 1] === undefined) {
      breadCrumbObjectArray.pop();
    }
    return breadCrumbObjectArray.map((folder, i) => {
      return (
        <React.Fragment key={`breadcrumb-${i}`}>
          <span
            className={`breadcrumb-link${i + 1 === breadCrumbObjectArray.length ? ' current' : ''}`}
            onClick={
              i + 1 < breadCrumbObjectArray.length
                ? () => navigateTo(fileDropId, encodeURIComponent(folder.breadCrumbPath))
                : null
            }
          >
            {folder.breadCrumbText}
          </span>
          {
            i + 1 < breadCrumbObjectArray.length &&
            <span className="breadcrumb-divider"> {pathDivider} </span>
          }
        </React.Fragment>
      );
    });
  }

  public renderCreateFolder() {
    const { createFolder, directories, fileDropId, thisDirectory, async } = this.props;
    const existingFolderNames = directories.map((directory) => directory.canonicalPath.split('/').slice(-1)[0]);
    return (
      <>
        <tr className="folder-row expanded">
          <td className="folder-icon">
            <svg className="content-type-icon">
              <use xlinkHref={'#folder'} />
            </svg>
          </td>
          <td>
            <Input
              error={existingFolderNames.indexOf(createFolder.name.trim()) > -1 ? 'Folder name already exists' : null}
              label="New Folder Name"
              name="new-folder-input"
              type="text"
              value={createFolder.name}
              autoFocus={true}
              onChange={({ currentTarget: target }: React.FormEvent<HTMLInputElement>) =>
                this.props.updateCreateFolderValues('name', target.value)}
              onSubmitCallback={() =>
                this.props.createFileDropFolder(fileDropId, thisDirectory.id, createFolder.name,
                  createFolder.description)
              }
              usesOnSubmitCallback={true}
            />
          </td>
          <td colSpan={2} />
          <td className="col-actions">
            {!async.createFileDropFolder ?
              <div>
                {
                  createFolder.name.trim().length > 0 &&
                  existingFolderNames.indexOf(createFolder.name.trim()) === -1 &&
                  <ActionIcon
                    label="Create Folder"
                    icon="check-circle"
                    inline={true}
                    action={() =>
                      this.props.createFileDropFolder(
                        fileDropId, thisDirectory.id, createFolder.name, createFolder.description)
                    }
                  />
                }
                <ActionIcon
                  label="Discard Changes"
                  icon="cancel-circle"
                  inline={true}
                  action={() => this.props.exitCreateFolderMode()}
                />
              </div> :
              <div className="spinner">
                <ButtonSpinner version="bars" spinnerColor="black" />
              </div>
            }
          </td>
        </tr>
        <tr>
          <td />
          <td colSpan={4}>
            <TextAreaInput
              error={null}
              label="Description"
              name="description"
              onChange={({ currentTarget: target }: React.FormEvent<HTMLInputElement>) =>
                this.props.updateCreateFolderValues('description', target.value)}
              value={createFolder.description}
              maxRows={3}
            />
          </td>
        </tr>

      </>
    );
  }

  public renderFolders() {
    const { directories, fileDropId, fileDropContentAttributes, navigateTo, fileDropName } = this.props;
    const { canonicalPath: path } = this.props.thisDirectory;
    return directories.map((directory) => {
      const [folderName] = directory.canonicalPath.split('/').slice(-1);
      const folderAttributes = fileDropContentAttributes[directory.id];
      const editing = folderAttributes && folderAttributes.editing ? true : false;
      const expanded = folderAttributes && folderAttributes.expanded ? true : false;
      const rowClass = editing || expanded ? 'expanded' : null;
      return (
        <React.Fragment key={directory.id}>
          <tr className={`folder-row ${rowClass}`}>
            <td className="folder-icon">
              <svg className="content-type-icon">
                <use xlinkHref={'#folder'} />
              </svg>
            </td>
            <td>
              {editing ?
                <Input
                  type="text"
                  name="folderName"
                  label="Folder Name"
                  value={folderAttributes.fileName}
                  onChange={({ currentTarget: target }: React.FormEvent<HTMLInputElement>) =>
                    this.props.updateFileDropItemName(directory.id, target.value)}
                  error={null}
                  onSubmitCallback={() =>
                    this.updateFolder(fileDropId, directory.id, folderAttributes, directory.canonicalPath)
                  }
                  usesOnSubmitCallback={true}
                /> :
                <span
                  className="folder"
                  onClick={() => navigateTo(fileDropId, encodeURIComponent(directory.canonicalPath))}
                >
                  {folderName}
                </span>
              }
            </td>
            <td className="col-file-size" />
            <td className="col-date-modified" />
            <td className="col-actions">
              {
                folderAttributes &&
                !folderAttributes.editing &&
                directory.description &&
                <ActionIcon
                  label="View Details"
                  icon={folderAttributes.expanded ? 'collapse-card' : 'expand-card'}
                  inline={true}
                  action={() => this.props.expandFileOrFolder(directory.id, !folderAttributes.expanded)}
                />
              }
              {
                folderAttributes &&
                folderAttributes.editing &&
                folderAttributes.saving &&
                <div className="spinner">
                  <ButtonSpinner version="bars" spinnerColor="black" />
                </div>
              }
              {
                folderAttributes &&
                folderAttributes.editing &&
                (folderAttributes.description !== folderAttributes.descriptionRaw ||
                  folderAttributes.fileName !== folderAttributes.fileNameRaw) &&
                !folderAttributes.saving &&
                <ActionIcon
                  label="Submit Changes"
                  icon="check-circle"
                  inline={true}
                  action={() =>
                    this.updateFolder(fileDropId, directory.id, folderAttributes, directory.canonicalPath)
                  }
                />
              }
              {
                editing &&
                !folderAttributes.saving &&
                <ActionIcon
                  label="Discard Changes"
                  icon="cancel-circle"
                  inline={true}
                  action={() => {
                    if (folderAttributes.fileName !== folderAttributes.fileNameRaw ||
                      folderAttributes.description !== folderAttributes.descriptionRaw) {
                      this.props.discardChanges(directory.id, 'Edit Folder');
                    } else {
                      this.props.editFileDropItem(directory.id, false, null, null);
                      this.props.expandFileOrFolder(directory.id, false);
                    }
                  }}
                />
              }
              {
                this.props.currentUserPermissions &&
                (this.props.currentUserPermissions.writeAccess ||
                  this.props.currentUserPermissions.deleteAccess) &&
                (folderAttributes && !folderAttributes.editing) &&
                <PopupMenu>
                  <ul>
                    {
                      this.props.currentUserPermissions.writeAccess &&
                      <>
                        <li
                          onClick={() =>
                            this.props.editFileDropItem(directory.id, true, folderName, directory.description)
                          }
                        >
                          <ActionIcon
                            icon="edit"
                            inline={true}
                            label="Edit"
                          />
                          <span className="menu-text">Edit</span>
                        </li>
                        <li
                          onClick={() => {
                            this.props.moveFileDropFolder(fileDropId, directory.id, fileDropName, path, folderName);
                          }}
                        >
                          <ActionIcon
                            icon="move-folder"
                            inline={true}
                            label="Move"
                          />
                          <span className="menu-text">Move</span>
                        </li>
                      </>
                    }
                    {
                      this.props.currentUserPermissions.deleteAccess &&
                      <li
                        className="warning"
                        onClick={() => this.props.deleteFolder(folderName, directory.id)}
                      >
                        <ActionIcon
                          icon="delete"
                          inline={true}
                          label="Edit"
                        />
                        <span className="menu-text">Delete</span>
                      </li>
                    }
                  </ul>
                </PopupMenu>
              }
            </td>
          </tr>
          {
            editing &&
            <>
              <tr>
                <td />
                <td colSpan={4}>
                  <TextAreaInput
                    error=""
                    autoFocus={true}
                    label="Description"
                    name="description"
                    onChange={({ currentTarget: target }: React.FormEvent<HTMLInputElement>) =>
                      this.props.updateFileDropItemDescription(directory.id, target.value)}
                    value={folderAttributes.description}
                    maxRows={3}
                  />
                </td>
              </tr>
            </>
          }
          {
            !editing &&
            expanded &&
            <>
              <tr>
                <td colSpan={5}>
                  <div className="file-drop-content-description">
                    {directory.description}
                  </div>
                </td>
              </tr>
            </>
          }
        </React.Fragment>
      );
    });
  }

  public renderFiles() {
    const { files, fileDropId, activeUploads, fileDropContentAttributes, thisDirectory, fileDropName } = this.props;
    const { canonicalPath: path } = this.props.thisDirectory;
    const baseAllFilesArray: Array<FileDropFile | FileDropUploadState> = [];
    const baseArrayWithFiles = baseAllFilesArray.concat(files);
    const baseArrayWithAllFiles = baseArrayWithFiles.concat(activeUploads);
    const sortedAllFiles = baseArrayWithAllFiles.sort((a, b) => {
      const fileA = a.fileName.toUpperCase();
      const fileB = b.fileName.toUpperCase();
      let comparison = 0;
      if (fileA > fileB) {
        comparison = 1;
      } else if (fileA < fileB) {
        comparison = -1;
      }
      return comparison;
    });

    const isFile = (file: any): file is FileDropFile => file.id !== undefined;

    return sortedAllFiles.map((file) => {
      if (isFile(file)) {
        const fileDownloadURL = [
          './FileDrop/DownloadFile?',
          `FileDropId=${fileDropId}&`,
          `FileDropFileId=${file.id}&`,
          `CanonicalFilePath=${path}${path[path.length - 1] === '/' ? '' : '/'}${file.fileName}`,
        ].join('');
        const fileAttributes = fileDropContentAttributes[file.id];
        const editing = fileAttributes && fileAttributes.editing ? true : false;
        const expanded = fileAttributes && fileAttributes.expanded ? true : false;
        const rowClass = editing || expanded ? 'expanded' : null;
        return (
          <React.Fragment key={file.id}>
            <tr className={rowClass}>
              <td className="file-icon">
                <svg className="content-type-icon">
                  <use xlinkHref={'#file'} />
                </svg>
              </td>
              <td>
                {editing ?
                  <div className="file-rename-container">
                    <div className="file-rename-input">
                      <Input
                        type="text"
                        name="fileName"
                        label="File Name"
                        value={this.getFileNameSansExtension(fileAttributes.fileName)}
                        onChange={({ currentTarget: target }: React.FormEvent<HTMLInputElement>) =>
                          this.props.updateFileDropItemName(file.id,
                            target.value.concat(this.getFileExtension(fileAttributes.fileName)))}
                        error={null}
                        onSubmitCallback={() => this.updateFile(fileDropId, file.id, fileAttributes, thisDirectory.id)}
                        usesOnSubmitCallback={true}
                      />
                    </div>
                    <span className="file-rename-extension">{this.getFileExtension(fileAttributes.fileName)}</span>
                  </div> :
                  (this.props.currentUserPermissions && this.props.currentUserPermissions.readAccess) ? (
                    <a
                      href={encodeURI(fileDownloadURL)}
                      download={true}
                      className="file-download"
                      title={file.description ? file.description : null}
                    >
                      {file.fileName}
                    </a>
                  ) : (
                    <span>{file.fileName}</span>
                  )
                }
              </td>
              <td className="col-file-size">{file.size}</td>
              <td
                className="col-date-modified"
                title={
                  file.uploadDateTimeUtc
                    ? moment(file.uploadDateTimeUtc).local().format('MM/DD/YYYY h:mm:ss A')
                    : null
                }
              >
                {
                  file.uploadDateTimeUtc
                    ? moment(file.uploadDateTimeUtc).local().format('MM/DD/YYYY')
                    : null
                }
              </td>
              <td className="col-actions">
                {
                  fileAttributes &&
                  !fileAttributes.editing &&
                  file.description &&
                  <ActionIcon
                    label="View Details"
                    icon={fileAttributes.expanded ? 'collapse-card' : 'expand-card'}
                    inline={true}
                    action={() => this.props.expandFileOrFolder(file.id, !fileAttributes.expanded)}
                  />
                }
                {
                  fileAttributes &&
                  fileAttributes.editing &&
                  fileAttributes.saving &&
                  <div className="spinner">
                    <ButtonSpinner version="bars" spinnerColor="black" />
                  </div>
                }
                {
                  fileAttributes &&
                  fileAttributes.editing &&
                  (fileAttributes.description !== fileAttributes.descriptionRaw ||
                   fileAttributes.fileName !== fileAttributes.fileNameRaw) &&
                  !fileAttributes.saving &&
                  <ActionIcon
                    label="Submit Changes"
                    icon="check-circle"
                    inline={true}
                    action={() =>
                      this.updateFile(fileDropId, file.id, fileAttributes, thisDirectory.id)
                    }
                  />
                }
                {
                  editing &&
                  !fileAttributes.saving &&
                  <ActionIcon
                    label="Discard Changes"
                    icon="cancel-circle"
                    inline={true}
                    action={() => {
                      if (fileAttributes.fileName !== fileAttributes.fileNameRaw ||
                        fileAttributes.description !== fileAttributes.descriptionRaw) {
                        this.props.discardChanges(file.id, 'Edit File');
                      } else {
                        this.props.editFileDropItem(file.id, false, null, null);
                        this.props.expandFileOrFolder(file.id, false);
                      }
                    }}
                  />
                }
                {
                  this.props.currentUserPermissions &&
                  (this.props.currentUserPermissions.readAccess ||
                   this.props.currentUserPermissions.writeAccess ||
                   this.props.currentUserPermissions.deleteAccess) &&
                  !fileAttributes.editing &&
                  <PopupMenu>
                    <ul>
                      {
                        this.props.currentUserPermissions.readAccess &&
                        <li>
                          <a
                            href={encodeURI(fileDownloadURL)}
                            download={true}
                          >
                            <ActionIcon
                              icon="download"
                              inline={true}
                              label="Download File"
                            />
                            <span className="menu-text">Download</span>
                          </a>
                        </li>
                    }
                      {
                        this.props.currentUserPermissions.writeAccess &&
                        <>
                          <li
                            onClick={() =>
                              this.props.editFileDropItem(file.id, true, file.fileName, file.description)
                            }
                          >
                            <ActionIcon
                              icon="edit"
                              inline={true}
                              label="Edit"
                            />
                            <span className="menu-text">Edit</span>
                          </li>
                          <li
                            onClick={() =>
                              this.props.moveFileDropFile(fileDropId, file.id, fileDropName, path, file.fileName)
                            }
                          >
                            <ActionIcon
                              icon="move-file"
                              inline={true}
                              label="Move File"
                            />
                            <span className="menu-text">Move</span>
                          </li>
                        </>
                      }
                      {
                        this.props.currentUserPermissions.deleteAccess &&
                        <li
                          className="warning"
                          onClick={() => this.props.deleteFile(file.fileName, file.id)}
                        >
                          <ActionIcon
                            icon="delete"
                            inline={true}
                            label="Delete File"
                          />
                          <span className="menu-text">Delete</span>
                        </li>
                      }
                    </ul>
                  </PopupMenu>
                }
              </td>
            </tr >
            {
              editing &&
              <>
                <tr>
                  <td colSpan={5}>
                    <TextAreaInput
                      error=""
                      autoFocus={true}
                      label="Description"
                      name="description"
                      onChange={({ currentTarget: target }: React.FormEvent<HTMLInputElement>) =>
                        this.props.updateFileDropItemDescription(file.id, target.value)}
                      value={fileAttributes.description}
                      maxRows={5}
                    />
                  </td>
                </tr>
              </>
            }
            {
              !editing &&
              expanded &&
              <>
                <tr>
                  <td colSpan={5}>
                    <div className="file-drop-content-description">
                      {file.description}
                    </div>
                  </td>
                </tr>
              </>
            }
          </React.Fragment>
        );
      } else {
        return (
          <tr key={file.uploadId}>
            <td className="file-icon">
              <svg className="content-type-icon">
                <use xlinkHref={'#file'} />
              </svg>
            </td>
            <td colSpan={4}>
              <div className="file-upload-row">
                <span className="file-name">{file.fileName}</span>
                {
                  file.cancelable &&
                  !file.errorMsg &&
                  <ButtonSpinner version="circle" spinnerColor="black" />
                }
                <ActionIcon
                  icon="cancel-circle"
                  disabled={!file.cancelable}
                  label="Cancel Upload"
                  action={() => this.props.beginFileDropUploadCancel(file.uploadId)}
                />
              </div>
              <div>
                <UploadStatusBar
                  checksumProgress={file.checksumProgress}
                  uploadProgress={file.uploadProgress}
                  errorMsg={file.errorMsg}
                />
              </div>
            </td>
          </tr>
        );
      }
    });
  }

  public renderAddButtons() {
    return (
      <>
        {
          this.props.browseRef &&
          <tr className="add-row" onClick={() => this.props.browseRef.current.click()}>
            <td className="file-icon">
              <svg className="content-type-icon">
                <use xlinkHref={'#add-file'} />
              </svg>
            </td>
            <td colSpan={4}>
              Add File
            </td>
          </tr>
        }
        <tr className="add-row" onClick={() => this.props.enterCreateFolderMode()}>
          <td className="folder-icon">
            <svg className="content-type-icon">
              <use xlinkHref={'#add-folder'} />
            </svg>
          </td>
          <td colSpan={4}>
            Add Folder
          </td>
        </tr>
      </>
    );
  }

  public render() {
    const { createFolder } = this.props;

    return (
      <div>
        <div className="breadcrumb-container">
          {this.renderBreadCrumbs()}
        </div>
        <table className="folder-content-table">
          <thead>
            <tr>
              <th className="col-name" colSpan={2}>Name</th>
              <th className="col-file-size">Size</th>
              <th className="col-date-modified">Date Modified</th>
              <th className="col-actions">Actions</th>
            </tr>
          </thead>
          <tbody>
            {
              createFolder &&
              this.renderCreateFolder()
            }
            {this.renderFolders()}
            {this.renderFiles()}
            {
              this.props.currentUserPermissions &&
              this.props.currentUserPermissions.writeAccess &&
              this.renderAddButtons()
            }
          </tbody>
        </table>
      </div>
    );
  }

  private getFileNameSansExtension(fileName: string) {
    return fileName.lastIndexOf('.') > -1 ? fileName.slice(0, fileName.lastIndexOf('.')) : fileName;
  }

  private getFileExtension(fileName: string) {
    return fileName.lastIndexOf('.') > -1 ? fileName.slice(fileName.lastIndexOf('.')) : '';
  }

  private updateFolder(fileDropId: Guid, folderId: Guid, folderAttributes: FileAndFolderAttributes,
                       canonicalPath: string) {
    if (folderAttributes.description !== folderAttributes.descriptionRaw) {
      this.props.saveFileDropFolder(fileDropId, folderId, folderAttributes.description);
    }
    if (folderAttributes.fileName !== folderAttributes.fileNameRaw) {
      const parentCanonicalPath = canonicalPath.slice().substr(0, canonicalPath.lastIndexOf('/') + 1);
      this.props.renameFileDropFolder(fileDropId, folderId, parentCanonicalPath, folderAttributes.fileName);
    }
  }

  private updateFile(fileDropId: Guid, fileId: Guid, fileAttributes: FileAndFolderAttributes,
                     currentDirectoryId: Guid) {
    if (fileAttributes.description !== fileAttributes.descriptionRaw) {
      this.props.saveFileDropFile(fileDropId, fileId, fileAttributes.description);
    }
    if (fileAttributes.fileName !== fileAttributes.fileNameRaw) {
      this.props.renameFileDropFile(fileDropId, fileId, currentDirectoryId, fileAttributes.fileName);
    }
  }
}
