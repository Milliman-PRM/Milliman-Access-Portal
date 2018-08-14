import { FormInput } from './input';

export class TextareaInput extends FormInput {
  protected _cssClasses = {
    main: 'form-input-textarea',
    title: 'form-input-textarea-title',
    extension: 'form-input-textarea-contents',
  };

  protected findInput = ($entryPoint: JQuery<HTMLElement>) => $entryPoint.find('textarea');

  protected getValueFn = ($input: JQuery<HTMLElement>) => $input.val;
  protected setValueFn = ($input: JQuery<HTMLElement>) => $input.val;

  protected disable = ($input: JQuery<HTMLElement>) => $input.attr('disabled', '');
  protected enable = ($input: JQuery<HTMLElement>) => $input.removeAttr('disabled');

  protected comparator = (a: string, b: string) => a === b;

  protected validFn = ($input: JQuery<HTMLElement>) => true;
}
