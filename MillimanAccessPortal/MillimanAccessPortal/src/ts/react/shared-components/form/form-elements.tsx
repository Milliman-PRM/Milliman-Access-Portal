import * as React from 'react';

export const FormSectionContainer: React.SFC = (props) => (
  <div className="form-section-container">
    {props.children}
  </div>
);

export const FormSectionDivider: React.SFC = (props) => (
  <div className="form-section-divider">
    {props.children}
  </div>
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
  alignItems?: 'flex-start' | 'flex-end';
  contentItemFlex?: 'none' | 1 | 2;
  flex?: boolean;
  flexPhone?: 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12;
  flexTablet?: 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12;
  flexDesktop?: 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12;
}

export const FormInputContainer: React.SFC<FormInputContainerProps> = (props) => {
  const cssAlign = (props.alignItems) ? `${props.alignItems}` : '';
  const cssPhone = (props.flexPhone) ? ` flex-item-${props.flexPhone}-12` : '';
  const cssTablet = (props.flexTablet) ? ` flex-item-for-tablet-up-${props.flexTablet}-12` : '';
  const cssDesktop = (props.flexDesktop) ? ` flex-item-for-desktop-up-${props.flexDesktop}-12` : '';
  const cssFlex = (props.flex) ? ' form-input-flex' : '';
  const cssCIFlex = (props.contentItemFlex) ? ` content-item-flex-${props.contentItemFlex}` : '';

  return (
    <div className={`form-input-container${cssAlign}${cssPhone}${cssTablet}${cssDesktop}${cssFlex}${cssCIFlex}`}>
      {props.children}
    </div>
  );
};
