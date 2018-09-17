import * as $ from 'jquery';

export class StatusMonitor<T> {
  public readonly interval: number = 5000;
  private active: boolean = false;

  constructor(
    readonly url: string,
    readonly callback: (response: T) => void,
    interval?: number,
  ) {
    if (!isNaN(interval)) {
      this.interval = interval;
    }
    this.stop = this.stop.bind(this);
    this.monitor = this.monitor.bind(this);
  }

  public start() {
    this.active = true;
    this.monitor();
  }

  public stop() {
    this.active = false;
  }

  public checkStatus() {
    $.get({
      url: this.url,
    })
    .done(this.callback)
    .fail(this.stop);
  }

  private monitor() {
    if (this.active) {
      this.checkStatus();
      setTimeout(this.monitor, this.interval);
    }
  }
}
