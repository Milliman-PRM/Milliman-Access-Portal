import { AccessMode } from '../form-modes';
import { FormElement } from '../form-element';

export abstract class EntityFormInput extends FormElement {
  protected abstract findInput: ($entryPoint: JQuery<HTMLElement>) => JQuery<HTMLElement>;
  private _$input: JQuery<HTMLElement>;
  protected get $input(): JQuery<HTMLElement> {
    if (!this._$input) {
      this._$input = this.findInput(this.$entryPoint);
    }
    return this._$input;
  }

  protected abstract getValueFn: (input: JQuery<HTMLElement>) => () => string | number | string[];
  protected get value(): string {
    return this.getValueFn(this.$input).bind(this.$input)().toString();
  }
  protected abstract setValueFn: (input: JQuery<HTMLElement>) => (value: string) => void
  protected set value(value: string) {
    this.setValueFn(this.$input).bind(this.$input)(value);
  }

  protected abstract disable: (input: JQuery<HTMLElement>) => void;
  protected abstract enable: (input: JQuery<HTMLElement>) => void;
  public setMode(value: AccessMode) {
    if (value === AccessMode.Read) {
      this.disable(this.$input);
    } else if (value === AccessMode.Write) {
      this.enable(this.$input);
    }
  }

  protected abstract comparator: (a: string, b: string) => boolean;
  public get modified(): boolean {
    return !this.comparator(this.originalValue, this.value);
  }

  protected originalValue: string;

  constructor() {
    super();
  }

  recordOriginalValue() {
    this.originalValue = this.value;
  }

  onChange(callback: () => void) {
    this.$entryPoint
      .off('keyup change')
      .on('keyup change', callback);
  }

  reset() {
    this.value = this.originalValue;
  }

  get name(): string {
    return this.$input.attr('name');
  }
}
