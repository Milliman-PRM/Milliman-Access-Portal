import { Upload } from '../upload/upload';
import { ProgressSummary } from '../upload/progress-monitor';

export enum PublicationComponent {
  Content = 'content',
  UserGuide = 'user_guide',
  Image = 'image',
  ReleaseNotes = 'release_notes',
}

export class PublicationUpload extends Upload {

  constructor(
    rootElement: HTMLElement,
    unloadAlertCallback: (a: boolean) => void,
    fileSuccessCallback: (guid: string) => void,
    readonly formToken: string,
    readonly component: PublicationComponent,
  ) {
    super(rootElement, unloadAlertCallback, fileSuccessCallback);
  }

  protected generateUID(file: File, event: Event): string {
    return `publication-${this.component}-${this.formToken}`;
  }

  protected selectBrowseElement(rootElement: HTMLElement): HTMLElement {
    return $(rootElement).find('input.file-upload')[0];
  }

  protected selectFileNameElement(rootElement: HTMLElement): HTMLInputElement {
    return $(rootElement).find('input.file-upload')[0] as HTMLInputElement;
  }

  protected selectChecksumBarElement(rootElement: HTMLElement): HTMLElement {
    return $(rootElement).find('svg')[0];
  }

  protected renderChecksumProgress(summary: ProgressSummary) {
//    $(this.rootElement).find('.card-progress-bar-1').width(summary.percentage);
//    $(this.rootElement)
//      .find('.card-progress-status-text')
//      .html(`${summary.rate}   ${summary.remainingTime}`);
  }

  protected renderUploadProgress(summary: ProgressSummary) {
//    $(this.rootElement).find('.card-progress-bar-2').width(summary.percentage);
//    $(this.rootElement)
//      .find('.card-progress-status-text')
//      .html(`${summary.rate}   ${summary.remainingTime}`);
    console.log(summary);
  }

  protected setProgressMessage(message: string) {
//    $(this.rootElement)
//      .find('.card-progress-status-text')
//      .html(message);
  }

  public onFileAdded(file: File) {
    if (this.component === PublicationComponent.Image) {
      const reader = new FileReader();

      reader.onload = (ev) => {
        $(this.rootElement)
          .find('.image-preview')
          .attr('src', reader.result);
      };

      reader.readAsDataURL(file);
    }
  }
}
