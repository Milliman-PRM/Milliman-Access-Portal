import { EntityFormInput } from './input';

export class EntityFormTextInput extends EntityFormInput {
  _cssClasses = {
    main: 'form-input-text',
    title: 'form-input-text-title',
    extension: 'form-input-text-contents',
  }

  protected findInput = ($entryPoint: JQuery<HTMLElement>) => $entryPoint.find('input');

  protected getValueFn = ($input: JQuery<HTMLElement>) => $input.val;
  protected setValueFn = ($input: JQuery<HTMLElement>) => $input.val;

  protected disable = ($input: JQuery<HTMLElement>) => $input.attr('disabled', '');
  protected enable = ($input: JQuery<HTMLElement>) => $input.removeAttr('disabled');

  protected comparator = (a: string, b: string) => a === b;

  constructor() {
    super();
  }
}
