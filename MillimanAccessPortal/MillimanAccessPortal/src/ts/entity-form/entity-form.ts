import * as $ from 'jquery';
import { confirmAndContinueForm } from '../shared';


interface EntityFormClassRegistry {
  main: string;
  title: string;
  extension: string;
}

abstract class EntityFormElement {
  protected abstract get cssClasses(): EntityFormClassRegistry;
  protected readonly template: string;
  protected readonly $entryPoint: JQuery<HTMLElement>;

  protected constructor(entryPoint: HTMLElement) {
    this.$entryPoint = $(entryPoint);
  }

  protected abstract get children(): Array<EntityFormElement>;
  build(): JQuery<HTMLElement> {
    const $form = $(this.template);
    this.children.forEach((section) => {
      $form.find(`.${this.cssClasses.main}`).append(section.build());
    });
    return $form;
  }

  abstract get modified(): boolean;
}

export class EntityForm extends EntityFormElement {
  private _mode: EntityFormMode;
  sections: Array<EntityFormSection>;
  submission: EntityFormSubmission;

  readonly cssClasses = {
    main: 'admin-panel-content',
    title: '',
    extension: 'form-section-container',
  }
  readonly template = `
    <form class="${this.cssClasses.main}">
      <div class="${this.cssClasses.extension}"></div>
    </form>
    `

