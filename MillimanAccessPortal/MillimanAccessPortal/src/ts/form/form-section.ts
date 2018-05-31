import { FormElement } from './form-element';
import { DropdownInput } from './form-input/dropdown';
import { FileUploadInput } from './form-input/file-upload';
import { HiddenInput } from './form-input/hidden';
import { FormInput } from './form-input/input';
import { SelectizedInput } from './form-input/selectized';
import { TextInput } from './form-input/text';
import { TextAreaInput } from './form-input/text-area';
import { ToggleInput } from './form-input/toggle';
import { Submission } from './form-submission';
import { AccessMode, SubmissionMode } from './form-modes';

export class FormInputSection extends FormElement {
  protected _cssClasses =  {
    main: 'form-section',
    title: 'form-section-title',
    extension: 'form-input-container',
  };

  public inputs: Array<FormInput>;

  public bindToDOM(entryPoint: HTMLElement) {
    super.bindToDOM(entryPoint);

    const childElements = this.$entryPoint
      .find(`.${this.cssClasses.extension}`).children().toArray();
    const inputConstructors: Array<() => FormInput> = [
      () => new TextInput(),
      () => new TextAreaInput(),
      () => new DropdownInput(),
      () => new ToggleInput(),
      () => new SelectizedInput(),
      () => new FileUploadInput(),
      () => new HiddenInput(),
    ];

    this.inputs = childElements
      .map((x: HTMLElement) => {
        const matchedInputs = inputConstructors
          .map((y) => y())
          .filter((y: FormInput) => $(x).is(`.${y.cssClasses.main}`));
        if (matchedInputs.length > 1) {
          throw new Error(`Element matches multiple input types.`);
        } else if (matchedInputs.length === 0) {
          return undefined;
        }
        const singleMatchedInput = matchedInputs[0];
        singleMatchedInput.bindToDOM(x);
        return singleMatchedInput;
      })
      .filter((x: FormInput) => x !== undefined);
  }

  public get modified() {
    return this.inputs
      .map((input) => input.modified)
      .reduce((cum, cur) => cum || cur, false);
  }

  public get name() {
    return this.$entryPoint.data().section;
  }

  public setMode(accessMode: AccessMode, submissionMode: SubmissionMode) {
    if (accessMode === AccessMode.Defer) {
      this.inputs.forEach((input) => {
        input.setAccessMode(submissionMode.groups
          .filter((group) => group.sections.indexOf(this.name) !== -1).length
            ? AccessMode.Write
            : AccessMode.Read
        );
      });
    } else {
      this.inputs.forEach((input) => input.setAccessMode(accessMode));
    }
  }

  public hasInput(inputName: string) {
    return this.inputs
      .map((input) => input.name === inputName)
      .reduce((cum, cur) => cum || cur);
  }
}

export class FormSubmissionSection extends FormElement {
  protected _cssClasses = {
    main: 'form-submission-section',
    title: '',
    extension: '',
  };

  public submissions: Array<Submission>;

  public bindToDOM(entryPoint: HTMLElement) {
    super.bindToDOM(entryPoint);

    const childElements = this.$entryPoint.children().toArray();
    this.submissions = childElements
      .map((x: HTMLElement) => ({
        submission: new Submission(),
        element: x,
      }))
      .filter((x) => $(x.element).is(`.${x.submission.cssClasses.main}`))
      .map((x) => {
        x.submission.bindToDOM(x.element);
        return x.submission;
      });
  }
}
