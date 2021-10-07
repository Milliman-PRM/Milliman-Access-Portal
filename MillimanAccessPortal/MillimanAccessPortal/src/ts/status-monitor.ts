export class StatusMonitor<T> {
  public readonly interval: number = 5000;
  private active: boolean = false;

  constructor(
    readonly url: string,
    readonly callback: (response: T) => void = () => null,
    interval?: number,
    readonly activeWindowOnly: boolean = false,
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

  public stop(response?: Response) {
    if (response && response.status === 401) {
      // session timed out, reload the page
      window.location.reload();
    }
    this.active = false;
  }

  public checkStatus() {
    fetch(this.url, {
      method: 'GET',
      cache: 'no-cache',
      credentials: 'same-origin',
    })
      .then((response) => {
        if (!response.ok) {
          this.stop(response);
        }
        return response.json() as Promise<T>;
      })
      .then((response) => this.callback(response))
      .catch((response) => this.stop(response));
  }

  private monitor() {
    if (this.active) {
      if (!(this.activeWindowOnly && document.hidden)) {
        this.checkStatus();
      }
      setTimeout(this.monitor, this.interval);
    }
  }
}
