import { resumableOptions } from '../lib-options';
import { FileScanner } from './file-scanner';
import { ProgressMonitor, ProgressSummary } from './progress-monitor';

import $ = require('jquery');
import forge = require('node-forge');
const resumable = require('resumablejs');

export enum UploadComponent {
  Content = 'MasterContent',
  UserGuide = 'UserGuide',
  Image = 'Thumbnail',
  ReleaseNotes = 'ReleaseNotes',
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

export class Upload {
  protected resumable: any;
  protected scanner: FileScanner;
  protected monitor: ProgressMonitor;

  // Additional headers to send with each resumable chunk
  protected resumableHeaders = {
    RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val().toString(),
  };
  // Additional form data to send with each resumable chunk
  protected resumableFormData = {
    Checksum: this.checksum,
  };

  // Cancelable, checksum, and file GUID indicate the upload state.
  // Whether the current ongoing process can be canceled
  private _cancelable: boolean = false;
  // The checksum of the file in progress
  private _checksum: string = null;
  // The GUID of a successful file upload provided by the server
  private _fileGUID: string = null;

  constructor() {
    this.resumable = new resumable(Object.assign({}, resumableOptions, {
      generateUniqueIdentifier: (file: File, event: Event) => this.getUID(file, event),
      headers: () => this.resumableHeaders,
      query: () => this.resumableFormData,
      target: '/FileUpload/UploadChunk',
    }));
    if (!this.resumable.support) {
      throw new Error('This browser does not support resumable file uploads.');
    }
    this.scanner = new FileScanner();

    this.resumable.on('fileAdded', async (resumableFile) => {
      const file: File = resumableFile.file;
      this.setCancelable(true);
      this.onFileAdded(resumableFile);

      this.onChecksumProgress(ProgressSummary.empty());
      this.onUploadProgress(ProgressSummary.empty());

      const messageDigest = forge.md.sha1.create();
      this.scanner.open(file);
      this.monitor = new ProgressMonitor(
        () => this.scanner.progress,
        this.onChecksumProgress,
        file.size,
      );
      this.monitor.activate();

      try {
        await this.scanner.scan(messageDigest.update);
      } catch {
        // Upload was canceled
        return;
      }
      this.setChecksum(messageDigest.digest().toHex());

      this.monitor = new ProgressMonitor(
        () => this.resumable.progress(),
        this.onUploadProgress,
        file.size,
      );
      this.monitor.activate();

      this.getChunkStatus();
      this.resumable.upload();
    });

    this.resumable.on('beforeCancel', () => {
      this.resumable.files.forEach((file) => {
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
        $.ajax({
          data: cancelInfo,
          headers: {
            RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val().toString(),
          },
          type: 'POST',
          url: 'FileUpload/CancelUpload',
        }).fail((response) => {
          throw new Error(`Something went wrong. Response: ${JSON.stringify(response)}`);
        }).always(() => {
          this.setChecksum(null);
        });
      });
    });

    this.resumable.on('fileSuccess', (file) => {
      this.setCancelable(false);
      this.onUploadProgress(ProgressSummary.full());
      const finalizeInfo: ResumableInfo = {
        Checksum: this.checksum,
        ChunkNumber: 0,
        ChunkSize: this.resumable.opts.chunkSize,
        FileName: file.fileName,
        TotalChunks: file.chunks.length,
        TotalSize: file.size,
        Type: '',
        UID: file.uniqueIdentifier,
      };
      $.ajax({
        data: finalizeInfo,
        headers: {
          RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val().toString(),
        },
        type: 'POST',
        url: 'FileUpload/FinalizeUpload',
      }).done((response: string) => {
        this.monitor.deactivate();
        this.onUploadProgress(ProgressSummary.full());
        this.setFileGUID(response);
        this.onFileSuccess(this.fileGUID);
      }).fail((response) => {
        throw new Error(`Something went wrong. Response: ${response}`);
      }).always(() => {
        this.setChecksum(null);
      });
    });
  }

  public getUID: (file: File, event: Event) => string = () => '';
  public onChecksumProgress: (progress: ProgressSummary) => void = () => undefined;
  public onUploadProgress: (progress: ProgressSummary) => void = () => undefined;
  public onProgressMessage: (message: string) => void = () => undefined;

  public onFileAdded: (resumableFile: any) => void = () => undefined;
  public onFileSuccess: (fileGUID: string) => void = () => undefined;
  public onStateChange: (alertUnload: boolean, cancelable: boolean) => void = () => undefined;

  public cancel() {
    if (this.resumable) { this.resumable.cancel(); }
    if (this.scanner) { this.scanner.cancel(); }
    if (this.monitor) { this.monitor.deactivate(); }
    this.onProgressMessage('Upload canceled');
    this.setCancelable(false);
    this.setChecksum(null);
  }

  public assignBrowse(element: HTMLElement) {
    this.resumable.assignBrowse(element, false);
  }

  public setFileTypes(fileTypes: string[]) {
    this.resumable.opts.fileType = fileTypes;
  }

  public reset() {
    this.cancel();
    this.setFileGUID(null);
    this.onChecksumProgress(ProgressSummary.empty());
    this.onUploadProgress(ProgressSummary.empty());
  }

  public valid() {
    if (this.cancelable || this.checksum !== null) {
      return false;
    } else if (this.fileGUID !== null) {
      return true;
    }
    return undefined;
  }

  protected get cancelable(): boolean {
    return this._cancelable;
  }
  protected setCancelable(cancelable: boolean) {
    this._cancelable = cancelable;
    this.onStateChange(this.alertUnload, this.cancelable);
  }

  protected get checksum(): string {
    return this._checksum;
  }
  protected setChecksum(checksum: string) {
    this._checksum = checksum;
    this.onStateChange(this.alertUnload, this.cancelable);
  }

  protected get fileGUID(): string {
    return this._fileGUID;
  }
  protected setFileGUID(fileGUID: string) {
    this._fileGUID = fileGUID;
    this.onStateChange(this.alertUnload, this.cancelable);
  }

  // If any of cancelable, checksum, and file GUID are set, then an alert
  // should appear before unloading the page.
  protected get alertUnload(): boolean {
    return this.checksum !== null || this.fileGUID !== null || this.cancelable;
  }

  private getChunkStatus() {
    // Not implemented
    // TODO: get request for already-received chunks
    // TODO: set `this.resumable.files[0].chunks[n].tested = true;` for already received
  }

}
