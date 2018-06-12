import { FormElement } from '../form-element';
import { AccessMode } from '../form-modes';

export abstract class FormInput extends FormElement {

  // The DOM element within this form element that holds a form value
  // private _$input: JQuery<HTMLElement>;
  protected get $input(): JQuery<HTMLElement> {
    // Caching $input was causing problems when the related DOM elements
    // were cloned and replaced. Leave this off unless the performance benefits
    // are important.
    //
    // if (!this._$input) {
    //   this._$input = this.findInput(this.$entryPoint);
    // }
    // return this._$input;
    return this.findInput(this.$entryPoint);
  }
  protected abstract findInput: ($enryPoint: JQuery<HTMLElement>) => JQuery<HTMLElement>;

  protected get value(): string {
    const val = this.getValueFn(this.$input).bind(this.$input)();
    return val ? val.toString() : '';
  }
  protected set value(value: string) {
    const change = this.value !== value;
    this.setValueFn(this.$input).bind(this.$input)(value);
    // trigger a change event when value is changed programmatically
    if (change) {
      this.$input.change();
    }
  }
  protected abstract getValueFn: (input: JQuery<HTMLElement>) => () => string | number | string[];
  protected abstract setValueFn: (input: JQuery<HTMLElement>) => (value: string) => void;

  protected abstract disable: (input: JQuery<HTMLElement>) => void;
  protected abstract enable: (input: JQuery<HTMLElement>) => void;

  public get modified(): boolean {
    return !this.comparator(this.originalValue, this.value);
  }
  protected abstract comparator: (a: string, b: string) => boolean;

  protected originalValue: string;

  private _accessMode: AccessMode = AccessMode.Read;
  public get accessMode(): AccessMode {
    return this._accessMode;
  }
  public setAccessMode(accessMode: AccessMode) {
    if (accessMode === AccessMode.Read || accessMode === AccessMode.WriteDisabled) {
      this.disable(this.$input);
    } else if (accessMode === AccessMode.Write) {
      this.enable(this.$input);
    }
    this._accessMode = accessMode;
  }

  public recordOriginalValue() {
    this.originalValue = this.value;
  }

  public onChange(callback: () => void) {
    this.$entryPoint
      .off('keyup change')
      .on('keyup change', callback);
  }

  public reset() {
    this.value = this.originalValue;
  }

  public get name(): string {
    return this.$input.attr('name');
  }
}
