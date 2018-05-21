import $ = require('jquery');
import forge = require('node-forge');
const Resumable = require('resumablejs');

import { resumableOptions } from '../lib-options';
import { ProgressMonitor, ProgressSummary } from './progress-monitor';
import { FileScanner } from './file-scanner';
import { RetainedValue } from './retained-value';


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

export abstract class Upload {
  protected scanner: FileScanner;
  protected resumable: any;
  protected monitor: ProgressMonitor;

  // attributes that indicate the upload state
  protected _checksum: string;
  protected get checksum(): string {
    return this._checksum;
  }
  protected set checksum(checksum: string) {
    this._checksum = checksum;
    this.signalRequiresUnloadAlert();
  }
  protected _fileGUID: string;
  protected get fileGUID(): string {
    return this._fileGUID;
  }
  protected set fileGUID(fileGUID: string) {
    this._fileGUID = fileGUID;
    this.signalRequiresUnloadAlert();
  }
  protected _cancelable: boolean;
  protected get cancelable(): boolean {
    if (this._cancelable === undefined) {
      this._cancelable = false;
    }
    return this._cancelable;
  }
  protected set cancelable(cancelable: boolean) {
    const $resumableInput = $(this.selectBrowseElement(this.rootElement))
      .children('input');
    const $cancelButton = $(this.selectBrowseElement(this.rootElement))
      .find('.btn-cancel');
    if (cancelable) {
      $resumableInput.attr('disabled', '');
      $cancelButton.css('visibility', 'visible');
    } else {
      $resumableInput.removeAttr('disabled');
      $cancelButton.css('visibility', 'hidden');
    }
    this._cancelable = cancelable;
    this.signalRequiresUnloadAlert();
  }

  protected headers = {
    RequestVerificationToken: $("input[name='__RequestVerificationToken']").val().toString(),
  };
  protected formData = {
    Checksum: this.checksum,
  }

  constructor(
    readonly rootElement: HTMLElement,
    readonly unloadAlertCallback: (a: boolean) => void,
    readonly fileSuccessCallback: (guid: string) => void,
  ) {
    this.scanner = new FileScanner();
    this.resumable = new Resumable(Object.assign({}, resumableOptions, {
      target: '/FileUpload/UploadChunk',
      headers: () => this.headers,
      query: () => this.formData,
      generateUniqueIdentifier: (file: File, event: Event) => this.generateUID(file, event),
    }));
    if (!this.resumable.support) {
      throw new Error('This browser does not support resumable file uploads.');
    }

    this.attachToBrowseElement(rootElement);

    this.resumable.on('fileAdded', async (file) => {
      this.cancelable = true;
      this.selectFileNameElement(this.rootElement).value = file.fileName;

      this.onFileAdded(file.file);

      this.renderUploadProgress({
        percentage: '0%',
        rate: '',
        remainingTime: '',
      });
      const message = forge.md.sha1.create();
      this.scanner.open(file.file);
      this.monitor = new ProgressMonitor(
        () => this.scanner.progress,
        this.renderChecksumProgress.bind(this),
        file.file.size,
      );
      this.monitor.monitor();
      try {
        await this.scanner.scan(message.update);
      } catch {
        // Upload was canceled
        return
      }
      this.checksum = message.digest().toHex();

      this.monitor = new ProgressMonitor(
        () => this.resumable.progress(),
        this.renderUploadProgress.bind(this),
        file.file.size,
      );
      this.monitor.monitor();
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
          throw new Error(`Something went wrong. Response: ${response}`);
        }).always((response) => {
          this.checksum = undefined;
        });
      });
    });
    this.resumable.on('fileSuccess', (file, message) => {
      this.cancelable = false;
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
      }).done((response) => {
        this.monitor.monitorEnd();
        this.renderUploadProgress({
          percentage: '100%',
          rate: 'Upload complete',
          remainingTime: '',
        });
        this.fileGUID = response;
        this.fileSuccessCallback(this.fileGUID);
      }).fail((response) => {
        throw new Error(`Something went wrong. Response: ${response}`);
      }).always((response) => {
        this.checksum = undefined;
      });
    });
    $(rootElement).find('.btn-cancel').click((event) => {
      event.preventDefault();
      event.stopPropagation();
      if (this.checksum) {
        this.resumable.cancel();
      } else {
        this.scanner.cancel();
      }
      this.monitor.monitorEnd();
      this.setProgressMessage('Upload canceled');
      this.cancelable = false;
      this.checksum = undefined;
    });
  }

  public attachToBrowseElement(element: HTMLElement) {
    // Clone the input to clear any event listeners
    const input = this.selectBrowseElement(element);
    $(input).replaceWith($(input.cloneNode(false)));

    this.resumable.assignBrowse(this.selectBrowseElement(element), false);
  }

  public reset() {
    this.cancelable = false;
    this.checksum = undefined;
    this.fileGUID = undefined;
  }

  private signalRequiresUnloadAlert() {
    this.unloadAlertCallback(
      this.cancelable
      || this.checksum !== undefined
      || this.fileGUID !== undefined
    );
  }

  private getChunkStatus() {
    // Not implemented
    // TODO: get request for already-received chunks
    // TODO: set `this.resumable.files[0].chunks[n].tested = true;` for already received
  }

  protected abstract generateUID(file: File, event: Event): string;
  
  protected abstract selectBrowseElement(rootElement: HTMLElement): HTMLElement;

  protected abstract selectFileNameElement(rootElement: HTMLElement): HTMLInputElement;

  protected abstract selectChecksumBarElement(rootElement: HTMLElement): HTMLElement;

  protected abstract renderChecksumProgress(summary: ProgressSummary);

  protected abstract renderUploadProgress(summary: ProgressSummary);

  protected abstract setProgressMessage(message: string);

  public abstract onFileAdded(file: File);
}
