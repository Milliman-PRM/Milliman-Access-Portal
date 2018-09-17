import { FormInput } from './input';

export class HiddenInput extends FormInput {
  protected _cssClasses = {
    main: 'form-input-hidden',
    title: '',
    extension: 'form-input-hidden-contents',
  };

  protected findInput = ($entryPoint: JQuery<HTMLElement>) => $entryPoint.find('input');

  protected getValueFn = ($input: JQuery<HTMLElement>) => $input.val;
  protected setValueFn = ($input: JQuery<HTMLElement>) => $input.val;

  protected disable = ($input: JQuery<HTMLElement>) => $input.attr('disabled', '');
  protected enable = ($input: JQuery<HTMLElement>) => $input.removeAttr('disabled');

  protected comparator = (a: string, b: string) => a === b;

  protected validFn = () => true;
}
