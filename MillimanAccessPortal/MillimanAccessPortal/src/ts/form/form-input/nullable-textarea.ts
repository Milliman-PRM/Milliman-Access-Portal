import { FormInput } from './input';

export class NullableTextareaInput extends FormInput {
  protected _cssClasses = {
    main: 'form-input-nullable-textarea',
    title: 'form-input-nullable-textarea-title',
    extension: 'form-input-nullable-textarea-contents',
  };

  private memory: string = '';

  public constructor(readonly defaultText: string) {
    super();
  }

  public configure() {
    this.$entryPoint.find('input').off('click').on('click', () => {
      if (this.$entryPoint.find('input').prop('checked')) {
        this.$input.removeAttr('disabled');
        this.$input.val(this.memory);
      } else {
        this.$input.attr('disabled', '');
        this.memory = this.$input.val().toString();
        this.$input.val(this.defaultText);
      }
    });
  }

  protected findInput = ($entryPoint: JQuery<HTMLElement>) => $entryPoint.find('textarea');

  protected getValueFn = ($input: JQuery<HTMLElement>) =>
    this.$entryPoint.find('input').prop('checked') ? $input.val : () => null
  protected setValueFn = ($input: JQuery<HTMLElement>) => (val: string) => {
    this.$entryPoint.find('input').prop('checked', val !== null);
    $input.val(val || '');
    if (val === null) {
      this.$input.val(this.defaultText);
      $input.attr('disabled', '');
    } else {
      $input.removeAttr('disabled');
    }
  }

  protected get value(): string {
    return this.getValueFn(this.$input).bind(this.$input)();
  }
  protected set value(value: string) {
    const change = this.value !== value;
    this.setValueFn(this.$input).bind(this.$input)(value);
    // trigger a change event when value is changed programmatically
    if (change) {
      this.$input.change();
    }
  }

  protected disable = ($input: JQuery<HTMLElement>) => {
    this.$entryPoint.find('input').attr('disabled', '');
    $input.attr('disabled', '');
  }
  protected enable = ($input: JQuery<HTMLElement>) => {
    this.$entryPoint.find('input').removeAttr('disabled');
    if (this.$entryPoint.find('input').prop('checked')) {
      $input.removeAttr('disabled');
    } else {
      $input.attr('disabled', '');
    }
  }

  protected comparator = (a: string, b: string) => a === b;

  protected validFn = () => true;
}
