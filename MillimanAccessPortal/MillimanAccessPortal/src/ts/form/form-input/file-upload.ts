import { FormInput } from './input';
import { Upload } from '../../upload/upload';
import { PublicationUpload, PublicationComponent } from '../../content-publishing/publication-upload';

export class FileUploadInput extends FormInput {
  protected _cssClasses = {
    main: 'form-input-file-upload',
    title: 'form-input-file-upload-title',
    extension: 'form-input-file-upload-contents',
  }
  private readonly componentMap: Map<string, PublicationComponent>;

  protected findInput = ($entryPoint: JQuery<HTMLElement>) => $entryPoint.find('input.file-upload-guid');

  protected getValueFn = ($input: JQuery<HTMLElement>) => $input.val;
  protected setValueFn = ($input: JQuery<HTMLElement>) => $input.val;

  protected disable = ($input: JQuery<HTMLElement>) => $input.attr('disabled', '').prev().attr('disabled', '');
  protected enable = ($input: JQuery<HTMLElement>) => $input.removeAttr('disabled').prev().removeAttr('disabled');

  protected comparator = (a: string, b: string) => (a === b) && !this.uploadInProgress;

  private _component: PublicationComponent;
  public get component(): PublicationComponent {
    return this._component;
  }

  private uploadInProgress: boolean = false;
  private _upload: PublicationUpload;
  public get upload(): PublicationUpload {
    return this._upload;
  }
  protected createUpload(token: string) {
    // TODO: generalize for other upload types
    this._upload = token && this.component && new PublicationUpload(
      this.$entryPoint[0],
      (a: boolean) => this.uploadInProgress = a, 
      (guid: string) => this.value = guid,
      token,
      this.component,
    );
  }

  public constructor() {
    super();

    this.componentMap = new Map<string, PublicationComponent>();
    this.componentMap.set('ContentFile', PublicationComponent.Content);
    this.componentMap.set('ThumbnailImage', PublicationComponent.Image);
    this.componentMap.set('ReleaseNotesFile', PublicationComponent.ReleaseNotes);
    this.componentMap.set('UserguideFile', PublicationComponent.UserGuide);
  }

  public bindToDOM(entryPoint: HTMLElement) {
    super.bindToDOM(entryPoint);

    this._component = this.componentMap.get(this.name);
    if (this.upload) {
      this.upload.attachToBrowseElement(this.$entryPoint[0])
    }
  }

  public configure(token: string) {
    this.createUpload(token);
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
