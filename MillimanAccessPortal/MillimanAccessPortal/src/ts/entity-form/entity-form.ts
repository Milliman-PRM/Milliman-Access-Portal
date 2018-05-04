import * as $ from 'jquery';


abstract class EntityFormElement {
  protected readonly cssClass: string;
  protected readonly cssExtensionClass: string;
  protected readonly template: string;

  abstract get children(): Array<EntityFormElement>;
  build(): JQuery<HTMLElement> {
    const $form = $(this.template);
    this.children.forEach((section) => {
      $form.find(`.${this.cssExtensionClass}`).append(section.build());
    });
    return $form;
  }
}

class EntityForm extends EntityFormElement {
  mode: EntityFormMode;
  sections: Array<EntityFormSection>;

  readonly cssClass = 'admin-panel-container';
  readonly cssExtensionClass = 'form-section-container';
  readonly template = `
    <form class="${this.cssClass}">
      <div class="${this.cssExtensionClass}"></div>
    </form>
    `

  constructor(readonly entryPoint: HTMLElement, readonly title: string) {
    super();
  }

  get children() {
    return this.sections;
  }

}

class EntityFormSection extends EntityFormElement {
  inputs: Array<EntityFormInput>;
  submission: EntityFormSubmission;

  constructor (readonly title: string) {
    super();
  }

  get children() {
    return this.inputs;
  }
}

abstract class EntityFormInput extends EntityFormElement {
  value: string;
  originalValue: string;

  constructor(
    readonly id: string,
    readonly name: string = '',
    readonly validation: EntityFormValidationType = EntityFormValidationType.Any,
  ) {
    super();
  }

  get children() { return []; }

  abstract get modified();
}

class EntityFormTextInput extends EntityFormInput {
  constructor(
    readonly id: string,
    readonly name: string = '',
    readonly validation: EntityFormValidationType = EntityFormValidationType.Any,
  ) {
    super(id, name, validation);
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
