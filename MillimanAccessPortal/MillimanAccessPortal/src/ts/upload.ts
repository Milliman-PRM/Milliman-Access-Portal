import $ = require('jquery');
import _ = require('lodash');
import forge = require('node-forge');
import options = require('./lib-options');
import shared = require('./shared');
const resumable = require('resumablejs');


interface ProgressSnapshot {
  ratio: number; // uploaded / total
  time: number; // absolute time at which this snapshot was taken
}

interface ProgressStats {
  percentage: string;
  rate: string;
  remainingTime: string;
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

enum UploadState {
  Initial = 'initial',
  Uploading = 'uploading',
  Paused = 'paused',
}


// A value that retains a configurable number of past values
class RetainedValue<T> {
  private _values: Array<T>;
  public get values(): Array<T> {
    return this._values;
  }

  constructor(readonly lengthLimit: number) {
    this.reset();
  }
  public reset() {
    this._values = [];
  }
  get now(): T {
    return this._values[0];
  }
  get ref(): T {
    return this._values[this._values.length - 1];
  }
  public insert(value: T): void {
    this._values.splice(0, 0, value);
    this._values = this._values.slice(0, this.lengthLimit);
  }
}

class ProgressTracker {
  private snapshot: RetainedValue<ProgressSnapshot>;
  private rate: RetainedValue<number>;
  private remainingTime: RetainedValue<number>;
  private lastRateUnitIndex: number; // corresponds with the magnitude of this.rate

  constructor(readonly chunkSize: number) {
    this.snapshot = new RetainedValue(8);
    this.rate = new RetainedValue(4);
    this.remainingTime = new RetainedValue(1);
    this.lastRateUnitIndex = 0;
  }

  public reset() {
    this.snapshot.reset();
    this.rate.reset();
    this.remainingTime.reset();
    this.lastRateUnitIndex = 0;
  }

  public update(ratio: number, time: number) {
    this.snapshot.insert({
      ratio: ratio,
      time: time,
    });
    this.rate.insert((() => {
      // Compute rate
      const bytes = this.chunkSize * (this.snapshot.now.ratio - this.snapshot.ref.ratio);
      const seconds = (this.snapshot.now.time - this.snapshot.ref.time) / 1000;
      return seconds
        ? bytes / seconds
        : 0; // return 0 instead of NaN when denominator is 0
    })());
    this.remainingTime.insert((() => {
      // Estimate remaining time
      const bytes = this.chunkSize * (1 - this.snapshot.now.ratio);
      const bytes_p_second = this.rate.now;
      return bytes_p_second
        ? bytes / bytes_p_second
        : 0; // return 0 instead of NaN when denominator is 0
    })());
  }

  public render(): ProgressStats {
    return {
      percentage: ((precision: number): string => {
        const precisionFactor = (10 ** precision);
        const _ = Math.floor(this.snapshot.now.ratio * 100 * precisionFactor) / precisionFactor;
        return `${_}%`;
      })(1),
      rate: ((precision: number, unitThreshold: [number, number], weights: Array<number>): string => {
        const units = ['', 'K', 'M', 'G'];
        const upperThreshold = unitThreshold[0];
        const lowerThreshold = unitThreshold[1];
        let rateUnitIndex = 0;
        let now = (() => {
          const sWeights = weights.slice(0, this.rate.values.length);
          const weightSum = sWeights.reduce((prev, cur) => prev + cur);
          const nWeights = sWeights.map((value) => value / weightSum);
          return this.rate.values
            .map((value, i) => value * (nWeights[i] || 0))
            .reduce((prev, cur) => prev + cur);
          })();
        while (now > (1000 * upperThreshold) && rateUnitIndex < units.length) {
          now /= 1000;
          rateUnitIndex += 1;
        }
        if (this.lastRateUnitIndex > rateUnitIndex) {
          if (now > (1000 * lowerThreshold)) {
            now /= 1000;
            rateUnitIndex += 1;
          }
        }
        this.lastRateUnitIndex = rateUnitIndex;
        const _ = `${now}`.slice(0, precision).replace(/\.$/, '');
        return `${_} ${units[rateUnitIndex]}B/s`;
      })(5, [2, 1], _.map(_.range(4, 0, -1), (x) => x**2)),
      remainingTime: (() => {
        const remainingSeconds = Math.ceil(this.remainingTime.now);
        const seconds = remainingSeconds % 60;
        const minutes = Math.floor(remainingSeconds / 60);
        return `${minutes}:${('0' + seconds).slice(-2)} remaining`;
      })(),
    };
  }
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
      this.generateChecksum(file.file)
        .then(() => this.getChunkStatus())
        .then(() => this.resumable.upload())
        .then(() => this.updateUploadProgress())
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
    this.stats = new ProgressTracker(this.resumable.opts.chunkSize);
    this.state = UploadState.Initial;
  }

  protected abstract generateUID(file: File, event: Event): string;
  
  protected abstract selectBrowseElement(rootElement: HTMLElement): HTMLElement;

  protected abstract selectFileNameElement(rootElement: HTMLElement): HTMLElement;

  protected abstract selectChecksumBarElement(rootElement: HTMLElement): HTMLElement;
  
  protected generateChecksum(file: File) {
    return new Promise((resolve, reject) => {
      const self = this;
      const md = forge.md.sha1.create();
      const reader = new FileReader();
      const chunkSize = (2 ** 20); // 1 MiB
      let offset = 0;
      reader.onload = function () {
        if (self.state === UploadState.Initial) {
          reject();
          return
        }
        md.update(this.result);
        offset += chunkSize;
        if (offset >= file.size) {
          self.renderChecksumProgress(1);
          self.checksum = md.digest().toHex();
          resolve();
        } else {
          self.renderChecksumProgress(offset / file.size);
          reader.readAsBinaryString(file.slice(offset, offset + chunkSize));
        }
      };
      reader.onerror = () => reject;

      $(self.rootElement).find('.card-progress-status-text').html('Processing...');
      reader.readAsBinaryString(file.slice(offset, offset + chunkSize));
    })
  }

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
      this.stats.update(this.resumable.progress(), new Date().getTime());
      this.stats.render();
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