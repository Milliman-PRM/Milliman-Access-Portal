import $ = require('jquery');
import options = require('../lib-options');
const resumable = require('resumablejs');

import { ProgressTracker } from './progress-tracker';
import { FileScanner } from './file-scanner';


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

enum UploadState {
  Initial = 'initial',
  Uploading = 'uploading',
  Paused = 'paused',
}




abstract class Upload {
  public resumable: any;
  protected rootElement: HTMLElement;
  protected checksum: string;
  protected stats: ProgressTracker;

  protected _state: UploadState;
  protected get state(): UploadState {
    return this._state;
  }
  protected set state(value: UploadState) {
    // Todo: run hooks
    if (value === UploadState.Initial) {
      $(this.rootElement).find('.btn-pause').css('visibility', 'hidden');
      $(this.rootElement).find('.btn-cancel').css('visibility', 'hidden');
      this.resumable.cancel();
    } else if (value === UploadState.Paused) {
      this.resumable.pause();
    } else if (value === UploadState.Uploading) {
      $(this.rootElement).find('.btn-pause').css('visibility', 'visible');
      $(this.rootElement).find('.btn-cancel').css('visibility', 'visible');
    }
    this._state = value;
  }

  protected headers = {
    RequestVerificationToken: $("input[name='__RequestVerificationToken']").val().toString(),
  };
  protected formData = {
    Checksum: this.checksum,
  }

  constructor(rootElement: HTMLElement) {
    this.resumable = new resumable(Object.assign({}, options.resumableOptions, {
      target: '/FileUpload/UploadChunk',
      headers: () => this.headers,
      query: () => this.formData,
      generateUniqueIdentifier: (file: File, event: Event) => this.generateUID(file, event),
    }));
    if (!this.resumable.support) {
      throw new Error('This browser does not support resumable file uploads.');
    }
    this.resumable.on('fileAdded', (file) => {
      this.selectFileNameElement(this.rootElement).innerHTML = file.fileName;
      this.state = UploadState.Uploading;
      new FileScanner(file.file, 2 ** 20).scan(() => {}, console.log)
        .then(() => this.getChunkStatus())
        //.then(() => this.resumable.upload())
        //.then(() => this.updateUploadProgress())
        .catch(() => console.log('upload canceled'));
    });
    this.resumable.assignBrowse(this.selectBrowseElement(rootElement), false);
    this.rootElement = rootElement;
    $(rootElement).find('.btn-pause').click((event) => {
      event.preventDefault();
      event.stopPropagation();
      if (this.state === UploadState.Paused) {
        this.state = UploadState.Uploading;
        this.resumable.upload();
      } else {
        this.state = UploadState.Paused;
      }
    });
    $(rootElement).find('.btn-cancel').click((event) => {
      event.preventDefault();
      event.stopPropagation();
      this.state = UploadState.Initial;
    });
    // this.stats = new ProgressTracker(this.resumable.opts.chunkSize);
    this.state = UploadState.Initial;
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

  protected renderChecksumProgress(progress: number) {
    const precision = 2;
    const progressFmt = `${Math.floor(progress * 100 * (10 ** precision)) / (10 ** precision)}%`;
    $(this.selectChecksumBarElement(this.rootElement)).width(progressFmt);
  }

  protected updateUploadProgress() {
    setTimeout(() => {
      // this.stats.update(this.resumable.progress(), new Date().getTime());
      // this.stats.render();
      if (this.resumable.progress() < 1) {
        this.updateUploadProgress();
      }
    }, 1000);
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
        // File is uploaded
        setUploadState(0); // TODO: remove
      }).fail((response) => {
        throw new Error(`Something went wrong. Response: ${response}`);
      });
    });
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