import * as _ from 'lodash';

import '../../../../images/icons/hide-password.svg';
import '../../../../images/icons/show-password.svg';

import '../../../../scss/react/shared-components/form-elements.scss';

import * as React from 'react';
import TextareaAutosize from 'react-autosize-textarea';

interface BaseInputProps {
  name: string;
  label: string;
  value: string | number | string[];
  onChange?: (currentTarget: React.FormEvent<HTMLInputElement> | React.FormEvent<HTMLTextAreaElement>) => void;
  onBlur?: (currentTarget: React.FormEvent<HTMLInputElement> | React.FormEvent<HTMLTextAreaElement>) => void;
  onClick?: (currentTarget: React.FormEvent<HTMLInputElement> | React.FormEvent<HTMLTextAreaElement> | null) => void;
  error: string;
  placeholderText?: string;
  autoFocus?: boolean;
  inputIcon?: string;
  readOnly?: boolean;
  hidden?: boolean;
}

interface InputProps extends BaseInputProps {
  type: string;
}

export const Input = React.forwardRef<HTMLInputElement, InputProps>((props, ref) => {
  const { name, label, error, inputIcon, placeholderText, children, readOnly, hidden, ...rest } = props;
  return (
    <div className={'form-element-container' + (readOnly ? ' disabled' : '') + (hidden ? ' hidden' : '')}>
      <div className={'form-element-input' + (error ? ' error' : '')}>
        {inputIcon && (
          <div className="input-icon-label">
            <svg className="input-icon">
              <use xlinkHref={`#${inputIcon}`} />
            </svg>
          </div>
        )}
        <div className="form-input-container">
          <input
            name={name}
            id={name}
            ref={ref}
            className="form-input"
            placeholder={placeholderText || label}
            readOnly={readOnly}
            {...rest}
          />
          <label className="form-input-label" htmlFor={name}>{label}</label>
        </div>
        {children}
      </div>
      {error && <div className="error-message">{error}</div>}
    </div>
  );
});

interface TextareaProps extends BaseInputProps {
  rows?: number;
  maxRows?: number;
}

export const TextAreaInput = React.forwardRef<HTMLTextAreaElement, TextareaProps>((props, ref) => {
  const { name, label, error, placeholderText, children, readOnly, hidden, value, rows, maxRows, ...rest } = props;
  return (
    <div className={'form-element-container' + (readOnly ? ' disabled' : '') + (hidden ? ' hidden' : '')}>
      <div className={'form-element-textarea' + (error ? ' error' : '')}>
        <div className="form-input-container">
          <TextareaAutosize
            name={name}
            id={name}
            ref={ref}
            className="form-input"
            placeholder={placeholderText || label}
            readOnly={readOnly}
            data-input-value={value}
            value={value || ''}
            {...rest}
            rows={rows ? rows : 5}
            maxRows={maxRows ? maxRows : 10}
          />
          <label className="form-input-label" htmlFor={name}>{label}</label>
        </div>
        {children}
      </div>
      {error && <div className="error-message">{error}</div>}
    </div>
  );
});

interface MultiAddProps extends InputProps {
  list: string[];
  limit?: number;
  limitText?: string;
  exceptions?: string[];
  addItem: (item: string, overLimit: boolean, itemAlreadyExists: boolean) => void;
  removeItemCallback?: (index: number) => void;
}

interface MultiAddInputState {
  currentText: string;
  isFocused: boolean;
}

export class MultiAddInput extends React.Component<MultiAddProps, MultiAddInputState> {
  public constructor(props: MultiAddProps) {
    super(props);

    this.state = {
      currentText: '',
      isFocused: false,
    };

    this.handleBlur = this.handleBlur.bind(this);
    this.handleFocus = this.handleFocus.bind(this);
  }

  public render() {
    const { name, label, error, placeholderText, children, readOnly, hidden, value, list, limit, limitText, exceptions,
      addItem, removeItemCallback, ...rest } = this.props;

    return (
      <div className={'form-element-container' + (readOnly ? ' disabled' : '') + (hidden ? ' hidden' : '')}>
        <div
          className={'form-element-multi-add-input' + (error ? ' error' : '')}
        >
          <div className="form-input-container-multi-add">
            <span className="badge-container">
              {list.map((element: string, index: number) => {
                return (
                  <div
                    className={`badge ${!exceptions || (exceptions && exceptions.indexOf(element) === -1) ?
                      'badge-secondary' : 'badge-primary'}`}
                    key={index}
                  >
                    {element}
                    <span
                      className="badge-remove-btn"
                      onClick={() => removeItemCallback(index)}
                    >
                      &nbsp;×
                    </span>
                  </div>
                );
              })}
              <input
                type="text"
                name={name}
                id={name}
                className="form-input-multi-add"
                value={this.state.currentText}
                readOnly={readOnly}
                onKeyDown={(event: React.KeyboardEvent<HTMLInputElement>) => {
                  if (!(event.key === 'Tab' && this.state.currentText === '')) {
                    if (event.key === 'Enter' || event.key === ',' || event.key === ';' || event.key === 'Tab') {
                      event.preventDefault();
                      this.addItemAndClear(addItem, list, exceptions, limit);
                    }
                    if ((event.key === 'Backspace' || event.key === 'Delete') && this.state.currentText === '') {
                      event.preventDefault();
                      removeItemCallback(list.length - 1);
                    }
                  }
                }}
                onChange={(event: React.ChangeEvent<HTMLInputElement>) => {
                  this.setState({ currentText: event.currentTarget.value });
                }}
                onFocus={this.handleFocus}
                onBlur={this.handleBlur}
              />
            </span>
            {this.state.currentText.trim() !== '' && this.state.isFocused ?
              <div
                className="multi-add-preview-dropdown"
                onMouseDown={(event: React.MouseEvent) => {
                  event.stopPropagation();
                  this.addItemAndClear(addItem, list, exceptions, limit);
                }}
              >
                Add <strong>{this.state.currentText}</strong>...
              </div> : null
            }
            <label className="form-input-label" htmlFor={name}>
              {label}&nbsp;
              {limit && limitText ?
                <span>
                  ({this.getEffectiveListLength(list, exceptions)} of {limit} {limitText} used)
                </span>
                : null
              }
            </label>
          </div>
          {children}
        </div>
        {error && <div className="error-message">{error}</div>}
      </div>
    );
  }

  private addItemAndClear(addItemCallback: (item: string, overLimit: boolean, itemAlreadyExists: boolean) => void,
                          list: string[] = [], exceptions: string[] = [], limit: number) {
    const effectiveListLength = this.getEffectiveListLength(list, exceptions);
    const overLimit = limit > 0 ? (effectiveListLength >= limit ? true : false) : false;
    const itemAlreadyExists = list.includes(this.state.currentText.trim());

    addItemCallback(this.state.currentText, overLimit, itemAlreadyExists);
    this.setState({ currentText: '' });
  }

  private getEffectiveListLength(list: string[], exceptions: string[]) {
    const tempList = list.slice();
    tempList.push(this.state.currentText);
    const numberOfExceptions = tempList.filter((value) => exceptions.includes(value)).length;

    if (exceptions.includes(this.state.currentText)) {
      return tempList.length - numberOfExceptions;
    }
    return tempList.length - numberOfExceptions - 1;
  }

  private handleBlur() {
    this.setState({ currentText: '', isFocused: false });
  }

  private handleFocus() {
    this.setState({ isFocused: true });
  }
}
