import '../../../../scss/react/shared-components/form-elements.scss';

import '../../../../images/icons/cancel.svg';
import '../../../../images/icons/checkmark.svg';
import '../../../../images/icons/delete.svg';
import '../../../../images/icons/upload.svg';

import * as React from 'react';
import { resumableOptions } from '../../../lib-options';
import { StatusMonitor } from '../../../status-monitor';
import { FileScanner } from '../../../upload/file-scanner';
import { FileSniffer } from '../../../upload/file-sniffer';
import { ProgressMonitor, ProgressSummary } from '../../../upload/progress-monitor';
import { UploadState } from '../../../upload/Redux/store';

import forge = require('node-forge');
import { Guid } from '../../models';
const resumable = require('resumablejs');

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
  fileExtensions: string[];
  label: string;
  name: string;
  placeholderText?: string;
  readOnly?: boolean;
  uploadId: string;
  fileUploadId: Guid;
  upload: UploadState;
  value: string;
  imageURL?: string;
  beginUpload: (uploadId: string, fileName: string) => void;
  cancelFileUpload: (uploadId: string) => void;
  finalizeUpload: (uploadId: string, fileName: string, Guid: string) => void;
  setUploadError: (uploadId: string, errorMsg: string) => void;
  updateChecksumProgress: (uploadId: string, progress: ProgressSummary) => void;
  updateUploadProgress: (uploadId: string, progress: ProgressSummary) => void;
}

interface FileUploadInputState {
  imageSrc: string;
}

export class FileUploadInput extends React.Component<FileUploadInputProps, FileUploadInputState> {
  protected checksum: string;
  protected progressMonitor: ProgressMonitor;
  protected resumable: Resumable.Resumable;
  protected resumableFormData: { Checksum: string };
  protected resumableHeaders: { RequestVerificationToken: string } = {
    RequestVerificationToken: (document.getElementsByName('__RequestVerificationToken')[0] as HTMLInputElement).value,
  };
  protected scanner: FileScanner;
  protected statusMonitor: StatusMonitor<any>;
  protected uploadRef: React.RefObject<HTMLInputElement>;

  constructor(props: FileUploadInputProps) {
    super(props);
    this.uploadRef = React.createRef();
    this.state = {
      imageSrc: null,
    };
  }

  public componentDidUpdate(prevProps: FileUploadInputProps) {
    const resumableInput: HTMLInputElement = this.uploadRef.current.querySelector('input[type="file"]');
    if (this.props.fileExtensions.toString() !== prevProps.fileExtensions.toString()) {
      this.resumable.opts.fileType = this.props.fileExtensions;
      const newFileExtensions = this.props.fileExtensions.map((ext) => `.${ext}`).join(',');
      resumableInput.setAttribute('accept', newFileExtensions);
    }
    resumableInput.disabled = this.props.readOnly;
    // resumableInput.setAttribute('disabled', `${this.props.readOnly}`);
  }

