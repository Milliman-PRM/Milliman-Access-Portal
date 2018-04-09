import $ = require('jquery');
import _ = require('lodash');
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
    this.values = this.values.slice(0, this.lengthLimit);
  }
  // TODO: extract into separate function; allow arbitrary maps on values
  public avg(weights: Array<number> = []): number {
    let result = 0;
    // fill weights to match length of values
    if (!weights.length) {
      _.fill(weights, 1, 0, this.values.length);
    }
    weights = _.slice(weights, 0, this.values.length);
    _.fill(weights, 0, weights.length, this.values.length);
    // normalize weights
    const sum = _.sum(weights);
    weights = _.map(weights, (x) => x / sum);
    // compute weighted average
    for (let pair of _.zip(this.values, weights)) {
      result += <any>pair[0] * pair[1];
    }
    return result;
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
  private lastRateUnitIndex: number; // corresponds with the magnitude of this.rate
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
    const rate = ((precision: number, unitThreshold: [number, number], weights: Array<number>): string => {
      const units = ['', 'K', 'M', 'G'];
      const upperThreshold = unitThreshold[0];
      const lowerThreshold = unitThreshold[1];
      let rateUnitIndex = 0;
      let now = this.rate.avg(weights);
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
    })(5, [2, 1], _.map(_.range(4, 0, -1), (x) => x**2));
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
