import { StatusMonitor } from '../status-monitor';
import { RootContentItemStatus } from '../view-models/content-publishing';
import { updateCardStatus } from '../shared';

export class PublicationStatusMonitor {
  private readonly monitor: StatusMonitor<RootContentItemStatus>;

  private readonly url: string = 'ContentPublishing/Status';
  private readonly interval: number = 5000;

  constructor() {
    this.monitor = new StatusMonitor<RootContentItemStatus>(
      this.url,
      statusCallback,
      this.interval,
    );
  }

  public start() {
    this.monitor.start();
  }

  public stop() {
    this.monitor.stop();
  }
}

function statusCallback(response: RootContentItemStatus) {
  $('#root-content-items').find('.card-container')
    .toArray().forEach((cardContainer: HTMLElement) => {
      const $cardContainer = $(cardContainer);
      const rootContentItemId = $cardContainer.data().rootContentItemId;
      updateCardStatus($cardContainer, {
        User: {
          FirstName: '',
        },
        StatusEnum: response.Status[rootContentItemId] || 0,
        RootContentItemId: rootContentItemId,
      });
    });
}
