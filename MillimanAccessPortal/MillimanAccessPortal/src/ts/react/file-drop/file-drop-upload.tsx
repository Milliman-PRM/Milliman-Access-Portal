import forge = require('node-forge');
import * as React from 'react';

import { resumableOptions } from '../../lib-options';
import { StatusMonitor } from '../../status-monitor';
import { FileScanner } from '../../upload/file-scanner';
import { FileSniffer } from '../../upload/file-sniffer';
import { ProgressMonitor, ProgressSummary } from '../../upload/progress-monitor';
import { FileUpload, FileUploadStatus, Guid, ResumableInfo } from '../models';

const resumable = require('resumablejs');

interface FileDropUploadProps {
  uploadId: string;
  clientId: Guid;
  fileDropId: Guid;
  folderId: Guid;
  cancelable: boolean;
  canceled: boolean;
  dragRef?: React.RefObject<HTMLElement>;
  browseRef?: Array<React.RefObject<HTMLElement>>;
  beginUpload: (uploadId: string, clientId: Guid, fileDropId: Guid, folderId: Guid, fileName: string) => void;
  cancelFileUpload: (uploadId: string) => void;
  finalizeUpload: (uploadId: string, fileName: string, Guid: string) => void;
  setUploadError: (uploadId: string, errorMsg: string) => void;
  updateChecksumProgress: (uploadId: string, progress: ProgressSummary) => void;
  updateUploadProgress: (uploadId: string, progress: ProgressSummary) => void;
}

export class FileDropUpload extends React.Component<FileDropUploadProps, {}> {
  protected canceled = false;
  protected checksum: string;
  protected progressMonitor: ProgressMonitor;
  protected resumable: Resumable.Resumable;
  protected resumableFormData: { Checksum: string };
  protected resumableHeaders: { RequestVerificationToken: string } = {
    RequestVerificationToken: (document.getElementsByName('__RequestVerificationToken')[0] as HTMLInputElement).value,
  };
  protected scanner: FileScanner;
  protected statusMonitor: StatusMonitor<any>;

  constructor(props: FileDropUploadProps) {
    super(props);
  }

  public setupResumable() {
    // Instantiate the resumable object and configure the upload chunks
    this.resumable = new resumable(Object.assign({}, resumableOptions, {
      generateUniqueIdentifier: (_: File, __: Event) => this.props.uploadId,
      fileTypeErrorCallback: () =>
        this.props.setUploadError(this.props.uploadId, 'File contents do not match extension.'),
      headers: () => this.resumableHeaders,
      query: () => this.resumableFormData,
      target: '/FileUpload/UploadChunk',
    }));

    // Hook up the file upload input
    // this.resumable.assignBrowse(this.props.browseRef.map((ref) => ref.current), false);
    // this.resumable.assignDrop(this.props.dragRef.current);

    // Define the process after a file is selected
    this.resumable.on('fileAdded', async (resumableFile: Resumable.ResumableFile) => {
      this.canceled = false;
      const file: File = resumableFile.file;

      // Make sure the file matches the expected magic numbers
      const sniffer = new FileSniffer(file);
      if (!await sniffer.extensionMatchesInitialBytes()) {
        this.props.setUploadError(this.props.uploadId, 'File contents do not match extension.');
        return false;
        this.props.beginUpload(
          this.props.uploadId, this.props.clientId, this.props.fileDropId, this.props.folderId, file.name,
        );
      }

      // Send the filename to the Redux store

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
                if (!this.canceled) {
                  // this.props.finalizeUpload(this.props.uploadId);
                }
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
      // Stop all monitoring immediately
      this.canceled = true;
      this.progressMonitor.deactivate();
      if (this.statusMonitor) {
        this.statusMonitor.stop();
      }

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
  }

  public componentWillReceiveProps(nextProps: FileDropUploadProps) {
    if (nextProps.canceled === true) {
      this.resumable.cancel();
    }
    if (nextProps.browseRef && nextProps.browseRef.length > 0) {
      this.resumable.assignBrowse(nextProps.browseRef.map((ref) => ref.current), false);
    }
    if (nextProps.dragRef.current && nextProps.dragRef.current !== null) {
      this.resumable.assignDrop(nextProps.dragRef.current);
    }
  }

  public render() {
    return <div />;
  }
}
