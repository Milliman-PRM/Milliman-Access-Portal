import { FormElement } from './form-element';
import { DropdownInput } from './form-input/dropdown';
import { FileUploadInput } from './form-input/file-upload';
import { HiddenInput } from './form-input/hidden';
import { FormInput } from './form-input/input';
import { NullableTextareaInput } from './form-input/nullable-textarea';
import { SelectizedInput } from './form-input/selectized';
import { TextInput } from './form-input/text';
import { TextareaInput } from './form-input/textarea';
import { ToggleInput } from './form-input/toggle';
import { AccessMode, SubmissionMode } from './form-modes';
import { Submission } from './form-submission';

export class FormInputSection extends FormElement {
  public inputs: FormInput[];

  public validReduce: {
    fn: (cum: boolean, cur: boolean) => boolean;
    init: boolean,
  };

  protected _cssClasses =  {
    main: 'form-section',
    title: 'form-section-title',
    extension: 'form-input-container',
  };

  public constructor(readonly defaultWelcomeText: string = '') {
    super();
  }

  public bindToDOM(entryPoint: HTMLElement) {
    super.bindToDOM(entryPoint);

    const childElements = this.$entryPoint
      .find(`.${this.cssClasses.extension}`).children().toArray();
    const inputConstructors: Array<() => FormInput> = [
      () => new TextInput(),
      () => new TextareaInput(),
      () => new NullableTextareaInput(this.defaultWelcomeText),
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
          throw new Error('Element matches multiple input types.');
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

  public get valid() {
    const filteredInputs = this.inputs
      .map((input) => input.valid)
      .filter((valid) => valid !== undefined);
    return filteredInputs.length
      ? filteredInputs.reduce(this.validReduce.fn, this.validReduce.init)
      : true;
  }

  public get name() {
    return this.$entryPoint.data().section as string;
  }

  public setMode(accessMode: AccessMode, submissionMode: SubmissionMode) {
    if (accessMode === AccessMode.Defer) {
      this.inputs.forEach((input) => {
        input.setAccessMode(submissionMode.groups
          .filter((group) => group.sections !== undefined && group.sections.indexOf(this.name) !== -1).length
            ? AccessMode.Write
            : AccessMode.Read,
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
  public submissions: Submission[];

  protected _cssClasses = {
    main: 'form-submission-section',
    title: '',
    extension: '',
  };

  public bindToDOM(entryPoint: HTMLElement) {
    super.bindToDOM(entryPoint);

    const childElements = this.$entryPoint.children().toArray();
    this.submissions = childElements
      .map((x: HTMLElement) => ({
        element: x,
        submission: new Submission(),
      }))
      .filter((x) => $(x.element).is(`.${x.submission.cssClasses.main}`))
      .map((x) => {
        x.submission.bindToDOM(x.element);
        return x.submission;
      });
  }
}
