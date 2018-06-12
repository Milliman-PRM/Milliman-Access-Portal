// A value that retains a configurable number of past values
export class RetainedValue<T> {
  private _values: T[];
  get values(): T[] {
    return this._values;
  }
  get now(): T {
    return this._values[0];
  }
  get ref(): T {
    return this._values[this._values.length - 1];
  }

  constructor(readonly lengthLimit: number) {
    this.reset();
  }
  public reset() {
    this._values = [];
  }
  public insert(value: T): void {
    this._values.splice(0, 0, value);
    this._values = this._values.slice(0, this.lengthLimit);
  }
}
