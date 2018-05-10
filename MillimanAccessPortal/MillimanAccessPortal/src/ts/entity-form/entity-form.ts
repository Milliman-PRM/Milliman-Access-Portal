import * as $ from 'jquery';
import { randomBytes } from 'crypto';
import { FormElement } from './form-element';
import { AccessMode } from './form-modes';
import { EntityFormSection, EntityFormSubmissionSection } from './form-section';
import { confirmAndContinueForm } from '../shared';
import { EntityFormFileUploadInput } from './form-input/file-upload';
import { PublicationComponent } from '../content-publishing/publication-upload';
import { EntityFormSubmissionGroup } from './form-submission';

export class EntityForm extends FormElement {
  private _accessMode: AccessMode;
  public get accessMode(): AccessMode {
    return this._accessMode;
  }
  public set accessMode(mode: AccessMode) {
    confirmAndContinueForm(() => {
      this.sections.forEach((section) => {
        section.inputs.forEach((input) => {
          input.reset();
          input.setMode(mode);
        });
      });
      this.submissionSection.submissions
        .forEach((submission) => submission.modified = this.modified);
    }, mode === AccessMode.Read && this.modified);
    this._accessMode = mode;
  }
  private _submissionModes: Array<string> = [];
  private _submissionMode: string;
  public get submissionMode(): string {
    return this._submissionMode;
  }
  public set submissionMode(submissionMode: string) {
    if (this._submissionModes.indexOf(submissionMode) === -1) {
      throw new Error(`Error setting mode: mode '${submissionMode}' does not exist for this form.`);
    }
    this.submissionSection.submissions
      .forEach((submission) => submission.submissionMode = submissionMode);
    this._submissionMode = this.submissionMode;
  }
  private _token: string;
  public get token(): string {
    if (!this._token) {
      this._token = randomBytes(8).toString('hex');
    }
    return this._token;
  }
  sections: Array<EntityFormSection>;
  submissionSection: EntityFormSubmissionSection;

  _cssClasses = {
    main: 'admin-panel-content',
    title: '',
    extension: 'form-section-container',
  };

  constructor() {
    super();
  }

  public bindToDOM(entryPoint: HTMLElement) {
    super.bindToDOM(entryPoint);

    const childElements = this.$entryPoint
      .find(`.${this.cssClasses.extension}`).children().toArray();

    // locate and bind to section-level DOM elements
    this.sections = childElements
      .map((x: HTMLElement) => ({
        section: new EntityFormSection(),
        element: x,
      }))
      .filter((x) => $(x.element).is(`.${x.section.cssClasses.main}`))
      .map((x) => {
        x.section.bindToDOM(x.element);
        return x.section;
      });
    this.submissionSection = childElements
      .map((x: HTMLElement) => ({
        section: new EntityFormSubmissionSection(),
        element: x,
      }))
      .filter((x) => $(x.element).is(`.${x.section.cssClasses.main}`))
      .map((x) => {
        x.section.bindToDOM(x.element);
        return x.section;
      })[0];

    // record original input values
    // attach event listeners
    this.sections.forEach((section) => {
      section.inputs.forEach((input) => {
        input.recordOriginalValue();
        input.onChange(() => {
          this.submissionSection.submissions
            .forEach((submission) => submission.modified = this.modified);
        });
      });
    });
  }

  public unbindFromDOM() {
    super.unbindFromDOM();
    this.sections.forEach((section) => section.unbindFromDOM());
  }

  public configure(groups: Array<{group: EntityFormSubmissionGroup<any>, mode: string}>) {
    this._submissionModes = groups.map((group) => group.mode);
    
    // Configure form reset and submission
    this.submissionSection.submissions
      .forEach((submission) => submission.setCallbacks(groups, this));

    // Create upload objects
    this.sections.forEach((section) => {
      section.inputs
        .filter((input) => input instanceof EntityFormFileUploadInput)
        .forEach((upload) => {
          const uploadInput = upload as EntityFormFileUploadInput;
          uploadInput.configure(this.token);
        });
    });
  }

  get modified() {
    return this.sections
      .map((section) => section.modified)
      .reduce((cum, cur) => cum || cur, false);
  }

  serialize(sections?: Array<string>): string {
    const allInputs = this.$entryPoint.serializeArray();
    const filteredInputs = (() => {
      return sections
        ? allInputs.filter((input) =>
          this.sections
            .map((section) =>
              sections.indexOf(section.section) > -1
              && section.hasInput(input.name))
            .reduce((cum, cur) => cum || cur, false))
        : allInputs;
    })();
    return filteredInputs
      .map((kvp) => `${kvp.name}=${kvp.value}`)
      .join('&');
  }
}
