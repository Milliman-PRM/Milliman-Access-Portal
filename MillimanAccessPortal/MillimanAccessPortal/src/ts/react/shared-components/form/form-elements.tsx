import * as React from 'react';

export const FormSectionContainer: React.SFC = (props) => (
  <form className="form-section-container" autoComplete="off">
    {props.children}
  </form>
);

interface FormSectionProps {
  title?: string;
}

export const FormSection: React.SFC<FormSectionProps> = (props) => (
  <div className="form-section">
    {props.title && <div className="form-section-title">{props.title}</div>}
    {props.children}
  </div>
);

interface FormInputContainerProps {
  alignSelf?: 'start' | 'end';
  contentItemFlex?: 'none' | 1 | 2;
  flex?: boolean;
  flexPhone?: 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12;
  flexTablet?: 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12;
  flexDesktop?: 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12;
}

export const FormInputContainer: React.SFC<FormInputContainerProps> = (props) => (
  <div
    className={`
      form-input-container
      ${(props.flexPhone) ? ` flex-item-${props.flexPhone}-12` : ''}
      ${(props.flexTablet) ? ` flex-item-for-tablet-up-${props.flexTablet}-12` : ''}
      ${(props.flexDesktop) ? ` flex-item-for-desktop-up-${props.flexDesktop}-12` : ''}
      ${(props.flex) ? ' form-input-flex' : ''}
      ${(props.contentItemFlex) ? ` content-item-flex-${props.contentItemFlex}` : ''}
    `}
  >
    {props.children}
  </div>
);
