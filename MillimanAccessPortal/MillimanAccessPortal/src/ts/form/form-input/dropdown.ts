import { FormInput } from './input';

export class DropdownInput extends FormInput {
  protected _cssClasses = {
    main: 'form-input-dropdown',
    title: 'form-input-dropdown-title',
    extension: 'form-input-dropdown-contents',
  };

  protected findInput = ($entryPoint: JQuery<HTMLElement>) => $entryPoint.find('select');

  protected getValueFn = ($input: JQuery<HTMLElement>) => $input.val;
  protected setValueFn = ($input: JQuery<HTMLElement>) => $input.val;

  protected disable = ($input: JQuery<HTMLElement>) => $input.attr('disabled', '');
  protected enable = ($input: JQuery<HTMLElement>) => $input.removeAttr('disabled');

  protected comparator = (a: string, b: string) => a === b;
}
