import {
  FormInput,
} from './input';

export class ToggleInput extends FormInput {
  // tslint:disable:object-literal-sort-keys
  protected _cssClasses = {
    main: 'form-input-toggle',
    title: 'form-input-toggle-title',
    extension: 'form-input-toggle-contents',
  };
  // tslint:enable:object-literal-sort-keys

  protected findInput = ($entryPoint: JQuery<HTMLElement>) => $entryPoint.find('input');

  protected getValueFn = ($input: JQuery<HTMLElement>) => () => $input.prop('checked');
  protected setValueFn = ($input: JQuery<HTMLElement>) => (value: string) => $input.prop('checked', value === 'true');

  protected disable = ($input: JQuery<HTMLElement>) => $input.attr('disabled', '');
  protected enable = ($input: JQuery<HTMLElement>) => $input.removeAttr('disabled');

  protected comparator = (a: string, b: string) => a === b;
}
