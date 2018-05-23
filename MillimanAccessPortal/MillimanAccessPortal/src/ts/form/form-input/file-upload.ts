import { FormInput } from './input';
import { Upload, UploadComponent } from '../../upload/upload';
import { ProgressSummary } from '../../upload/progress-monitor';

export class FileUploadInput extends FormInput {
  protected _cssClasses = {
    main: 'form-input-file-upload',
    title: 'form-input-file-upload-title',
    extension: 'form-input-file-upload-contents',
  }

  protected findInput = ($entryPoint: JQuery<HTMLElement>) => $entryPoint.find('input.file-upload-guid');

  protected getValueFn = ($input: JQuery<HTMLElement>) => $input.val;
  protected setValueFn = ($input: JQuery<HTMLElement>) => $input.val;

  protected disable = ($input: JQuery<HTMLElement>) => $input.parent().find('*').attr('disabled', '');
  protected enable = ($input: JQuery<HTMLElement>) => $input.parent().find('*').removeAttr('disabled');

  protected comparator = (a: string, b: string) => (a === b) && !this.uploadInProgress;

  public get component(): UploadComponent {
    return this.name as UploadComponent;    
  }

  private uploadInProgress: boolean = false;
  private _upload: Upload;
  public get upload(): Upload {
    if (!this._upload) {
      this._upload = new Upload();
    }
    return this._upload;
  }


  public configure(token: string) {
    this.upload.getUID = (file: File, event: Event) => {
      return `publication-${this.component}-${token}`;
    };
    this.upload.onChecksumProgress = (progress: ProgressSummary) => {
      const progressBar = this.$entryPoint.find('div.progress-bar-1');
      const isEndpoint = progress.percentage === '0%' || progress.percentage === '100%';
      progressBar.toggleClass('progress-easing', !isEndpoint);
      progressBar.width(progress.percentage);
    };
    this.upload.onUploadProgress = (progress: ProgressSummary) => {
      const progressBar = this.$entryPoint.find('div.progress-bar-2');
      const isEndpoint = progress.percentage === '0%' || progress.percentage === '100%';
      progressBar.toggleClass('progress-easing', !isEndpoint);
      progressBar.width(progress.percentage);
    };
    this.upload.onProgressMessage = (message: string) => {
    };

    this.upload.onFileAdded = (resumableFile: any) => {
      this.$entryPoint.find('input.file-upload').val(resumableFile.fileName);
    };
    this.upload.onFileSuccess = (fileGUID: string) => {
      this.value = fileGUID;
    };
    this.upload.onStateChange = (alertUnload: boolean, cancelable: boolean) => {
      this.uploadInProgress = alertUnload;

      if (cancelable) {
        this.$entryPoint.find('upload-icon').hide();
        this.$entryPoint.find('cancel-icon').show();
      } else {
        this.$entryPoint.find('cancel-icon').hide();
        this.$entryPoint.find('upload-icon').show();
      }
    };

    // Clone the input to clear any event listeners
    const clickableElement = this.$entryPoint.find('label')[0];
    const $clonedInput = $(clickableElement.cloneNode(true));
    $clonedInput.find('input[type="file"]').remove();
    $(clickableElement).replaceWith($clonedInput);

    this.upload.assignBrowse(this.$entryPoint.find('label')[0]);
  }

  public reset() {
    super.reset();
    this.$entryPoint.find('input.file-upload').val('');
    this.$entryPoint.find('img').removeAttr('src');
    if (this.upload) {
      this.upload.reset();
    }
  }
}
