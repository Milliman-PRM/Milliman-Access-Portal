import * as $ from 'jquery';


interface EntityFormClassRegistry {
  main: string;
  title: string;
  extension: string;
}

abstract class EntityFormElement {
  protected abstract get cssClasses(): EntityFormClassRegistry;
  protected readonly template: string;

  protected abstract get children(): Array<EntityFormElement>;
  build(): JQuery<HTMLElement> {
    const $form = $(this.template);
    this.children.forEach((section) => {
      $form.find(`.${this.cssClasses.main}`).append(section.build());
    });
    return $form;
  }
}

export class EntityForm extends EntityFormElement {
  mode: EntityFormMode;
  sections: Array<EntityFormSection>;

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

  constructor(readonly entryPoint: HTMLElement, readonly title: string) {
    super();

    if (!$(entryPoint).is(`.${this.cssClasses.main}`)) {
      throw new Error(`Cannot bind form: element not of class ${this.cssClasses.main}`);
    }

    this.sections = $(entryPoint)
      .find(`.${this.cssClasses.extension}`).children()
      .toArray().map((section) => {
        try {
          return new EntityFormSection(section);
        } catch (e) {
          return undefined;
        }
      }).filter((child) => child !== undefined);
  }

  get children() {
    return this.sections;
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
    </form>
    `
  inputs: Array<EntityFormInput>;
  submission: EntityFormSubmission;

  constructor (readonly entryPoint: HTMLElement) {
    super();

    if (!$(entryPoint).is(`.${this.cssClasses.main}`)) {
      throw new Error(`Cannot bind form: element not of class ${this.cssClasses.main}`);
    }

    const inputConstructors: Array<(input: HTMLElement) => EntityFormInput> = [
      (input) => new EntityFormTextInput(input),
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
}




abstract class EntityFormInput extends EntityFormElement {
  protected value: string;
  protected originalValue: string;

  constructor(entryPoint: HTMLElement) {
    super();
    
    if (!$(entryPoint).is(`.${this.cssClasses.main}`)) {
      throw new Error(`Cannot bind form: element not of class ${this.cssClasses.main}`);
    }
  }

  get children() { return []; }

  abstract get modified();
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
    </form>
    `
  constructor(entryPoint: HTMLElement) {
    super(entryPoint);
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
    </form>
    `
  constructor(entryPoint: HTMLElement) {
    super(entryPoint);
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
    </form>
    `
  constructor(entryPoint: HTMLElement) {
    super(entryPoint);
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
    </form>
    `
  constructor(entryPoint: HTMLElement) {
    super(entryPoint);
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
    </form>
    `
  constructor(entryPoint: HTMLElement) {
    super(entryPoint);
  }

  get modified() {
    return (this.value !== this.originalValue);
  }
}

class EntityFormHiddenInput extends EntityFormInput {
  get cssClasses() {
    return {
      main: 'form-input-hidden',
      title: 'form-input-hidden-title',
      extension: 'form-input-hidden-contents',
    };
  }
  readonly template = `
    <div class="${this.cssClasses.main}">
      <label class="${this.cssClasses.title}"></label>
      <div class="${this.cssClasses.extension}">
      </div>
    </form>
    `
  constructor(entryPoint: HTMLElement) {
    super(entryPoint);
  }

  get modified() {
    return (this.value !== this.originalValue);
  }
}



class EntityFormSubmission {
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
