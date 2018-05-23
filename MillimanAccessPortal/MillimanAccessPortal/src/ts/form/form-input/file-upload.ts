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

  protected disable = ($input: JQuery<HTMLElement>) => $input.parent().attr('disabled', '').children().attr('disabled', '');
  protected enable = ($input: JQuery<HTMLElement>) => $input.parent().removeAttr('disabled').children().removeAttr('disabled');

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
    };
    this.upload.onUploadProgress = (progress: ProgressSummary) => {
    };
    this.upload.onProgressMessage = (message: string) => {
    };

<<<<<<< HEAD
    this.upload.onFileAdded = (file: File) => {
    };
    this.upload.onFileSuccess = (fileGUID: string) => {
      this.value = fileGUID;
    };
    this.upload.onStateChange = (alertUnload: boolean, cancelable: boolean) => {
      this.uploadInProgress = alertUnload;
    };
=======
  public bindToDOM(entryPoint: HTMLElement) {
    super.bindToDOM(entryPoint);
>>>>>>> 00ff2eea... Remove unbind and rebind features

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
