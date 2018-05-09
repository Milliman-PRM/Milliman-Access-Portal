import * as $ from 'jquery';
import { randomBytes } from 'crypto';
import { FormElement } from './form-element';
import { AccessMode, SubmissionMode } from './form-modes';
import { EntityFormSection, EntityFormSubmission } from './form-section';
import { confirmAndContinueForm } from '../shared';
import { EntityFormFileUploadInput } from './form-input/file-upload';
import { PublicationComponent } from '../content-publishing/publication-upload';

export class EntityForm extends FormElement {
  private _mode: AccessMode;
  public get mode(): AccessMode {
    return this._mode;
  }
  public set mode(mode: AccessMode) {
    confirmAndContinueForm(() => {
      this.sections.forEach((section) => {
        section.inputs.forEach((input) => {
          input.reset();
          input.setMode(mode);
        });
      });
      this.submission.modified = this.modified;
    }, mode === AccessMode.Read && this.modified);
    this._mode = mode;
  }
  private _token: string;
  public get token(): string {
    if (!this._token) {
      this._token = randomBytes(8).toString('hex');
    }
    return this._token;
  }
  sections: Array<EntityFormSection>;
  submission: EntityFormSubmission;

  _cssClasses = {
    main: 'admin-panel-content',
    title: '',
    extension: 'form-section-container',
  };

  constructor() {
    super();
  }

  public bind(entryPoint: HTMLElement) {
    super.bind(entryPoint);

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
        x.section.bind(x.element);
        return x.section;
      });
    this.submission = childElements
      .map((x: HTMLElement) => ({
        submission: new EntityFormSubmission(),
        element: x,
      }))
      .filter((x) => $(x.element).is(`.${x.submission.cssClasses.main}`))
      .map((x) => {
        x.submission.bind(x.element);
        return x.submission;
      })[0];

    // record original input values
    // attach event listeners
    this.sections.forEach((section) => {
      section.inputs.forEach((input) => {
        input.recordOriginalValue();
        input.onChange(() => {
          this.submission.modified = this.modified;
        });
      });
    });
    this.submission.onReset(() => {
      confirmAndContinueForm(() => {
        this.sections.forEach((section) => {
          section.inputs.forEach((input) => {
            input.reset();
          });
        });
        this.submission.modified = this.modified;
      });
    });
  }

  public configure(submitGroups: Array<EntityFormSubmissionGroup>, submissionMode: SubmissionMode) {
    this.sections.forEach((section) => {
      section.inputs
        .filter((input) => input instanceof EntityFormFileUploadInput)
        .forEach((upload) => {
          const uploadInput = upload as EntityFormFileUploadInput;
          uploadInput.configure(this.token);
        });
    });

    this.submission.onSubmit(() => {
      let requests: Array<() => void> = [];
      for (let i = 0; i < submitGroups.length; i += 1) {
        requests.push(() => $.post({
          url: submitGroups[i].url,
          data: this.serialize(submitGroups[i].sections),
        }).done((response) => {
          if (i + 1 < submitGroups.length) {
            requests[i + 1]();
          } else {
          }
        }).fail((response) => {
        }).always((response) => {
        }));
      }
      requests[0]();
    });

    this.submission.mode = submissionMode;
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

export class EntityFormSubmissionGroup {
  sections: Array<string>;
  url: string;
}

enum EntityFormValidationType {
  Any,
  Email,
  Domain,
  Phone,
}
