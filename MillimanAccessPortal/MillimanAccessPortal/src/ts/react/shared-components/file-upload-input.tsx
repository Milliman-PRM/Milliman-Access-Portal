import '../../../scss/react/shared-components/form-elements.scss';

import * as React from 'react';
import { resumableOptions } from '../../lib-options';
import { StatusMonitor } from '../../status-monitor';
import { FileScanner } from '../../upload/file-scanner';
import { FileSniffer } from '../../upload/file-sniffer';
import { ProgressMonitor, ProgressSummary } from '../../upload/progress-monitor';

import forge = require('node-forge');
const resumable = require('resumablejs');
import Resumable = require('resumablejs');
import { randomBytes } from 'crypto';

export enum FileUploadStatus {
  InProgress = 0,
  Complete = 1,
  Error = 2,
}

interface ResumableInfo {
  ChunkNumber: number;
  TotalChunks: number;
  ChunkSize: number;
  TotalSize: number;
  FileName: string;
  UID: string;
  Checksum: string;
  Type: string;
}

interface FileUpload {
  id: string;
  initiatedDateTimeUtc: string;
  clientFileIdentifier: string;
  status: FileUploadStatus;
  statusMessage: string;
}

interface FileUploadInputProps {
  associatedContentIndex: number;
  name: string;
  label: string;
  value: string;
  purpose: 'masterContent' | 'thumbnail' | 'userGuide' | 'releaseNotes' | 'associatedContent';
  onClick?: (currentTarget: React.FormEvent<HTMLInputElement> | null) => void;
  error: string;
  placeholderText?: string;
  readOnly?: boolean;
  hidden?: boolean;
  cancelable: boolean;
  checksum: string;
  checksumProgress: ProgressSummary;
  uploadProgress: ProgressSummary;
  uploadId: string;
  setChecksum: (uploadId: string, checksum: string) => void;
  setCancelable: (uploadId: string, cancelable: boolean) => void;
  setUploadError: (uploadId: string, errorMsg: string) => void;
  token?: string;
  updateChecksumProgress: (uploadId: string, progress: ProgressSummary) => void;
  updateUploadProgress: (uploadId: string, progress: ProgressSummary) => void;
}

export class FileUploadInput extends React.Component<FileUploadInputProps, {}> {
  protected uploadRef: React.RefObject<HTMLInputElement>;

  protected resumable: Resumable.Resumable;

  protected resumableFormData: { Checksum: string };

  protected resumableHeaders: { RequestVerificationToken: string };

  protected scanner: FileScanner;

  protected progressMonitor: ProgressMonitor;

  protected statusMonitor: StatusMonitor<any>;

  protected uniqueId: string;

  protected generateUniqueId() {
    this.uniqueId = `publication-${this.props.purpose}-${randomBytes(8).toString('hex')}`;
  }

  constructor(props: FileUploadInputProps) {
    super(props);
    this.uploadRef = React.createRef();
  }

