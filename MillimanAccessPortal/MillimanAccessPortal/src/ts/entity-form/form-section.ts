import { FormElement } from './form-element';
import { EntityFormDropdownInput } from './form-input/dropdown';
import { EntityFormFileUploadInput } from './form-input/file-upload';
import { EntityFormHiddenInput } from './form-input/hidden';
import { EntityFormInput } from './form-input/input';
import { EntityFormSelectizedInput } from './form-input/selectized';
import { EntityFormTextInput } from './form-input/text';
import { EntityFormTextAreaInput } from './form-input/text-area';
import { EntityFormToggleInput } from './form-input/toggle';
import { SubmissionMode } from './form-modes';

export class EntityFormSection extends FormElement {
  _cssClasses =  {
    main: 'form-section',
    title: 'form-section-title',
    extension: 'form-input-container',
  };
  inputs: Array<EntityFormInput>;

  constructor () {
    super();


  }

  public bind(entryPoint: HTMLElement) {
    super.bind(entryPoint);

    const childElements = this.$entryPoint
      .find(`.${this.cssClasses.extension}`).children().toArray();
    const inputConstructors: Array<() => EntityFormInput> = [
      () => new EntityFormTextInput(),
      () => new EntityFormTextAreaInput(),
      () => new EntityFormDropdownInput(),
      () => new EntityFormToggleInput(),
      () => new EntityFormSelectizedInput(),
      () => new EntityFormFileUploadInput(),
      () => new EntityFormHiddenInput(),
    ];

    this.inputs = childElements
      .map((x: HTMLElement) => {
        const matchedInputs = inputConstructors
          .map((y) => y())
          .filter((y: EntityFormInput) => $(x).is(`.${y.cssClasses.main}`));
        if (matchedInputs.length > 1) {
          throw new Error(`Element matches multiple input types.`);
        } else if (matchedInputs.length === 0) {
          return undefined; 
        }
        const singleMatchedInput = matchedInputs[0];
        singleMatchedInput.bind(x);
        return singleMatchedInput;
      })
      .filter((x: EntityFormInput) => x !== undefined);
  }

  get modified() {
    return this.inputs
      .map((input) => input.modified)
      .reduce((cum, cur) => cum || cur, false);
  }

  get section() {
    return this.$entryPoint.data().section;
  }

  hasInput(inputName: string) {
    return this.inputs
      .map((input) => input.name === inputName)
      .reduce((cum, cur) => cum || cur);
  }
}

export class EntityFormSubmission extends FormElement {
  _cssClasses = {
    main: 'form-submission',
    title: '',
    extension: '',
  };

  private _disabled = false;
  public mode;
  private get buttonSelector(): string {
    return this.mode === SubmissionMode.Create
      ? '.button-container-new'
      : '.button-container-edit';
  }

  constructor() {
    super();
  }

  public bind(entryPoint: HTMLElement) {
    super.bind(entryPoint);
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
