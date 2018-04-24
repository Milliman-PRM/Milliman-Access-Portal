import { RetainedValue } from './retained-value';

interface ProgressSnapshot {
  ratio: number; // uploaded / total
  time: number; // absolute time at which this snapshot was taken
}

interface ProgressStats {
  percentage: string;
  rate: string;
  remainingTime: string;
}

export class ProgressTracker {
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