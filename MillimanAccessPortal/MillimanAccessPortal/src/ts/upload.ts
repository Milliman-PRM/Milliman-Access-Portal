import $ = require('jquery');
import shared = require('./shared');
import { Resumable } from 'resumablejs';

// A value that retains a configurable number of past values
// Only the most recent value and the oldest value are public
class RetainedValue<T> {
  private values: Array<T>;
  constructor(readonly lengthLimit: number) {
    this.values = [];
  }
  get now(): T {
    return this.values[0];
  }
  get ref(): T {
    return this.values[this.values.length - 1];
  }
  public insert(value: T): void {
    this.values.splice(0, 0, value);
    this.values.slice(this.lengthLimit);
  }
}

interface ResumableProgressSnapshot {
  ratio: number; // uploaded / total
  time: number; // absolute time at which this snapshot was taken
}

export class ResumableProgressStats {
  snapshot: RetainedValue<ResumableProgressSnapshot>;
  rate: RetainedValue<number>;
  remainingTime: RetainedValue<number>;
  lastRateUnitIndex: number; // corresponds with the magnitude of this.rate
  constructor(snapshotLengthLimit: number) {
    this.snapshot = new RetainedValue(snapshotLengthLimit);
    this.rate = new RetainedValue(1);
    this.remainingTime = new RetainedValue(1);
    this.lastRateUnitIndex = 0;
  }

  public update(r: any) {
    this.snapshot.insert({
      ratio: r.progress(),
      time: new Date().getTime(),
    });
    this.rate.insert((() => {
      // Compute upload rate
      const bytes = r.getSize() * (this.snapshot.now.ratio - this.snapshot.ref.ratio);
      const seconds = (this.snapshot.now.time - this.snapshot.ref.time) / 1000;
      return bytes / seconds;
    })());
    this.remainingTime.insert((() => {
      // Estimate remaining time
      const bytes = r.getSize() * (1 - this.snapshot.now.ratio);
      const bytes_p_second = this.rate.now;
      return bytes / bytes_p_second;
    })());
  }

  public render() {
    const percentage = ((precision: number): string => {
      const precisionFactor = (10 ** precision);
      const _ = Math.floor(this.snapshot.now.ratio * 100 * precisionFactor) / precisionFactor;
      return `${_}%`;
    })(1);
    const rate = ((precision: number, upperThreshold: number, lowerThreshold: number): string => {
      const units = ['', 'Ki', 'Mi', 'Gi'];
      let rateUnitIndex = 0;
      let now = this.rate.now;
      while (now > (1024 * upperThreshold) && rateUnitIndex < units.length) {
        now /= 1024;
        rateUnitIndex += 1;
      }
      if (this.lastRateUnitIndex > rateUnitIndex) {
        if (now > (1024 * lowerThreshold)) {
          now /= 1024;
          rateUnitIndex += 1;
        }
      }
      this.lastRateUnitIndex = rateUnitIndex;
      const _ = `${now}`.slice(0, precision).replace(/\.$/, '');
      return `${_} ${units[rateUnitIndex]}B/s`;
    })(5, 2, 1);
    const remainingTime = (() => {
      const remainingSeconds = Math.ceil(this.remainingTime.now);
      const seconds = remainingSeconds % 60;
      const minutes = Math.floor(remainingSeconds / 60);
      return `${minutes}:${('0' + seconds).slice(-2)} remaining`;
    })();
    
    (() => {
      $('#file-progress-resumable').width(percentage);
    })();
    console.log(`${rate}  ${remainingTime}...`);
  }
}
