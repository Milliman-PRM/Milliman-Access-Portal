import { Upload } from '../upload/upload';
import { ProgressSummary } from '../upload/progress-monitor';

export enum PublicationComponent {
  Content,
  UserGuide,
  Image,
}
export const PublicationComponentInfo = [
  {
    name: 'content',
    displayName: 'Content',
  },
  {
    name: 'user_guide',
    displayName: 'User guide',
  },
  {
    name: 'image',
    displayName: 'Image',
  },
];

export class PublicationUpload extends Upload {

  constructor(
    rootElement: HTMLElement,
    unloadAlertCallback: (a: boolean) => void,
    readonly publicationGUID: string,
    readonly component: PublicationComponent,
  ) {
    super(rootElement, unloadAlertCallback);
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

  protected renderChecksumProgress(summary: ProgressSummary) {
    $(this.rootElement).find('.card-progress-bar-1').width(summary.percentage);
    $(this.rootElement)
      .find('.card-progress-status-text')
      .html(`${summary.rate}   ${summary.remainingTime}`);
  }

  protected renderUploadProgress(summary: ProgressSummary) {
    $(this.rootElement).find('.card-progress-bar-2').width(summary.percentage);
    $(this.rootElement)
      .find('.card-progress-status-text')
      .html(`${summary.rate}   ${summary.remainingTime}`);
  }

  protected setProgressMessage(message: string) {
    $(this.rootElement)
      .find('.card-progress-status-text')
      .html(message);
  }
}
