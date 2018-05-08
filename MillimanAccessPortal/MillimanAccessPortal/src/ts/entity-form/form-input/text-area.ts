import { EntityFormInput } from './input';

export class EntityFormTextAreaInput extends EntityFormInput {
  _cssClasses = {
    main: 'form-input-text-area',
    title: 'form-input-text-area-title',
    extension: 'form-input-text-area-contents',
  }

  protected findInput = ($entryPoint: JQuery<HTMLElement>) => $entryPoint.find('textarea');

  protected getValueFn = ($input: JQuery<HTMLElement>) => $input.val;
  protected setValueFn = ($input: JQuery<HTMLElement>) => $input.val;

  protected disable = ($input: JQuery<HTMLElement>) => $input.attr('disabled', '');
  protected enable = ($input: JQuery<HTMLElement>) => $input.removeAttr('disabled');

  protected comparator = (a: string, b: string) => a === b;

  constructor() {
    super();
  }
}
