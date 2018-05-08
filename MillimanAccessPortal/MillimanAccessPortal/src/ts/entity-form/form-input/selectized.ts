import { EntityFormInput } from './input';

export class EntityFormSelectizedInput extends EntityFormInput {
  _cssClasses = {
    main: 'form-input-selectized',
    title: 'form-input-selectized-title',
    extension: 'form-input-selectized-contents',
  }

  protected findInput = ($entryPoint: JQuery<HTMLElement>) => $entryPoint.find('input.selectized');

  protected getValueFn = ($input: JQuery<HTMLElement>) => $input.val;
  protected setValueFn = ($input: JQuery<HTMLElement>) => (value: string) => $input[0].selectize.setValue(value.split(','));

  protected disable = ($input: JQuery<HTMLElement>) => $input[0].selectize.disable();
  protected enable = ($input: JQuery<HTMLElement>) => $input[0].selectize.enable();

  protected comparator = (a: string, b: string) => a === b;

  constructor() {
    super();
  }
}
