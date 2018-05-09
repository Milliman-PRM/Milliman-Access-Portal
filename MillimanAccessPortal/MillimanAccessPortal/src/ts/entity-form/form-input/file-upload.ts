import { EntityFormInput } from './input';
import { Upload } from '../../upload/upload';
import { PublicationUpload, PublicationComponent } from '../../content-publishing/publication-upload';

export class EntityFormFileUploadInput extends EntityFormInput {
  _cssClasses = {
    main: 'form-input-file-upload',
    title: 'form-input-file-upload-title',
    extension: 'form-input-file-upload-contents',
  }
  private readonly componentMap: Map<string, PublicationComponent>;

  protected findInput = ($entryPoint: JQuery<HTMLElement>) => $entryPoint.find('input.file-upload-guid');

  protected getValueFn = ($input: JQuery<HTMLElement>) => $input.val;
  protected setValueFn = ($input: JQuery<HTMLElement>) => $input.val;

  protected disable = ($input: JQuery<HTMLElement>) => $input.attr('disabled', '');
  protected enable = ($input: JQuery<HTMLElement>) => $input.removeAttr('disabled');

  protected comparator = (a: string, b: string) => a === b;

  private token: string;
  private component: PublicationComponent;
  private _upload: PublicationUpload;
  public get upload(): PublicationUpload {
    return this._upload;
  }
  protected createUpload() {
    // TODO: generalize for other upload types
    this._upload = this.token && this.component && new PublicationUpload(
      this.$entryPoint[0],
      () => {},
      this.token,
      this.component,
    );
  }

  constructor() {
    super();

    this.componentMap = new Map<string, PublicationComponent>();
    this.componentMap.set('ContentFile', PublicationComponent.Content);
    this.componentMap.set('ThumbnailImage', PublicationComponent.Image);
    this.componentMap.set('ReleaseNotesFile', PublicationComponent.ReleaseNotes);
    this.componentMap.set('UserguideFile', PublicationComponent.UserGuide);
  }

  public bind(entryPoint: HTMLElement) {
    super.bind(entryPoint);

    this.component = this.componentMap.get(this.name);
  }

  public configure(token: string) {
    this.token = token;
    this.createUpload();
  }
}
