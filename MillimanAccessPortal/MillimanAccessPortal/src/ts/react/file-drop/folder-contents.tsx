import '../../../scss/react/file-drop/folder-contents.scss';

import '../../../images/icons/file.svg';
import '../../../images/icons/folder.svg';
import '../../../images/icons/menu.svg';

import * as moment from 'moment';
import * as React from 'react';
import { FileDropDirectory, FileDropFile, Guid } from '../models';
import { ActionIcon } from '../shared-components/action-icon';
import { UploadStatusBar } from '../shared-components/upload-status-bar';
import { FileDropUploadState } from './redux/store';

interface FolderContentsProps {
  thisDirectory: FileDropDirectory;
  directories: FileDropDirectory[];
  files: FileDropFile[];
  activeUploads: FileDropUploadState[];
  fileDropName: string;
  fileDropId: Guid;
  navigateTo: (fileDropId: Guid, canonicalPath: string) => void;
  beginFileDropUploadCancel: (uploadId: string) => void;
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
                ? () => navigateTo(fileDropId, folder.breadCrumbPath)
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

  public renderFolders() {
    const { directories, fileDropId, navigateTo } = this.props;
    return directories.map((directory) => {
      const [folderName] = directory.canonicalPath.split('/').slice(-1);
      return (
        <tr className="folder-row" key={directory.id}>
          <td className="folder-icon">
            <svg className="content-type-icon">
              <use xlinkHref={'#folder'} />
            </svg>
          </td>
          <td>
            <span
              className="folder"
              onClick={() => navigateTo(fileDropId, directory.canonicalPath)}
            >
              {folderName}
            </span>
          </td>
          <td className="col-file-size" />
          <td className="col-date-modified" />
          <td className="col-actions">
            <svg className="menu-icon">
              <use xlinkHref={'#menu'} />
            </svg>
          </td>
        </tr>
      );
    });
  }

  public renderFiles() {
    const { files, fileDropId, activeUploads } = this.props;
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
        return (
          <tr key={file.id}>
            <td className="file-icon">
              <svg className="content-type-icon">
                <use xlinkHref={'#file'} />
              </svg>
            </td>
            <td>
              <a
                href={encodeURI(fileDownloadURL)}
                download={true}
                className="file-download"
              >
                {file.fileName}
              </a>
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
              <svg className="menu-icon">
                <use xlinkHref={'#menu'} />
              </svg>
            </td>
          </tr >
        );
      } else {
        return (
          <tr key={file.fileName}>
            <td className="file-icon">
              <svg className="content-type-icon">
                <use xlinkHref={'#file'} />
              </svg>
            </td>
            <td colSpan={4}>
              <div className="file-upload-row">
                {file.fileName}
                <ActionIcon
                  icon={'cancel'}
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

  public render() {

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
            {this.renderFolders()}
            {this.renderFiles()}
          </tbody>
        </table>
      </div>
    );
  }
}
