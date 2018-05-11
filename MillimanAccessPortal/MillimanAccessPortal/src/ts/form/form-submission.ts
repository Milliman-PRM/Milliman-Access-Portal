import * as toastr from 'toastr';
import { showButtonSpinner, hideButtonSpinner } from '../shared';
import { FormBase } from './form-base';
import { FormElement } from "./form-element";
import { confirmAndContinueForm } from "../shared";
import { SubmissionMode } from './form-modes';

export class Submission extends FormElement {
  protected _cssClasses = {
    main: 'button-container',
    title: '',
    extension: '',
  };

  private _disabled = false;
  private _submissionMode: SubmissionMode;
  public get submissionMode(): SubmissionMode {
    return this._submissionMode;
  };
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

  public setCallbacks(modes: Array<{group: SubmissionGroup<any>, name: string}>, form: FormBase) {
    // Find the first group (if any) that matches
    const filteredGroups = modes.filter((group) =>
      this.$entryPoint.is(`.button-container-${group.name}`));
    if (!filteredGroups.length) {
      return;
    }
    const group = filteredGroups[0];

    // Set submit callback
    this.$entryPoint
      .find('.button-submit')
      .off('click')
      .on('click', () => group.group.submit(form));

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
  constructor(
    readonly sections: Array<string>,
    readonly url: string,
    readonly method: string,
    readonly callback: (response: T, form?: FormBase) => void,
  ) { }

  public chain<U>(that: SubmissionGroup<U>): SubmissionGroup<T> {
    const chainedGroup = new SubmissionGroup<T>(
      this.sections,
      this.url,
      this.method,
      (response: T, form: FormBase) => {
        this.callback(response);
        that.submit(form);
      },
    );
    return chainedGroup;
  }

  public submit(form: FormBase) {
    showButtonSpinner($(`.button-container-${form.submissionMode} .button-submit`));
    $.ajax({
      method: this.method,
      url: this.url,
      data: form.serialize(this.sections),
      headers: {
        RequestVerificationToken: form.antiforgeryToken,
      },
    }).done((response: T) => {
      this.callback(response, form);
    }).fail((response) => {
      toastr.warning(response.getResponseHeader('warning'));
      // TODO: do something on fail
    }).always(() => {
      hideButtonSpinner($(`.button-container-${form.submissionMode} .button-submit`));
    });
  }
}
