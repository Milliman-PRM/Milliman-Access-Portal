import * as toastr from 'toastr';

import { confirmAndContinueForm } from '../shared';
import { FormBase } from './form-base';
import { FormElement } from './form-element';
import { AccessMode, SubmissionMode } from './form-modes';

export class Submission extends FormElement {
  public form: FormBase;

  protected _cssClasses = {
    main: 'button-container',
    title: '',
    extension: '',
  };

  private _accessMode: AccessMode;
  public get accessMode(): AccessMode {
    return this._accessMode;
  }
  public set accessMode(mode: AccessMode) {
    this._accessMode = mode;
    if (this.anyValidAndModified()) {
      this.$entryPoint.find('button').removeAttr('disabled');
    } else {
      this.$entryPoint.find('button').attr('disabled', '');
    }
  }
  private _submissionMode: SubmissionMode;
  public get submissionMode(): SubmissionMode {
    return this._submissionMode;
  }
  public set submissionMode(submissionMode: SubmissionMode) {
    this._submissionMode = submissionMode;
    if (this.$entryPoint.is(this.activeButtonSelector)) {
      this.$entryPoint.show();
    } else {
      this.$entryPoint.hide();
    }
  }
  private get activeButtonSelector(): string {
    return `.button-container-${this.submissionMode.name}`;
  }

  private _modified: boolean;
  public get modified() {
    return this._modified;
  }
  public set modified(value: boolean) {
    this._modified = value;
    if (this.anyValidAndModified()) {
      this.$entryPoint.find('button').removeAttr('disabled');
    } else {
      this.$entryPoint.find('button').attr('disabled', '');
    }
  }

  private _valid: boolean;
  public get valid() {
    return this._valid;
  }
  public set valid(value: boolean) {
    this._valid = value;
    if (this.anyValidAndModified()) {
      this.$entryPoint.find('button').removeAttr('disabled');
    } else {
      this.$entryPoint.find('button').attr('disabled', '');
    }
  }

  public setCallbacks(modes: SubmissionMode[], form: FormBase) {
    // Find the first group (if any) that matches
    const singleMode = modes.filter((group) =>
      this.$entryPoint.is(`.button-container-${group.name}`));
    if (!singleMode.length) {
      return;
    }
    const mode = singleMode[0];

    this.form = form;

    // Set submit callback
    this.$entryPoint
      .find('.button-submit')
      .off('click')
      .on('click', async (event) => {
        event.preventDefault();
        // show button spinner
        const $button = this.$entryPoint.find('button.button-submit');
        if ($button.find('.spinner-small').length) { return; }
        $button.data('originalText', $button.html());
        $button.html($button.data().submitText || 'Submitting');
        $button.append('<div class="spinner-small"></div>');
        $button.attr('disabled', '');
        try {
          for (const group of mode.groups) {
            await group.submit(form, mode.sparse);
          }
        } finally {
          // hide button spinner
          $button.html($button.data().originalText);
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

  private anyValidAndModified(): boolean {
    if (!this.submissionMode || !this.form) {
      return false;
    }
    return this.submissionMode.groups.map((group) => {
      const modified = this.form.inputSections
        .filter((inputSection) => group.sections === undefined || group.sections.indexOf(inputSection.name) !== -1)
        .map((inputSection) => inputSection.modified)
        .reduce((cum, cur) => cum || cur, false);
      const valid = this.form.valid(group.sections);
      return modified && valid;
    }).reduce((cum, cur) => this.submissionMode.sparse ? cum || cur : cum && cur, !this.submissionMode.sparse)
    && (this._accessMode === AccessMode.Defer || this._accessMode === AccessMode.Write);
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
      const modified = this.url === null || form.inputSections
        .filter((inputSection) => this.sections === undefined || this.sections.indexOf(inputSection.name) !== -1)
        .map((inputSection) => inputSection.modified)
        .reduce((cum, cur) => cum || cur, false);
      if (!modified) {
        return;
      }
    }

    form.validate();

    if (this.url === null) {
      return new Promise(async (resolve) => {
        await this.callback(null);
        resolve();
      });
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
