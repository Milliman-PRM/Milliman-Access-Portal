import { randomBytes } from 'crypto';
import * as $ from 'jquery';

import { confirmAndContinueForm } from '../shared';
import { FormElement } from './form-element';
import { FileUploadInput } from './form-input/file-upload';
import { NullableTextareaInput } from './form-input/nullable-textarea';
import { AccessMode, SubmissionMode } from './form-modes';
import { FormInputSection, FormSubmissionSection } from './form-section';

export class FormBase extends FormElement {
  public inputSections: FormInputSection[];
  public submissionSection: FormSubmissionSection;

  protected _cssClasses = {
    main: 'admin-panel-content',
    title: '',
    extension: 'form-section-container',
  };

  private _accessMode: AccessMode;
  public get accessMode(): AccessMode {
    return this._accessMode;
  }
  public set accessMode(mode: AccessMode) {
    confirmAndContinueForm(() => {
      this.inputSections.forEach((section) => {
        section.inputs.forEach((input) => {
          input.reset();
        });
        section.setMode(mode, this._submissionMode);
      });
      this.submissionSection.submissions
        .forEach((submission) => submission.modified = this.modified);
      this.resetValidation();
    }, mode === AccessMode.Read && this.modified);
    this._accessMode = mode;
  }

  private _submissionModes: SubmissionMode[] = [];
  private _submissionMode: SubmissionMode;
  public get submissionMode(): string {
    return this._submissionMode
      ? this._submissionMode.name
      : null;
  }
  public set submissionMode(submissionMode: string) {
    const filtered = this._submissionModes.filter((mode) => mode.name === submissionMode);
    if (filtered.length === 0) {
      throw new Error(`Error setting mode: mode '${submissionMode}' does not exist for this form.`);
    }
    this.submissionSection.submissions
      .forEach((submission) => submission.submissionMode = filtered[0]);
    this._submissionMode = filtered[0];
  }

  private _token: string;
  public get token(): string {
    if (!this._token) {
      this._token = randomBytes(8).toString('hex');
    }
    return this._token;
  }

  public constructor(readonly defaultWelcomeText: string = '') {
    super();
  }

  public bindToDOM(entryPoint: HTMLElement) {
    super.bindToDOM(entryPoint);

    const childElements = this.$entryPoint
      .find(`.${this.cssClasses.extension}`).children().toArray();

    // locate and bind to section-level DOM elements
    this.inputSections = childElements
      .map((x: HTMLElement) => ({
        element: x,
        section: new FormInputSection(this.defaultWelcomeText),
      }))
      .filter((x) => $(x.element).is(`.${x.section.cssClasses.main}`))
      .map((x) => {
        const orFlag = $(x.element).is('.form-section-valid-or');
        x.section.validReduce = {
          fn: orFlag ? (cum, cur) => cum || cur : (cum, cur) => cum && cur,
          init: !orFlag,
        };
        x.section.bindToDOM(x.element);
        return x.section;
      });
    this.submissionSection = this.submissionSection || childElements
      .map((x: HTMLElement) => ({
        element: x,
        section: new FormSubmissionSection(),
      }))
      .filter((x) => $(x.element).is(`.${x.section.cssClasses.main}`))
      .map((x) => {
        x.section.bindToDOM(x.element);
        return x.section;
      })[0];

    // record original input values
    // attach event listeners
    this.inputSections.forEach((section) => {
      section.inputs.forEach((input) => {
        input.recordOriginalValue();
        input.onChange(() => {
          const modified = this.modified;
          this.submissionSection.submissions
            .forEach((submission) => {
              submission.modified = modified;
              const sections = submission.submissionMode
                ? submission.submissionMode.groups
                  .map((group) => group.sections)
                  .reduce((cum, cur) => cum.concat(cur), [])
                : [];
              submission.valid = this.valid(sections);
            });
        });
      });
    });
  }

  public configure(modes: SubmissionMode[]) {
    this._submissionModes = modes;
    // Configure form reset and submission

    this.submissionSection.submissions
      .forEach((submission) => submission.setCallbacks(modes, this));

    // Create upload objects, configure special inputs
    this.inputSections.forEach((section) => {
      section.inputs
        .filter((input) => input instanceof FileUploadInput)
        .forEach((upload) => {
          const uploadInput = upload as FileUploadInput;
          uploadInput.configure(this.token);
        });
      section.inputs
        .filter((input) => input instanceof NullableTextareaInput)
        .forEach((textarea) => {
          const nullableTextareaInput = textarea as NullableTextareaInput;
          nullableTextareaInput.configure();
        });
    });
  }

  public get modified() {
    return this.inputSections
      .map((section) => section.modified)
      .reduce((cum, cur) => cum || cur, false);
  }

  public valid(sections: string[] = []) {
    return this.inputSections
      .filter((section) => sections.length === 0 || sections.indexOf(section.name) > -1)
      .map((section) => section.valid)
      .reduce((cum, cur) => cum && cur, true);
  }

  public validate() {
    this.$entryPoint.validate();
  }

  public resetValidation() {
    this.$entryPoint
      .find('.input-validation-error').removeClass('input-validation-error');
    this.$entryPoint
      .find('span.field-validation-error > span').remove();
  }

  public serialize(sectionNames: string[]): string {
    const allInputs = this.$entryPoint.serializeArray();
    const filteredInputs = (() => {
      return sectionNames
        ? allInputs.filter((input) =>
          this.inputSections
            .map((section) =>
              sectionNames.indexOf(section.name) > -1
              && section.hasInput(input.name))
            .reduce((cum, cur) => cum || cur, false))
        : allInputs;
    })();
    return filteredInputs
      .map((kvp) => `${kvp.name}=${kvp.value}`)
      .join('&');
  }

  public get antiforgeryToken() {
    return this.$entryPoint.find('.form-antiforgery-token input').val().toString();
  }
}
