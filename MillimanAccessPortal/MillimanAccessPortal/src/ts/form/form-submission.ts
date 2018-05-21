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
  private sparse: boolean = false;
  private next: SubmissionGroup<any> = null;
  private get lastChain(): SubmissionGroup<any> {
    let next = this as SubmissionGroup<any>;
    while (next.next !== null) next = next.next;
    return next;
  }
  public callback: (response: T, form?: FormBase) => void;
  constructor(
    readonly sections: Array<string>,
    readonly url: string,
    readonly method: string,
    callback: (response: T, form?: FormBase) => void,
    readonly transform: (data: string) => any = (data) => data,
  ) {
    this.callback = callback;
  }

  public static FinalGroup<T>(callback: (response: T) => void = () => {}): SubmissionGroup<T> {
    const group = new SubmissionGroup<T>(
      [],
      null,
      null,
      callback,
    );
    group.sparse = true;
    return group;
  }

  public chain<U>(that: SubmissionGroup<U>, sparse: boolean = false): SubmissionGroup<T> {
    const lastChain = this.lastChain;
    lastChain.next = that ? that : SubmissionGroup.FinalGroup();

    const originalCallback = lastChain.callback;
    lastChain.callback = (response: T, form: FormBase) => {
      if (response !== null) {
        originalCallback(response, form);
      }
      lastChain.next.submit(form);
    };
    lastChain.sparse = sparse;

    return this;
  }

  public submit(form: FormBase) {
    showButtonSpinner($(`.button-container-${form.submissionMode} .button-submit`));

    const modified = form.inputSections
      .filter((inputSection) => this.sections.indexOf(inputSection.name) !== -1)
      .map((inputSection) => inputSection.modified)
      .reduce((cum, cur) => cum || cur, false);

    if (this.sparse && !modified) {
      // skip this request and go to the next
      this.callback(null, form);
      hideButtonSpinner($(`.button-container-${form.submissionMode} .button-submit`));
      return;
    }

    $.ajax({
      method: this.method,
      url: this.url,
      data: this.transform(form.serialize(this.sections)),
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
