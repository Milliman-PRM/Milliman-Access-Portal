import * as toastr from 'toastr';

import { ProgressSummary } from '../../upload/progress-monitor';
import { Upload, UploadComponent } from '../../upload/upload';
import { AccessMode } from '../form-modes';
import { FormInput } from './input';

import 'toastr/toastr.scss';

export class FileUploadInput extends FormInput {
  protected _cssClasses = {
    main: 'form-input-file-upload',
    title: 'form-input-file-upload-title',
    extension: 'form-input-file-upload-contents',
  };

  private uploadInProgress: boolean = false;
  private _upload: Upload;
  public get upload(): Upload {
    if (!this._upload) {
      this._upload = new Upload();
    }
    return this._upload;
  }

  private _deletable: boolean;
  private get deletable(): boolean {
    if (!this._deletable) {
      this._deletable = this.$entryPoint.hasClass('deletable');
    }
    return this._deletable;
  }

  private cancelable: boolean = false;

  private originalName: string;

  public configure(token: string) {
    this.upload.setFileTypes(fileTypes.get(this.component));

    this.upload.getUID = () => {
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
    this.upload.onProgressMessage = () => undefined;
    this.upload.onError = (message: string) => {
      const errorBar = this.$entryPoint.find('div.progress-bar-3');
      errorBar.width('100%');
      toastr.error(message);
    };

    this.upload.onFileAdded = (resumableFile: any) => {
      this.originalName = resumableFile.fileName;
      this.$entryPoint.find('input.file-upload').val(this.originalName);
      if (this.component === UploadComponent.Image) {
        const reader = new FileReader();
        reader.onload = () => {
          this.$entryPoint.find('img.image-preview').attr('src', reader.result.toString());
        };
        reader.readAsDataURL(resumableFile.file);
      }
    };
    this.upload.onFileSuccess = (fileGUID: string) => {
      this.value = `${this.originalName}~${fileGUID}`;
      toastr.success('File uploaded successfully.');
    };
    this.upload.onStateChange = (alertUnload: boolean) => {
      this.uploadInProgress = alertUnload;
      this.setCancelable(alertUnload);
      this.$entryPoint.change(); // trigger a change event
    };

    // Clone the input to clear any event listeners
    const clickableElement = this.$entryPoint.find('label')[0];
    const $clonedInput = $(clickableElement.cloneNode(true));
    $clonedInput.find('input[type="file"]').remove();
    $clonedInput.find('.file-upload').data($(clickableElement).find('.file-upload').data());
    $(clickableElement).replaceWith($clonedInput);

    this.upload.assignBrowse(this.$entryPoint.find('label')[0]);
    this.$entryPoint.find('.cancel-icon').click((event) => {
      event.stopPropagation();
      if (this.cancelable) {
        this.upload.cancel();
        this.$entryPoint.find('div.progress-bar-3').width('0');
        this.reset();
      } else {
        this.value = `${this.originalName}~delete`;
        this.$entryPoint.find('input.file-upload').val('[deleted]');
        toastr.success('File marked for deletion.');
      }
    });
  }

  public reset() {
    super.reset();
    const $fileUpload = this.$entryPoint.find('input.file-upload');
    const fileUploadData = $fileUpload.data();
    $fileUpload.val(fileUploadData && fileUploadData.originalName || '');
    this.$entryPoint.find('img.image-preview').removeAttr('src');
    if (this.upload) {
      this.upload.reset();
    }
    this.$entryPoint.change(); // trigger a change event
  }

  protected findInput = ($entryPoint: JQuery<HTMLElement>) => $entryPoint.find('input.file-upload-guid');

  protected getValueFn = ($input: JQuery<HTMLElement>) => $input.val;
  protected setValueFn = ($input: JQuery<HTMLElement>) => $input.val;

  protected disable = ($input: JQuery<HTMLElement>) => $input
    .parent().find('*').not('input.file-upload-guid').attr('disabled', '')
  protected enable = ($input: JQuery<HTMLElement>) => $input
    .parent().find('*').not('input.file-upload-guid').removeAttr('disabled')

  protected comparator = (a: string, b: string) => (a === b) && !this.uploadInProgress;

  protected validFn = () => {
    const uploadValid = (this.upload && this.upload.valid());
    const deleteValid = this.value.endsWith('delete') || undefined;
    return uploadValid || deleteValid;
  }

  public get component(): UploadComponent {
    return this.name as UploadComponent;
  }

  private setCancelable(cancelable: boolean) {
    if (cancelable) {
      this.setAccessMode(AccessMode.WriteDisabled);
      this.$entryPoint.find('.upload-icon').hide();
      this.$entryPoint.find('.cancel-icon').show();
      this.$entryPoint.find('.cancel-icon').removeAttr('disabled');
      this.$entryPoint.find('.progress-bars').css('visibility', 'visible');
    } else {
      if (this.accessMode === AccessMode.WriteDisabled) {
        this.setAccessMode(AccessMode.Write);
      }
      this.$entryPoint.find('.upload-icon').show();
      if (!this.deletable
          || this.$entryPoint.find('.file-upload').val() === ''
          || this.value.endsWith('delete')) {
        this.$entryPoint.find('.cancel-icon').hide();
      }
      this.$entryPoint.find('.progress-bars').css('visibility', 'hidden');
    }
    this.cancelable = cancelable;
  }
}

const fileTypes = new Map<UploadComponent, string[]>([
  [UploadComponent.Image, ['jpg', 'jpeg', 'png', 'gif']],
  [UploadComponent.Content, ['qvw']],
  [UploadComponent.UserGuide, ['pdf']],
  [UploadComponent.ReleaseNotes, ['pdf']],
]);
