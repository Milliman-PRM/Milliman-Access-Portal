import { RetainedValue } from './retained-value';

interface ProgressSnapshot {
  progress: number; // uploaded / total
  time: number; // absolute time at which this snapshot was taken
}

export interface ProgressSummary {
  percentage: string;
  rate: string;
  remainingTime: string;
}

export class ProgressMonitor {
  private snapshot: RetainedValue<ProgressSnapshot>;
  private rate: RetainedValue<number>;
  private remainingTime: RetainedValue<number>;
  private readonly monitorInterval: number;
  private lastRateUnitIndex: number = 0; // corresponds with the magnitude of this.rate
  private active: boolean = false;

  constructor(
    readonly progressCallback: () => number,
    readonly renderCallback: (s: ProgressSummary) => void,
    readonly fileSize: number,
  ) {
    this.snapshot = new RetainedValue(8);
    this.rate = new RetainedValue(4);
    this.remainingTime = new RetainedValue(1);
    this.monitorInterval = 1000;

    this._monitor = this._monitor.bind(this);
  }

  public monitor() {
    this.active = true;
    this._monitor();
  }

  public monitorEnd() {
    this.active = false;
  }

  private _monitor() {
    if (this.active) {
      const progress = this.progressCallback();
      const now = new Date().getTime();
      this.update(progress, now);

      const summary = this.render();
      this.renderCallback(summary);

      // stop monitoring if monitored callback has finished
      this.active = (progress < 1);

      setTimeout(this._monitor, this.monitorInterval);
    }
  }

  private update(progress: number, time: number) {
    this.snapshot.insert({
      progress: progress,
      time: time,
    });
    this.rate.insert((() => {
      // Compute rate
      const bytes = this.fileSize * (this.snapshot.now.progress - this.snapshot.ref.progress);
      const seconds = (this.snapshot.now.time - this.snapshot.ref.time) / 1000;
      return seconds
        ? bytes / seconds
        : 0; // return 0 instead of NaN when denominator is 0
    })());
    this.remainingTime.insert((() => {
      // Estimate remaining time
      const bytes = this.fileSize * (1 - this.snapshot.now.progress);
      const bytes_p_second = this.rate.now;
      return bytes_p_second
        ? bytes / bytes_p_second
        : 0; // return 0 instead of NaN when denominator is 0
    })());
  }

  private render(): ProgressSummary {
    return {
      percentage: ((precision: number): string => {
        const precisionFactor = (10 ** precision);
        const progress = Math.min(Math.max(this.snapshot.now.progress, 0), 1);
        const _ = Math.floor(progress * 100 * precisionFactor) / precisionFactor;
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
      })(5, [2, 1], [4, 3, 2, 1].map((value) => value ** 2)),
      remainingTime: (() => {
        const remainingSeconds = Math.ceil(this.remainingTime.now);
        const seconds = remainingSeconds % 60;
        const minutes = Math.floor(remainingSeconds / 60);
        return `${minutes}:${('0' + seconds).slice(-2)} remaining`;
      })(),
    };
  }
}