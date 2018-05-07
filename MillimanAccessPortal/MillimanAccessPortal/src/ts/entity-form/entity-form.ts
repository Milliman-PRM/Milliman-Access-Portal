import * as $ from 'jquery';


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
  mode: EntityFormMode;
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
          return new EntityFormSubmission(input);
        } catch (e) {
          return undefined;
        }
      }).filter((child) => child !== undefined)[0];

    // attach event listeners
    this.sections.forEach((section) => {
      section.inputs.forEach((input) => {
        input.onChange(() => {
          this.submission.disabled = this.modified;
        });
      });
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
  protected abstract get value(): string;
  protected originalValue: string;
  protected $entryPoint: JQuery<HTMLElement>;

  constructor(entryPoint: HTMLElement) {
    super(entryPoint);
    
    if (!$(entryPoint).is(`.${this.cssClasses.main}`)) {
      throw new Error(`Cannot bind form: element not of class ${this.cssClasses.main}`);
    }
  }

  get children() { return []; }

  onChange(callback: () => void) {
    // TODO: Remove previous callback
    this.$entryPoint.change(callback);
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
  get value() {
    return this.$entryPoint.find('input').val().toString();
  }
  constructor(entryPoint: HTMLElement) {
    super(entryPoint);
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
  get value() {
    return this.$entryPoint.find('textarea').val().toString();
  }
  constructor(entryPoint: HTMLElement) {
    super(entryPoint);
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
  get value() {
    return this.$entryPoint.find('select').val().toString();
  }
  constructor(entryPoint: HTMLElement) {
    super(entryPoint);
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
    return this.$entryPoint.find('input').prop('checked').toString();
  }
  constructor(entryPoint: HTMLElement) {
    super(entryPoint);
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
  get value() {
    return this.$entryPoint.find('input').val().toString();
  }
  constructor(entryPoint: HTMLElement) {
    super(entryPoint);
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
  get value() {
    return this.$entryPoint.find('input').val().toString();
  }
  constructor(entryPoint: HTMLElement) {
    super(entryPoint);
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
  get value() {
    return this.$entryPoint.find('input').val().toString();
  }
  constructor(entryPoint: HTMLElement) {
    super(entryPoint);
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

  constructor (entryPoint: HTMLElement) {
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

  set disabled(value: boolean) {
    if (value) {
      this.$entryPoint.find('button').attr('disabled', '');
    } else {
      this.$entryPoint.find('button').removeAttr('disabled');
    }
    this._disabled = value;
  }
}


class EntityFormSubmissionStatus {
  sections: Array<EntityFormSection>;

  constructor(readonly url: string) {
  }  
}

enum EntityFormMode {
  Read,
  Write,
}

enum EntityFormInputType {
  Text,
  Selectize,
  Dropdown,
  Toggle,
}

enum EntityFormValidationType {
  Any,
  Email,
  Domain,
  Phone,
}
