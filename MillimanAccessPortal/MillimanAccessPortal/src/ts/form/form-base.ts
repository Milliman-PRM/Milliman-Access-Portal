import * as $ from 'jquery';
import { randomBytes } from 'crypto';
import { FormElement } from './form-element';
import { AccessMode, SubmissionMode } from './form-modes';
import { FormInputSection, FormSubmissionSection } from './form-section';
import { confirmAndContinueForm } from '../shared';
import { FileUploadInput } from './form-input/file-upload';
import { UploadComponent } from '../upload/upload';
import { SubmissionGroup } from './form-submission';

export class FormBase extends FormElement {
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
    }, mode === AccessMode.Read && this.modified);
    this._accessMode = mode;
  }

  private _submissionModes: Array<SubmissionMode> = [];
  private _submissionMode: SubmissionMode;
  public get submissionMode(): string {
    return this._submissionMode.name;
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

  public inputSections: Array<FormInputSection>;
  public submissionSection: FormSubmissionSection;

  protected _cssClasses = {
    main: 'admin-panel-content',
    title: '',
    extension: 'form-section-container',
  };

  public constructor() {
    super();
  }

  public bindToDOM(entryPoint?: HTMLElement) {
    super.bindToDOM(entryPoint);

    if (entryPoint) {
      const childElements = this.$entryPoint
        .find(`.${this.cssClasses.extension}`).children().toArray();

      // locate and bind to section-level DOM elements
      this.inputSections = childElements
        .map((x: HTMLElement) => ({
          section: new FormInputSection(),
          element: x,
        }))
        .filter((x) => $(x.element).is(`.${x.section.cssClasses.main}`))
        .map((x) => {
          x.section.bindToDOM(x.element);
          return x.section;
        });
      this.submissionSection = this.submissionSection || childElements
        .map((x: HTMLElement) => ({
          section: new FormSubmissionSection(),
          element: x,
        }))
        .filter((x) => $(x.element).is(`.${x.section.cssClasses.main}`))
        .map((x) => {
          x.section.bindToDOM(x.element);
          return x.section;
        })[0];
    } else {
      this.inputSections.forEach((section) => section.bindToDOM());
      this.submissionSection.bindToDOM();
    }

    // record original input values
    // attach event listeners
    this.inputSections.forEach((section) => {
      section.inputs.forEach((input) => {
        input.recordOriginalValue();
        input.onChange(() => {
          const modified = this.modified;
          this.submissionSection.submissions
            .forEach((submission) => submission.modified = modified);
        });
      });
    });
  }

  public unbindFromDOM() {
    super.unbindFromDOM();
    this.inputSections.forEach((section) => section.unbindFromDOM());
    this.submissionSection.unbindFromDOM();
  }

  public configure(modes: Array<SubmissionMode>) {
    this._submissionModes = modes;
    
    // Configure form reset and submission
    this.submissionSection.submissions
      .forEach((submission) => submission.setCallbacks(modes, this));

    // Create upload objects
    this.inputSections.forEach((section) => {
      section.inputs
        .filter((input) => input instanceof FileUploadInput)
        .forEach((upload) => {
          const uploadInput = upload as FileUploadInput;
          uploadInput.configure(this.token);
        });
    });
  }

  public get modified() {
    return this.inputSections
      .map((section) => section.modified)
      .reduce((cum, cur) => cum || cur, false);
  }

  public serialize(sectionNames: Array<string>): string {
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
