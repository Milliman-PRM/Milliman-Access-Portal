import $ = require('jquery');
import options = require('../lib-options');
const Resumable = require('resumablejs');
import * as forge from 'node-forge';

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

abstract class Upload {
  protected scanner: FileScanner;
  protected resumable: any;
  protected monitor: ProgressMonitor;

  protected checksum: string;
  protected cancelable: boolean;

  protected headers = {
    RequestVerificationToken: $("input[name='__RequestVerificationToken']").val().toString(),
  };
  protected formData = {
    Checksum: this.checksum,
  }

  constructor(readonly rootElement: HTMLElement) {
    this.scanner = new FileScanner();
    this.resumable = new Resumable(Object.assign({}, options.resumableOptions, {
      target: '/FileUpload/UploadChunk',
      headers: () => this.headers,
      query: () => this.formData,
      generateUniqueIdentifier: (file: File, event: Event) => this.generateUID(file, event),
    }));
    if (!this.resumable.support) {
      throw new Error('This browser does not support resumable file uploads.');
    }

    this.resumable.assignBrowse(this.selectBrowseElement(rootElement), false);
    this.resumable.on('fileAdded', async (file) => {
      this.selectFileNameElement(this.rootElement).innerHTML = file.fileName;
      //s this.state = UploadState.Uploading;

      const message = forge.md.sha1.create();
      this.monitor = new ProgressMonitor(
        () => this.scanner.progress,
        this.renderChecksumProgress,
        file.file.size,
      );
      this.monitor.monitor();
      await this.scanner.scan(file.file, message.update);
      this.checksum = message.digest().toHex();

      this.monitor = new ProgressMonitor(
        () => this.resumable.progress(),
        this.renderUploadProgress,
        file.file.size,
      );
      this.monitor.monitor();
      this.getChunkStatus();
      this.resumable.upload();
    });
    this.resumable.on('fileSuccess', (file, message) => {
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
        this.renderUploadProgress({
          percentage: '100%',
          rate: 'Upload complete',
          remainingTime: '',
        });
      }).fail((response) => {
        throw new Error(`Something went wrong. Response: ${response}`);
      }).always((response) => {
      });
    });

    $(rootElement).find('.btn-cancel').click((event) => {
      event.preventDefault();
      event.stopPropagation();
      this.scanner.cancel();
      //s this.state = UploadState.Initial;
    });
    //s this.state = UploadState.Initial;

    // bind functions
    this.renderChecksumProgress = this.renderChecksumProgress.bind(this);
    this.renderUploadProgress = this.renderUploadProgress.bind(this);
  }

  protected abstract generateUID(file: File, event: Event): string;
  
  protected abstract selectBrowseElement(rootElement: HTMLElement): HTMLElement;

  protected abstract selectFileNameElement(rootElement: HTMLElement): HTMLElement;

  protected abstract selectChecksumBarElement(rootElement: HTMLElement): HTMLElement;
  

  protected getChunkStatus() {
    // Not implemented
    // TODO: get request for already-received chunks
    // TODO: set `this.r.files[0].chunks[n].tested = true;` for already received
  }

  protected renderChecksumProgress(summary: ProgressSummary) {
    $(this.rootElement).find('.card-progress-bar-1').width(summary.percentage);
    $(this.rootElement)
      .find('.card-progress-status-text')
      .html(`${summary.rate}   ${summary.remainingTime}`);
  }

  protected renderUploadProgress(summary: ProgressSummary) {
    $(this.rootElement).find('.card-progress-bar-2').width(summary.percentage);
    $(this.rootElement)
      .find('.card-progress-status-text')
      .html(`${summary.rate}   ${summary.remainingTime}`);
  }
}


export enum PublicationComponent {
  Content = 'content',
  UserGuide = 'user_guide',
  Image = 'image',
}

export class PublicationUpload extends Upload {
  private publicationGUID: string;
  private component: PublicationComponent;

  constructor(rootElement: HTMLElement, publicationGUID: string, component: PublicationComponent, setUploadState: any) {
    super(rootElement);
    this.publicationGUID = publicationGUID;
    this.component = component;
  }

  protected generateUID(file: File, event: Event): string {
    return `publication-${this.component}-${this.publicationGUID}`;
  }

  protected selectBrowseElement(rootElement: HTMLElement): HTMLElement {
    return rootElement;
  }

  protected selectFileNameElement(rootElement: HTMLElement): HTMLElement {
    return $(rootElement).find('.card-body-secondary-text')[0];
  }

  protected selectChecksumBarElement(rootElement: HTMLElement): HTMLElement {
    return $(rootElement).find('.card-progress-bar-1')[0];
  }

}