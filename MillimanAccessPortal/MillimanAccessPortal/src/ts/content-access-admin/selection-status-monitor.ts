import { updateCardStatus } from '../shared';
import { StatusMonitor } from '../status-monitor';
import { ContentAccessStatus } from '../view-models/content-access-admin';

export class SelectionStatusMonitor {
  private readonly monitor: StatusMonitor<ContentAccessStatus>;

  private readonly url: string = 'ContentAccessAdmin/Status';
  private readonly interval: number = 20000;

  constructor() {
    this.monitor = new StatusMonitor<ContentAccessStatus>(
      this.url,
      statusCallback,
      this.interval,
      true,
    );
  }

  public start() {
    this.monitor.start();
  }

  public stop() {
    this.monitor.stop();
  }

  public checkStatus() {
    this.monitor.checkStatus();
  }
}

function statusCallback(response: ContentAccessStatus) {
  $('#root-content-items').find('.card-container')
    .toArray().forEach((cardContainer: HTMLElement) => {
      const $cardContainer = $(cardContainer);
      const status = response.RootContentItemStatusList.Status.filter((s) =>
        s && s.RootContentItemId === $cardContainer.data().rootContentItemId)[0];
      updateCardStatus($cardContainer, status);
      $cardContainer.data('statusEnum', status && status.StatusEnum);
    });
  $('#selection-groups').find('.card-container')
    .toArray().forEach((cardContainer: HTMLElement) => {
      const $cardContainer = $(cardContainer);
      const status = response.SelectionGroupStatusList.Status.filter((s) =>
        s && s.SelectionGroupId === $cardContainer.data().selectionGroupId)[0];
      updateCardStatus($cardContainer, status);
      // updateCardStatusButtons($cardContainer, status && status.StatusEnum);
      $cardContainer.data('statusEnum', status && status.StatusEnum);
    });
  // updateFormStatusButtons();
}
