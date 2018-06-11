import * as toastr from 'toastr';

import { confirmAndContinueForm, hideButtonSpinner, showButtonSpinner } from '../shared';
import { FormBase } from './form-base';
import { FormElement } from './form-element';
import { SubmissionMode } from './form-modes';

export class Submission extends FormElement {
  // tslint:disable:object-literal-sort-keys
  protected _cssClasses = {
    main: 'button-container',
    title: '',
    extension: '',
  };
  // tslint:enable:object-literal-sort-keys

  private _disabled = false;
  private _submissionMode: SubmissionMode;
  public get submissionMode(): SubmissionMode {
    return this._submissionMode;
  }
  public set submissionMode(submissionMode: SubmissionMode) {
    this._submissionMode = submissionMode;
    if (this.$entryPoint.is(this.activeButtonSelector)) {
      this.$entryPoint.find('button').show();
    } else {
      this.$entryPoint.find('button').hide();
    }
  }
  private get activeButtonSelector(): string {
    return `.button-container-${this.submissionMode.name}`;
  }

  public get modified() {
    return false;
  }
  public set modified(value: boolean) {
    if (value) {
      this.$entryPoint.show();
    } else {
      this.$entryPoint.hide();
    }
    this._disabled = value;
  }

  public setCallbacks(modes: SubmissionMode[], form: FormBase) {
    // Find the first group (if any) that matches
    const singleMode = modes.filter((group) =>
      this.$entryPoint.is(`.button-container-${group.name}`));
    if (!singleMode.length) {
      return;
    }
    const mode = singleMode[0];

    // Set submit callback
    this.$entryPoint
      .find('.button-submit')
      .off('click')
      .on('click', async () => {
        for (const group of mode.groups) {
          await group.submit(form, mode.sparse);
        }
      });

    // Set reset callback
    this.$entryPoint
      .find('.button-reset')
      .off('click')
      .on('click', () => confirmAndContinueForm(() => {
        form.inputSections.forEach((section) => {
          section.inputs.forEach((input) => {
            input.reset();
          });
        });
        this.modified = form.modified;
      }));
  }
}

export class SubmissionGroup<T> {
  public static FinalGroup<T>(callback: (response: T) => void = () => undefined): SubmissionGroup<T> {
    const group = new SubmissionGroup<T>(
      undefined,
      null,
      null,
      callback,
    );
    return group;
  }

  public callback: (response: T, form?: FormBase) => void;
  constructor(
    readonly sections: string[],
    readonly url: string,
    readonly method: string,
    callback: (response: T, form?: FormBase) => void,
    readonly transform: (data: string) => any = (data) => data,
  ) {
    this.callback = callback;
  }

  public submit(form: FormBase, sparse: boolean = false): Promise<any> {
  // showButtonSpinner($(`.button-container-${form.submissionMode} .button-submit`));
  // hideButtonSpinner($(`.button-container-${form.submissionMode} .button-submit`));

    if (sparse) {
      const modified = form.inputSections
        .filter((inputSection) => this.sections === undefined || this.sections.indexOf(inputSection.name) !== -1)
        .map((inputSection) => inputSection.modified)
        .reduce((cum, cur) => cum || cur, false);
      if (!modified) {
        return;
      }
    }

    return new Promise((resolve, reject) => {
      $.ajax({
        data: this.transform(form.serialize(this.sections)),
        headers: {
          RequestVerificationToken: form.antiforgeryToken,
        },
        method: this.method,
        url: this.url,
      }).done(async (response: T) => {
        await this.callback(response);
        resolve();
      }).fail((response) => {
        toastr.warning(response.getResponseHeader('Warning')
          || 'An unknown error has occurred.');
        reject();
      });
    });
  }
}
