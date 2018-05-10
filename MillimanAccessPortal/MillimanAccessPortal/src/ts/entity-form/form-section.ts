import { FormElement } from './form-element';
import { EntityFormDropdownInput } from './form-input/dropdown';
import { EntityFormFileUploadInput } from './form-input/file-upload';
import { EntityFormHiddenInput } from './form-input/hidden';
import { EntityFormInput } from './form-input/input';
import { EntityFormSelectizedInput } from './form-input/selectized';
import { EntityFormTextInput } from './form-input/text';
import { EntityFormTextAreaInput } from './form-input/text-area';
import { EntityFormToggleInput } from './form-input/toggle';
import { EntityFormSubmission } from './form-submission';

export class EntityFormSection extends FormElement {
  _cssClasses =  {
    main: 'form-section',
    title: 'form-section-title',
    extension: 'form-input-container',
  };
  inputs: Array<EntityFormInput>;

  public bindToDOM(entryPoint: HTMLElement) {
    super.bindToDOM(entryPoint);

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
        singleMatchedInput.bindToDOM(x);
        return singleMatchedInput;
      })
      .filter((x: EntityFormInput) => x !== undefined);
  }

  public unbindFromDOM() {
    super.unbindFromDOM();
    this.inputs.forEach((section) => section.unbindFromDOM());
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

export class EntityFormSubmissionSection extends FormElement {
  _cssClasses = {
    main: 'form-submission-section',
    title: '',
    extension: '',
  };
  submissions: Array<EntityFormSubmission>;

  public bindToDOM(entryPoint: HTMLElement) {
    super.bindToDOM(entryPoint);

    const childElements = this.$entryPoint.children().toArray();
    this.submissions = childElements
      .map((x: HTMLElement) => ({
        submission: new EntityFormSubmission(),
        element: x,
      }))
      .filter((x) => $(x.element).is(`.${x.submission.cssClasses.main}`))
      .map((x) => {
        x.submission.bindToDOM(x.element);
        return x.submission;
      });
  }
}
