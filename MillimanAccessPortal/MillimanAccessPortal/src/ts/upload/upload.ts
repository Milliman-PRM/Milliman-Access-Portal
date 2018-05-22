import $ = require('jquery');
import forge = require('node-forge');
const Resumable = require('resumablejs');

import { resumableOptions } from '../lib-options';
import { ProgressMonitor, ProgressSummary } from './progress-monitor';
import { FileScanner } from './file-scanner';
import { RetainedValue } from './retained-value';


export enum UploadComponent {
  Content = 'MasterContent',
  UserGuide = 'Thumbnail',
  Image = 'ReleaseNotes',
  ReleaseNotes = 'UserGuide',
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

  public getUID: (file: File, event: Event) => string = () => '';
  public onChecksumProgress: (progress: ProgressSummary) => void = () => {};
  public onUploadProgress: (progress: ProgressSummary) => void = () => {};
  public onProgressMessage: (message: string) => void = () => {};

  public onFileAdded: (file: File) => void = () => {};
  public onFileSuccess: (fileGUID: string) => void = () => {};
  public onStateChange: (alertUnload: boolean, cancelable: boolean) => void = () => {};

  // Cancelable, checksum, and file GUID indicate the upload state.
  // Whether the current ongoing process can be canceled
  protected _cancelable: boolean = false;
  protected get cancelable(): boolean {
    return this._cancelable;
  }
  protected setCancelable(cancelable: boolean) {
    this._cancelable = cancelable;
    this.onStateChange(this.alertUnload, this.cancelable);
  }
  // The checksum of the file in progress
  protected _checksum: string = null;
  protected get checksum(): string {
    return this._checksum;
  }
  protected setChecksum(checksum: string) {
    this._checksum = checksum;
    this.onStateChange(this.alertUnload, this.cancelable);
  }
  // The GUID of a successful file upload provided by the server
  protected _fileGUID: string = null;
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

  // Additional headers to send with each resumable chunk
  protected resumableHeaders = {
    RequestVerificationToken: $("input[name='__RequestVerificationToken']").val().toString(),
  };
  // Additional form data to send with each resumable chunk
  protected resumableFormData = {
    Checksum: this.checksum,
  }

  constructor() {
    this.resumable = new Resumable(Object.assign({}, resumableOptions, {
      target: '/FileUpload/UploadChunk',
      headers: () => this.resumableHeaders,
      query: () => this.resumableFormData,
      generateUniqueIdentifier: (file: File, event: Event) => this.getUID(file, event),
    }));
    if (!this.resumable.support) {
      throw new Error('This browser does not support resumable file uploads.');
    }
    this.scanner = new FileScanner();

    this.resumable.on('fileAdded', async (resumableFile) => {
      const file: File = resumableFile.file;
      this.setCancelable(true);
      this.onFileAdded(file);

      this.onChecksumProgress(ProgressSummary.Empty());
      this.onUploadProgress(ProgressSummary.Empty());

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
        return
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
          ChunkNumber: 0,
          TotalChunks: -1,
          ChunkSize: this.resumable.opts.chunkSize,
          TotalSize: file.size,
          FileName: file.fileName,
          UID: file.uniqueIdentifier,
          Checksum: this.checksum,
          Type: '',
        };
        $.ajax({
          type: 'POST',
          url: 'FileUpload/CancelUpload',
          data: cancelInfo,
          headers: {
            RequestVerificationToken: $("input[name='__RequestVerificationToken']").val().toString()
          }
        }).done((response) => {
        }).fail((response) => {
          throw new Error(`Something went wrong. Response: ${JSON.stringify(response)}`);
        }).always((response) => {
          this.setChecksum(null);
        });
      });
    });

    this.resumable.on('fileSuccess', (file, message) => {
      this.setCancelable(false);
      const finalizeInfo: ResumableInfo = {
        ChunkNumber: 0,
        TotalChunks: file.chunks.length,
        ChunkSize: this.resumable.opts.chunkSize,
        TotalSize: file.size,
        FileName: file.fileName,
        UID: file.uniqueIdentifier,
        Checksum: this.checksum,
        Type: '',
      };
      $.ajax({
        type: 'POST',
        url: 'FileUpload/FinalizeUpload',
        data: finalizeInfo,
        headers: {
          RequestVerificationToken: $("input[name='__RequestVerificationToken']").val().toString()
        }
      }).done((response: string) => {
        this.monitor.deactivate();
        this.onUploadProgress(ProgressSummary.Full());
        this.setFileGUID(response);
        this.onFileSuccess(this.fileGUID);
      }).fail((response) => {
        throw new Error(`Something went wrong. Response: ${response}`);
      }).always((response) => {
        this.setChecksum(null);
      });
    });
  }

  private cancel() {
    if (this.resumable) this.resumable.cancel();
    if (this.scanner) this.scanner.cancel();
    if (this.monitor) this.monitor.deactivate();
    this.onProgressMessage('Upload canceled');
    this.setCancelable(false);
    this.setChecksum(null);
  }

  public assignBrowse(element: HTMLElement) {
   // // Clone the input to clear any event listeners
   // const input = this.selectBrowseElement(element);
   // const clonedInput = input.cloneNode(true);
   // $(clonedInput).find('input[type="file"]').remove();
   // $(input).replaceWith($(clonedInput));

    this.resumable.assignBrowse(element, false);
  }

  public reset() {
    this.cancel();
    this.setFileGUID(null);
    this.onChecksumProgress(ProgressSummary.Empty());
    this.onUploadProgress(ProgressSummary.Empty());
  }

  private getChunkStatus() {
    // Not implemented
    // TODO: get request for already-received chunks
    // TODO: set `this.resumable.files[0].chunks[n].tested = true;` for already received
  }

}
