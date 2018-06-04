import { FormInput } from './input';

export class TextAreaInput extends FormInput {
  // tslint:disable:object-literal-sort-keys
  protected _cssClasses = {
    main: 'form-input-text-area',
    title: 'form-input-text-area-title',
    extension: 'form-input-text-area-contents',
  };
  // tslint:enable:object-literal-sort-keys

  protected findInput = ($entryPoint: JQuery<HTMLElement>) => $entryPoint.find('textarea');

  protected getValueFn = ($input: JQuery<HTMLElement>) => $input.val;
  protected setValueFn = ($input: JQuery<HTMLElement>) => $input.val;

  protected disable = ($input: JQuery<HTMLElement>) => $input.attr('disabled', '');
  protected enable = ($input: JQuery<HTMLElement>) => $input.removeAttr('disabled');

  protected comparator = (a: string, b: string) => a === b;
}
