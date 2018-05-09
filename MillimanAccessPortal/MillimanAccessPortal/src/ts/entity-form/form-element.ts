import * as $ from 'jquery';

export interface FormClassRegistry {
  main: string;
  title: string;
  extension: string;
}

export abstract class FormElement {
  protected readonly _cssClasses: FormClassRegistry;
  public get cssClasses(): FormClassRegistry {
    return this._cssClasses;
  }
  private _$entryPoint;
  private entryPoint;
  protected get $entryPoint(): JQuery<HTMLElement> {
    if (!this._$entryPoint) {
      this._$entryPoint = this.entryPoint && $(this.entryPoint);
    }
    return this._$entryPoint;
  }
  protected constructor() {
    this._bound = false;
  }

  private _bound: boolean;
  protected get bound(): boolean {
    return this._bound;
  }
  public bindToDOM(entryPoint: HTMLElement) {
    this.entryPoint = entryPoint;
    if (!this.$entryPoint.is(`.${this.cssClasses.main}`)) {
      throw new Error(`Cannot bind to entry point: expected class .${this.cssClasses.main} not found.`);
    }
    this._bound = true;
  }
}
