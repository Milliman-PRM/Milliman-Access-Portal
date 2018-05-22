import { AccessMode } from '../form-modes';
import { FormElement } from '../form-element';

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

  private storedValue: string;
  protected get value(): string {
    return this.bound
      ? (() => {
        const val = this.getValueFn(this.$input).bind(this.$input)();
        return val ? val.toString() : '';
      })()
      : this.storedValue;
  }
  protected set value(value: string) {
    if (this.bound) {
      const change = this.value !== value;
      this.setValueFn(this.$input).bind(this.$input)(value);
      // trigger a change event when value is changed programmatically
      if (change) {
        this.$input.change();
      }
    } else {
      this.storedValue = value;
    }
  }
  protected abstract getValueFn: (input: JQuery<HTMLElement>) => () => string | number | string[];
  protected abstract setValueFn: (input: JQuery<HTMLElement>) => (value: string) => void

  public setMode(value: AccessMode) {
    if (value === AccessMode.Read) {
      this.disable(this.$input);
    } else if (value === AccessMode.Write) {
      this.enable(this.$input);
    }
  }
  protected abstract disable: (input: JQuery<HTMLElement>) => void;
  protected abstract enable: (input: JQuery<HTMLElement>) => void;

  public get modified(): boolean {
    return !this.comparator(this.originalValue, this.value);
  }
  protected abstract comparator: (a: string, b: string) => boolean;

  protected originalValue: string;

  public bindToDOM(entryPoint?: HTMLElement) {
    // before bind: this.value references shadow value
    let value: string;
    if (!this.bound) {
      value = this.value;
      this.value = undefined;
    }
    super.bindToDOM(entryPoint);
    // after bind: this.value references DOM value
    if (value) {
      this.value = value;
    }
  }

  public unbindFromDOM() {
    // before unbind: this.value references DOM value
    const value = this.value;
    super.unbindFromDOM();
    // after unbind: this.value references shadow value
    this.value = value;
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