  constructor(readonly entryPoint: HTMLElement) {
    super(entryPoint);

    if (!$(entryPoint).is(`.${this.cssClasses.main}`)) {
      throw new Error(`Cannot bind form: element not of class ${this.cssClasses.main}`);
    }

    // bind members to DOM elements
    this.sections = $(entryPoint)
      .find(`.${this.cssClasses.extension}`).children()
      .toArray().map((section) => {
        try {
          return new EntityFormSection(section);
        } catch (e) {
          return undefined;
        }
      }).filter((child) => child !== undefined);
    this.submission = $(entryPoint)
      .find(`.${this.cssClasses.extension}`).children()
      .toArray().map((input) => {
        try {
          return new EntityFormSubmission(input, EntityFormSubmissionMode.Update);
        } catch (e) {
          return undefined;
        }
      }).filter((child) => child !== undefined)[0];

    // attach event listeners
    this.sections.forEach((section) => {
      section.inputs.forEach((input) => {
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
    this.submission.onSubmit(() => {

    });
  }

  get children() {
    return this.sections;
  }

  get modified() {
    return this.sections
      .map((section) => section.modified)
      .reduce((cum, cur) => cum || cur, false);
  }

  set mode(mode: EntityFormMode) {
    if (mode === EntityFormMode.Read && this.modified) {
      confirmAndContinueForm(() => {
        this.sections.forEach((section) => {
          section.inputs.forEach((input) => {
            input.reset();
            input.mode = mode;
          });
        });
        this.submission.modified = this.modified;
      });
    } else {
      this.sections.forEach((section) => {
        section.inputs.forEach((input) => {
          input.mode = mode;
        });
      });
    }
    this._mode = mode;
  }
}

class EntityFormSection extends EntityFormElement {
  get cssClasses() {
    return {
      main: 'form-section',
      title: 'form-section-title',
      extension: 'form-input-container',
    };
  }
  readonly template = `
    <div class="${this.cssClasses.main}">
      <h4 class="${this.cssClasses.title}"></h4>
      <div class="${this.cssClasses.extension}"></div>
    </div>
    `
  inputs: Array<EntityFormInput>;
  submission: EntityFormSubmission;

  constructor (readonly entryPoint: HTMLElement) {
    super(entryPoint);

    if (!$(entryPoint).is(`.${this.cssClasses.main}`)) {
      throw new Error(`Cannot bind form: element not of class ${this.cssClasses.main}`);
    }

    const inputConstructors: Array<(input: HTMLElement) => EntityFormInput> = [
      (input) => new EntityFormTextInput(input),
      (input) => new EntityFormTextAreaInput(input),
      (input) => new EntityFormDropdownInput(input),
      (input) => new EntityFormToggleInput(input),
      (input) => new EntityFormSelectizedInput(input),
      (input) => new EntityFormFileUploadInput(input),
      (input) => new EntityFormHiddenInput(input),
    ];
    this.inputs = $(entryPoint)
      .find(`.${this.cssClasses.extension}`).children()
      .toArray().map((input) => {
        for (let i = 0; i < inputConstructors.length; i += 1) {
          try {
            return inputConstructors[i](input);
          } catch (e) {
          }
        }
      }).filter((child) => child !== undefined);

  }

  get children() {
    return this.inputs;
  }

  get modified() {
    return this.inputs
      .map((input) => input.modified)
      .reduce((cum, cur) => cum || cur, false);
  }
}




abstract class EntityFormInput extends EntityFormElement {
  protected get value(): string {
    return this.$input.val().toString();
  }
  protected set value(value: string) {
    this.$input.val(value);
  }
  set mode(value: EntityFormMode) {
    if (value === EntityFormMode.Read) {
      this.$input.attr('disabled', '');
    } else if (value === EntityFormMode.Write) {
      this.$input.removeAttr('disabled');
    }
  }
  protected originalValue: string;
  protected $entryPoint: JQuery<HTMLElement>;
  protected $input: JQuery<HTMLElement>;

  constructor(entryPoint: HTMLElement) {
    super(entryPoint);
    
    if (!$(entryPoint).is(`.${this.cssClasses.main}`)) {
      throw new Error(`Cannot bind form: element not of class ${this.cssClasses.main}`);
    }
  }

  get children() { return []; }

  onChange(callback: () => void) {
    this.$entryPoint
      .off('change')
      .on('change', callback);
  }

  reset() {
    this.value = this.originalValue;
  }
}

class EntityFormTextInput extends EntityFormInput {
  get cssClasses() {
    return {
      main: 'form-input-text',
      title: 'form-input-text-title',
      extension: 'form-input-text-contents',
    };
  }
  readonly template = `
    <div class="${this.cssClasses.main}">
      <label class="${this.cssClasses.title}"></label>
      <div class="${this.cssClasses.extension}">
        <input></input>
        <span></span>
      </div>
    </div>
    `
  constructor(entryPoint: HTMLElement) {
    super(entryPoint);
    this.$input = this.$entryPoint.find('input');
    this.originalValue = this.value;
  }

  get modified() {
    return (this.value !== this.originalValue);
  }
}

class EntityFormTextAreaInput extends EntityFormInput {
  get cssClasses() {
    return {
      main: 'form-input-text-area',
      title: 'form-input-text-area-title',
      extension: 'form-input-text-area-contents',
    };
  }
  readonly template = `
    <div class="${this.cssClasses.main}">
      <label class="${this.cssClasses.title}"></label>
      <div class="${this.cssClasses.extension}">
        <textarea></textarea>
      </div>
    </div>
    `
  constructor(entryPoint: HTMLElement) {
    super(entryPoint);
    this.$input = this.$entryPoint.find('textarea');
    this.originalValue = this.value;
  }

  get modified() {
    return (this.value !== this.originalValue);
  }
}

class EntityFormDropdownInput extends EntityFormInput {
  get cssClasses() {
    return {
      main: 'form-input-dropdown',
      title: 'form-input-dropdown-title',
      extension: 'form-input-dropdown-contents',
    };
  }
  readonly template = `
    <div class="${this.cssClasses.main}">
      <label class="${this.cssClasses.title}"></label>
      <div class="${this.cssClasses.extension}">
      </div>
    </div>
    `
  constructor(entryPoint: HTMLElement) {
    super(entryPoint);
    this.$input = this.$entryPoint.find('select');
    this.originalValue = this.value;
  }

  get modified() {
    return (this.value !== this.originalValue);
  }
}

class EntityFormToggleInput extends EntityFormInput {
  get cssClasses() {
    return {
      main: 'form-input-toggle',
      title: 'form-input-toggle-title',
      extension: 'form-input-toggle-contents',
    };
  }
  readonly template = `
    <div class="${this.cssClasses.main}">
      <label class="${this.cssClasses.title}"></label>
      <div class="${this.cssClasses.extension}">
      </div>
    </div>
    `
  get value() {
    return this.$input.prop('checked').toString();
  }
  set value(value: string) {
    this.$input.prop('checked', value === 'true');
  }
  constructor(entryPoint: HTMLElement) {
    super(entryPoint);
    this.$input = this.$entryPoint.find('input');
    this.originalValue = this.value;
  }

  get modified() {
    return (this.value !== this.originalValue);
  }
}

class EntityFormSelectizedInput extends EntityFormInput {
  get cssClasses() {
    return {
      main: 'form-input-selectized',
      title: 'form-input-selectized-title',
      extension: 'form-input-selectized-contents',
    };
  }
  readonly template = `
    <div class="${this.cssClasses.main}">
      <label class="${this.cssClasses.title}"></label>
      <div class="${this.cssClasses.extension}">
      </div>
    </div>
    `
  constructor(entryPoint: HTMLElement) {
    super(entryPoint);
    this.$input = this.$entryPoint.find('input');
    this.originalValue = this.value;
  }

  get modified() {
    return (this.value !== this.originalValue);
  }
}

class EntityFormFileUploadInput extends EntityFormInput {
  get cssClasses() {
    return {
      main: 'form-input-file-upload',
      title: 'form-input-file-upload-title',
      extension: 'form-input-file-upload-contents',
    };
  }
  readonly template = `
    <div class="${this.cssClasses.main}">
      <label class="${this.cssClasses.title}"></label>
      <div class="${this.cssClasses.extension}">
      </div>
    </div>
    `
  constructor(entryPoint: HTMLElement) {
    super(entryPoint);
    this.$input = this.$entryPoint.find('input');
    this.originalValue = this.value;
  }

  get modified() {
    return (this.value !== this.originalValue);
  }
}

class EntityFormHiddenInput extends EntityFormInput {
  get cssClasses() {
    return {
      main: 'form-input-hidden',
      title: '',
      extension: 'form-input-hidden-contents',
    };
  }
  readonly template = `
    <div class="${this.cssClasses.main}">
      <div class="${this.cssClasses.extension}">
      </div>
    </div>
    `
  constructor(entryPoint: HTMLElement) {
    super(entryPoint);
    this.$input = this.$entryPoint.find('input');
    this.originalValue = this.value;
  }

  get modified() {
    return (this.value !== this.originalValue);
  }
}

class EntityFormSubmission extends EntityFormElement {
  get cssClasses() {
    return {
      main: 'form-submission',
      title: '',
      extension: '',
    };
  }
  readonly template = `
    <div class="${this.cssClasses.main}">
      <button></button>
    </div>
    `

  private _disabled = false;
  private get buttonSelector(): string {
    return this.mode === EntityFormSubmissionMode.Create
      ? '.button-container-new'
      : '.button-container-edit';
  }

  constructor (entryPoint: HTMLElement, readonly mode: EntityFormSubmissionMode) {
    super(entryPoint);

    if (!$(entryPoint).is(`.${this.cssClasses.main}`)) {
      throw new Error(`Cannot bind form: element not of class ${this.cssClasses.main}`);
    }

  }

  get children() {
    return [];
  }

  get modified() {
    return false;
  }

  set modified(value: boolean) {
    if (value) {
      this.$entryPoint.find(this.buttonSelector).show();
    } else {
      this.$entryPoint.find(this.buttonSelector).hide();
    }
    this._disabled = value;
  }

  onReset(callback: () => void) {
    this.$entryPoint
      .find(this.buttonSelector)
      .find('.button-reset')
      .off('click')
      .on('click', callback);
  }
  onSubmit(callback: () => void) {
    this.$entryPoint
      .find(this.buttonSelector)
      .find('.button-submit')
      .off('click')
      .on('click', callback);
  }
}


class EntityFormSubmissionStatus {
  sections: Array<EntityFormSection>;

  constructor(readonly url: string) {
  }  
}

export enum EntityFormMode {
  Read,
  Write,
}
enum EntityFormSubmissionMode {
  Create,
  Update,
}
enum EntityFormValidationType {
  Any,
  Email,
  Domain,
  Phone,
}