  public setupResumable() {
    // Instantiate the resumable object and configure the upload chunks
    this.resumable = new resumable(Object.assign({}, resumableOptions, {
      generateUniqueIdentifier: (_: File, __: Event) => this.props.uploadId,
      fileType: this.props.fileExtensions,
      fileTypeErrorCallback: () =>
        this.props.setUploadError(this.props.uploadId, 'File contents do not match extension.'),
      headers: () => this.resumableHeaders,
      query: () => this.resumableFormData,
      target: '/FileUpload/UploadChunk',
    }));

    // Hook up the file upload input
    this.resumable.assignBrowse(this.uploadRef.current, false);
    this.resumable.assignDrop(this.uploadRef.current);

    // Define the process after a file is selected
    this.resumable.on('fileAdded', async (resumableFile: Resumable.ResumableFile) => {
      const file: File = resumableFile.file;

      // Ensure that the uploaded file type matches the expected
      if (this.props.fileExtensions.length > 0) {
        const fileParts: string[] = file.name.toLowerCase().split('.');
        const fileExtension: string = fileParts[fileParts.length - 1];
        if (this.props.fileExtensions.indexOf(fileExtension) === -1) {
          this.props.setUploadError(this.props.uploadId, 'File extension not supported.');
          return false;
        }
      }

      if (this.props.name === 'thumbnail') {
        const reader = new FileReader();
        reader.onload = () => {
          this.setState({ imageSrc: reader.result.toString() });
        };
        reader.readAsDataURL(file);
      }

      // Make sure the file matches the expected magic numbers
      const sniffer = new FileSniffer(file);
      if (!await sniffer.extensionMatchesInitialBytes()) {
        this.props.setUploadError(this.props.uploadId, 'File contents do not match extension.');
        return false;
      }

      // Send the filename to the Redux store
      this.props.beginUpload(this.props.uploadId, file.name);

      // Begin the process of creating a checksum and monitoring the progress
      const messageDigest = forge.md.sha1.create();
      this.scanner = new FileScanner();
      this.scanner.open(file);
      this.progressMonitor = new ProgressMonitor(
        () => this.scanner.progress,
        this.props.updateChecksumProgress,
        this.props.uploadId,
        file.size,
      );
      this.progressMonitor.activate();

      // Wait until the scan process is finished then update the checksum
      try {
        await this.scanner.scan(messageDigest.update);
      } catch {
        // Upload was canceled
        return;
      }
      this.checksum = messageDigest.digest().toHex();
      this.resumableFormData = { Checksum: this.checksum };

      // Start the monitor for the upload chunking progress
      this.progressMonitor = new ProgressMonitor(
        () => this.resumable.progress(),
        this.props.updateUploadProgress,
        this.props.uploadId,
        file.size,
      );
      this.progressMonitor.activate();

      // Begin the upload process
      this.resumable.upload();
    });

    // Define the process after a file is successfully uploaded
    this.resumable.on('fileSuccess', (resumableFile: Resumable.ResumableFile) => {
      // Make sure the upload can't be canceled any more and set the upload progress to 100%
      this.props.updateUploadProgress(this.props.uploadId, ProgressSummary.full());

      // Define the information that needs to be sent with the Finalize Upload POST request
      const finalizeInfo: ResumableInfo = {
        Checksum: this.checksum,
        ChunkNumber: 0,
        ChunkSize: this.resumable.opts.chunkSize,
        FileName: resumableFile.fileName,
        TotalChunks: resumableFile.chunks.length,
        TotalSize: resumableFile.size,
        Type: '',
        UID: resumableFile.uniqueIdentifier,
      };

      // Make the FinalizeUpload POST request to begin the backend process of assembling the file
      fetch('FileUpload/FinalizeUpload', {
        method: 'POST',
        headers: Object.assign({}, this.resumableHeaders, {
          'Accept': 'application/json',
          'Content-Type': 'application/json',
        }),
        body: JSON.stringify(finalizeInfo),
      })
        .then((response) => response.json())
        .then((fileGUID: string) => {
          // Start a monitor that polls for status of asynchronous backend data finalization
          this.statusMonitor = new StatusMonitor<{}>(
            `/FileUpload/FinalizeUpload?fileUploadId=${fileGUID}`,
            (fileUpload: FileUpload) => {
              if (fileUpload.status === FileUploadStatus.Complete) {
                this.progressMonitor.deactivate();
                this.props.finalizeUpload(this.props.uploadId, resumableFile.fileName, fileGUID);
                this.statusMonitor.stop();
              } else if (fileUpload.status === FileUploadStatus.Error) {
                this.props.setUploadError(
                  this.props.uploadId,
                  fileUpload.statusMessage || 'Something went wrong during upload. Please try again.',
                );
                this.statusMonitor.stop();
              }
            });
          setTimeout(() => this.statusMonitor.start(), 1000);
        })
        .catch((response) => {
          // Pass back the error message if something failed
          this.props.setUploadError(
            this.props.uploadId,
            response.getResponseHeader('Warning') || 'Something went wrong during upload. Please try again.',
          );
        });
    });

    // Define the process if an upload is canceled
    this.resumable.on('beforeCancel', () => {
      // Define the information that needs to be sent with the Finalize Upload POST request
      this.resumable.files.forEach((file: any) => {
        const cancelInfo: ResumableInfo = {
          Checksum: this.checksum,
          ChunkNumber: 0,
          ChunkSize: this.resumable.opts.chunkSize,
          FileName: file.fileName,
          TotalChunks: -1,
          TotalSize: file.size,
          Type: '',
          UID: file.uniqueIdentifier,
        };

        // Make the CancelUpload POST request to cancel the upload process
        fetch('FileUpload/CancelUpload', {
          method: 'POST',
          headers: this.resumableHeaders,
          body: JSON.stringify(cancelInfo),
        })
          .then(() => {
            this.props.cancelFileUpload(this.props.uploadId);
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

  public componentDidMount() {
    this.setupResumable();
    const resumableInput: HTMLInputElement = this.uploadRef.current.querySelector('input[type="file"]');
    resumableInput.disabled = this.props.readOnly;
  }

  public componentWillReceiveProps(nextProps: FileUploadInputProps) {
    if (nextProps.value === '') {
      this.setState({ imageSrc: null });
    }
  }

  public render() {
    const { label, name, placeholderText, readOnly, upload, value, children } = this.props;
    const { checksumProgress, uploadProgress, cancelable, errorMsg } = upload;
    const hasImage = (value.length > 0 && this.props.imageURL || this.state.imageSrc);
    const checksumEasing =
      (checksumProgress.percentage === '0%' || checksumProgress.percentage === '100%') ? '' : ' progress-easing';
    const uploadEasing =
      (uploadProgress.percentage === '0%' || uploadProgress.percentage === '100%') ? '' : ' progress-easing';
    return (
      <div
        className={`form-element-container${readOnly ? ' disabled' : ''}${hasImage ? ' thumbnail' : ''}`}
        title={value}
      >
        <div className={`form-element-input ${errorMsg ? ' error' : ''}`} ref={this.uploadRef}>
          <div className="form-input-container">
            <input
              type="text"
              className={`form-input file-upload-input ${hasImage ? 'preview' : ''}`}
              name={name}
              id={name}
              placeholder={placeholderText || 'Upload ' + label}
              value={value}
              onChange={() => false}
              readOnly={readOnly}
            />
            <label className="form-input-label" htmlFor={name}>{label}</label>
          </div>
          {
            hasImage && value &&
            <img
              className="thumbnail-preview"
              src={this.state.imageSrc || this.props.imageURL}
              alt="thumbnail preview"
            />
          }
          {children}
        </div>
        {
          upload.cancelable &&
          <div className="progress-bars">
            {!errorMsg &&
              <div
                className={`progress-bar-checksum${checksumEasing}`}
                style={{ width: checksumProgress.percentage }}
              />}
            {!errorMsg &&
              <div
                className={`progress-bar-upload${uploadEasing}`}
                style={{ width: uploadProgress.percentage }}
              />}
            {errorMsg &&
              <div
                className="progress-bar-error progress-easing"
              />}
          </div>
        }
        {errorMsg && <div className="error-message">{errorMsg}</div>}
        {
          !readOnly &&
          <div className="upload-icon-container">
            {
              !cancelable &&
              <div
                className="upload-icon tooltip"
                title="Upload file"
                onClick={() => this.uploadRef.current.click()}
              >
                <svg className="upload icon green">
                  <use xlinkHref="#upload" />
                </svg>
              </div>
            }
            {
              cancelable &&
              <div
                className="upload-icon tooltip"
                title="Cancel upload"
                onClick={(event: React.MouseEvent) => {
                  event.stopPropagation();
                  alert('Cancel!');
                }}
              >
                <svg className="icon red">
                  <use xlinkHref="#cancel" />
                </svg>
              </div>
            }
            {
              this.props.fileUploadId && this.props.fileUploadId.length > 0 &&
              <div
                className="upload-icon tooltip"
                title="Upload Complete"
              >
                <svg className="icon green">
                  <use xlinkHref="#checkmark" />
                </svg>
              </div>
            }
            {
              value.length > 0 && !cancelable &&
              <div
                className="upload-icon tooltip"
                title="Delete existing file"
                onClick={(event: React.MouseEvent) => {
                  event.stopPropagation();
                  alert('Delete!');
                }}
              >
                <svg className="icon red">
                  <use xlinkHref="#delete" />
                </svg>
              </div>
            }
          </div>
        }
      </div>
    );
  }
}
