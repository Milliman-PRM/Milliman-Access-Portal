import { FormInput } from './input';
import { Upload, UploadComponent } from '../../upload/upload';
import { PublicationUpload } from '../../content-publishing/publication-upload';

export class FileUploadInput<T extends Upload> extends FormInput {
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
  private _upload: PublicationUpload;
  public get upload(): PublicationUpload {
    return this._upload;
  }

  public bindToDOM(entryPoint?: HTMLElement) {
    super.bindToDOM(entryPoint);

    if (this.upload) {
      this.upload.attachToBrowseElement(this.$entryPoint[0]);
    }
  }

  public configure(token: string) {
    this._upload = new PublicationUpload(
      this.$entryPoint[0],
      (a: boolean) => this.uploadInProgress = a, 
      (guid: string) => this.value = guid,
      token,
      this.component,
    );
    this.upload.attachToBrowseElement(this.$entryPoint[0]);
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