  public componentDidMount() {
    this.generateUniqueId();
    this.resumableHeaders = {
      RequestVerificationToken: (document.getElementsByName('__RequestVerificationToken')[0] as HTMLInputElement).value,
    };
    this.resumable = new resumable(Object.assign({}, resumableOptions, {
      generateUniqueIdentifier: () => {
        return this.uniqueId;
      },
      headers: () => this.resumableHeaders,
      query: () => this.resumableFormData,
      target: '/FileUpload/UploadChunk',
    }));
    // Hook up the file upload input
    this.resumable.assignBrowse(this.uploadRef.current, false);
    this.resumable.assignDrop(this.uploadRef.current);
    this.resumable.on('fileAdded', async (resumableFile: Resumable.ResumableFile) => {
      const file: File = resumableFile.file;

      // Make sure the file matches the expected magic numbers
      const sniffer = new FileSniffer(file);
      if (!await sniffer.extensionMatchesInitialBytes()) {
        this.props.setUploadError(this.props.uploadId, 'File contents do not match extension.');
        return false;
      }

      const messageDigest = forge.md.sha1.create();
      this.scanner.open(file);
      this.progressMonitor = new ProgressMonitor(
        () => this.scanner.progress,
        this.props.updateChecksumProgress,
        this.props.uploadId,
        file.size,
      );
      this.progressMonitor.activate();

      try {
        await this.scanner.scan(messageDigest.update);
      } catch {
        // Upload was canceled
        return;
      }
      this.props.setChecksum(this.props.uploadId, messageDigest.digest().toHex());

      this.progressMonitor = new ProgressMonitor(
        () => this.resumable.progress(),
        this.props.updateUploadProgress,
        this.props.uploadId,
        file.size,
      );
      this.progressMonitor.activate();

      this.resumable.upload();
    });

    this.resumable.on('fileSuccess', (resumableFile: Resumable.ResumableFile) => {
      this.props.setCancelable(this.props.uploadId, false);
      this.props.updateUploadProgress(this.props.uploadId, ProgressSummary.full());
      const finalizeInfo: ResumableInfo = {
        Checksum: this.props.checksum,
        ChunkNumber: 0,
        ChunkSize: this.resumable.opts.chunkSize,
        FileName: resumableFile.fileName,
        TotalChunks: resumableFile.chunks.length,
        TotalSize: resumableFile.size,
        Type: '',
        UID: resumableFile.uniqueIdentifier,
      };
      fetch('FileUpload/FinalizeUpload', {
        method: 'POST',
        headers: this.resumableHeaders,
        body: JSON.stringify(finalizeInfo),
      })
        .then((response) => response.json())
        .then((response: { uploadId: string }) => {
          // Begin polling for status of asynchronous data finalization
          this.statusMonitor = new StatusMonitor<{}>(
            `/FileUpload/FinalizeUpload?fileUploadId=${response.uploadId}`,
            (fileUpload: FileUpload) => {
              if (fileUpload.status === FileUploadStatus.Complete) {
                this.progressMonitor.deactivate();
                this.props.updateUploadProgress(this.props.uploadId, ProgressSummary.full());
                // this.setFileGUID(uploadId);
                // this.onFileSuccess(this.fileGUID);
                this.props.setChecksum(this.props.uploadId, null);
                this.statusMonitor.stop();
              } else if (fileUpload.status === FileUploadStatus.Error) {
                this.props.setCancelable(this.props.uploadId, true);
                this.props.setUploadError(
                  this.props.uploadId,
                  fileUpload.statusMessage || 'Something went wrong during upload. Please try again.',
                );
                this.props.setChecksum(this.props.uploadId, null);
                this.statusMonitor.stop();
              }
            });
          setTimeout(() => this.statusMonitor.start(), 1000);
        }).catch((response) => {
          this.props.setCancelable(this.props.uploadId, true);
          this.props.setUploadError(
            this.props.uploadId,
            response.getResponseHeader('Warning') || 'Something went wrong during upload. Please try again.',
          );
          this.props.setChecksum(this.props.uploadId, null);
        });
    });

    this.resumable.on('beforeCancel', () => {
      this.resumable.files.forEach((file: any) => {
        const cancelInfo: ResumableInfo = {
          Checksum: this.props.checksum,
          ChunkNumber: 0,
          ChunkSize: this.resumable.opts.chunkSize,
          FileName: file.fileName,
          TotalChunks: -1,
          TotalSize: file.size,
          Type: '',
          UID: file.uniqueIdentifier,
        };
        fetch('FileUpload/CancelUpload', {
          method: 'POST',
          headers: this.resumableHeaders,
          body: JSON.stringify(cancelInfo),
        })
          .then(() => {
            this.props.setChecksum(this.props.uploadId, null);
          })
          .catch((response) => {
            this.props.setUploadError(
              this.props.uploadId,
              response.getResponseHeader('Warning') || 'Something went wrong during upload. Please try again.',
            );
          });
      });
      if (this.statusMonitor) {
        this.statusMonitor.stop();
      }
    });

    this.resumable.on('error', () => {
      this.props.setUploadError(this.props.uploadId, 'An error occurred during upload.');
    });
  }

  public render() {
    const { name, label, error, placeholderText, children, readOnly, hidden, ...rest } = this.props;
    return (
      <div className={`form-element-container ${readOnly ? ' disabled' : ''}${hidden ? ' hidden' : ''}`}>
        <div className={`form-element-input ${error ? ' error' : ''}`}>
          <div className="form-input-container">
            <input
              ref={this.uploadRef}
              type="text"
              className="form-input"
              name={name}
              id={name}
              placeholder={placeholderText || 'Upload ' + label}
              readOnly={readOnly}
              {...rest}
            />
            <label className="form-input-label" htmlFor={name}>{label}</label>
          </div>
          {children}
        </div>
        {error && <div className="error-message">{error}</div>}
      </div>
    );
  }
}
